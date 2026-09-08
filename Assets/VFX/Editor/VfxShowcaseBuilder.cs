using System.Collections.Generic;
using System.IO;
using DesertTower.VFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DesertTower.VFX.Editor
{
    /// <summary>
    /// Builds Assets/VFX/Scenes/VFX_Showcase.unity: every VFX prefab on its own station, with the
    /// props each one needs (a target for the XP cubes, a dummy enemy to burn, a facility that rises).
    /// Open it and press Play — VfxShowcaseStation keeps everything cycling so the scripted parts
    /// (attractor, spin, flicker, scale-in, core stability, beams) can be seen, not just the particles.
    /// </summary>
    public static class VfxShowcaseBuilder
    {
        public const string SceneDir = VfxBuildKit.RootDir + "/Scenes";
        public const string ScenePath = SceneDir + "/VFX_Showcase.unity";

        const int PerRow = 5;
        const float SpacingX = 4.6f;
        const float SpacingZ = 5.5f;
        static readonly Color Sand = new Color(0.62f, 0.50f, 0.32f);
        static readonly Color Prop = new Color(0.45f, 0.40f, 0.34f);

        [MenuItem("DesertTower/VFX/Showcase/Build Showcase Scene")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("DesertTower/VFX/Showcase/Open Showcase Scene")]
        public static void Open()
        {
            if (!File.Exists(ScenePath)) { Build(); }
            else if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("DesertTower/VFX/Showcase/Open Showcase Scene + Play")]
        public static void OpenAndPlay()
        {
            Open();
            EditorApplication.isPlaying = true;
        }

        // ---- Exhibit list ---------------------------------------------------
        // Each entry: label, mode, prefab, and a dresser that adds props / offsets once the station is placed.

        sealed class Exhibit
        {
            public string Label;
            public VfxShowcaseStation.Mode Mode;
            public string Prefab;
            public System.Action<VfxShowcaseStation> Dress;
        }

        static Exhibit[] Exhibits() => new[]
        {
            // Row 1: the mana loop
            new Exhibit { Label = "1 Mana Bolt (projectile → impact)", Mode = VfxShowcaseStation.Mode.Projectile, Prefab = ManaBoltProjectileBuilder.PrefabPath, Dress = st =>
            {
                st.ImpactPrefab = Load(ImpactVfxBuilder.ManaBoltImpactPath);
                st.transform.position += new Vector3(-2f, 0.9f, 0f);
                st.transform.rotation = Quaternion.LookRotation(Vector3.right);
                st.Track = 4f;
            }},
            new Exhibit { Label = "2 Pierce Beam", Mode = VfxShowcaseStation.Mode.Replay, Prefab = BeamVfxBuilder.PierceBeamPath, Dress = st =>
            {
                st.transform.position += new Vector3(-2.3f, 0.9f, 0f);
                st.transform.rotation = Quaternion.LookRotation(Vector3.right);
                st.Interval = 1.6f;
                Box("Dummy", st.transform.position + new Vector3(2.6f, 0f, 0.3f), new Vector3(0.6f, 1.8f, 0.6f));
            }},
            new Exhibit { Label = "3 Mana Charge (core → lamp) + Complete", Mode = VfxShowcaseStation.Mode.Loop, Prefab = LoopVfxBuilder.ManaChargePath, Dress = st =>
            {
                st.transform.position += new Vector3(-1.2f, 0f, 0f);
                var lamp = Box("Lamp", st.transform.position + new Vector3(2.6f, 0.5f, 0f), new Vector3(0.4f, 1.0f, 0.4f));
                st.Target = lamp.transform;
                var done = st.gameObject.AddComponent<VfxShowcaseStation>(); // second exhibit on the same station
                done.Kind = VfxShowcaseStation.Mode.Replay;
                done.Prefab = Load(ImpactVfxBuilder.ManaChargeCompletePath);
                done.Interval = 3f;
                done.enabled = false;
                var holder = new GameObject("Complete (at lamp)");
                holder.transform.position = lamp.transform.position + new Vector3(0f, 0.6f, 0f);
                var st2 = holder.AddComponent<VfxShowcaseStation>();
                st2.Kind = VfxShowcaseStation.Mode.Replay; st2.Prefab = done.Prefab; st2.Interval = 3f;
                Object.DestroyImmediate(done);
            }},
            new Exhibit { Label = "4 Core Ambient (stability sweep)", Mode = VfxShowcaseStation.Mode.Core, Prefab = CoreAmbientBuilder.PrefabPath, Dress = st => st.CorePeriod = 10f },
            new Exhibit { Label = "5 Core Damage (enemy → core, flash)", Mode = VfxShowcaseStation.Mode.Replay, Prefab = ImpactVfxBuilder.CoreDamageEnemyPath, Dress = st =>
            {
                st.transform.position += new Vector3(-1.4f, 0f, 0f);
                st.Interval = 2.8f;
                var core = new GameObject("Core (target)");
                core.transform.position = st.transform.position + new Vector3(2.8f, 0f, 0f);
                st.Target = core.transform;
                var flash = core.AddComponent<VfxShowcaseStation>();
                flash.Kind = VfxShowcaseStation.Mode.Replay; flash.Prefab = Load(ImpactVfxBuilder.CoreDamageFlashPath); flash.Interval = 2.8f;
                Box("Dummy", st.transform.position + new Vector3(0f, 0.9f, 0f), new Vector3(0.6f, 1.8f, 0.6f));
            }},

            // Row 2: fire
            new Exhibit { Label = "6 Flame Cobra Breath", Mode = VfxShowcaseStation.Mode.PulseLoop, Prefab = FlameBreathBuilder.BreathPath, Dress = st =>
            {
                st.transform.position += new Vector3(-1.2f, 0.9f, 0f);
                st.transform.rotation = Quaternion.LookRotation(Vector3.right);
                Box("Cobra head", st.transform.position + new Vector3(-0.35f, 0f, 0f), new Vector3(0.5f, 0.5f, 0.5f));
            }},
            new Exhibit { Label = "7 Fire Impact", Mode = VfxShowcaseStation.Mode.Replay, Prefab = ImpactVfxBuilder.FireImpactPath, Dress = st =>
            {
                st.transform.position += new Vector3(0f, 0.6f, 0f);
                Box("Dummy", st.transform.position + new Vector3(0.7f, 0.3f, 0.5f), new Vector3(0.6f, 1.8f, 0.6f));
            }},
            new Exhibit { Label = "8 Burning (on a dummy)", Mode = VfxShowcaseStation.Mode.Replay, Prefab = FlameBreathBuilder.BurningPath, Dress = st =>
            {
                Box("Dummy", st.transform.position + new Vector3(0f, 0.9f, 0f), new Vector3(0.6f, 1.8f, 0.6f));
                st.Interval = 3.2f;
            }},
            new Exhibit { Label = "9 Torch", Mode = VfxShowcaseStation.Mode.Loop, Prefab = LoopVfxBuilder.TorchPath, Dress = st =>
            {
                Box("Post", st.transform.position + new Vector3(0f, 0.7f, 0f), new Vector3(0.18f, 1.4f, 0.18f));
                st.transform.position += new Vector3(0f, 1.4f, 0f);
            }},
            new Exhibit { Label = "10 Sand Burst", Mode = VfxShowcaseStation.Mode.Replay, Prefab = ImpactVfxBuilder.SandBurstPath, Dress = st => st.Interval = 3f },

            // Row 3: world / ground
            new Exhibit { Label = "11 Enemy Death (tint + XP to player)", Mode = VfxShowcaseStation.Mode.EnemyDeath, Prefab = ImpactVfxBuilder.EnemyDeathPath, Dress = st =>
            {
                var player = Capsule("Player (XP target)", st.transform.position + new Vector3(1.8f, 0f, 0.6f));
                st.Target = player.transform;
                st.Interval = 2.6f;
            }},
            new Exhibit { Label = "12 Enemy Spawn", Mode = VfxShowcaseStation.Mode.Replay, Prefab = ImpactVfxBuilder.EnemySpawnPath, Dress = st => st.Interval = 2.4f },
            new Exhibit { Label = "13 Build Complete (facility rises)", Mode = VfxShowcaseStation.Mode.BuildComplete, Prefab = ImpactVfxBuilder.BuildCompletePath, Dress = st =>
            {
                st.Facility = Box("Facility", st.transform.position + new Vector3(0f, 0.5f, 0f), Vector3.one);
                st.Interval = 3f;
            }},
            new Exhibit { Label = "14 Summon Circle + Pillar", Mode = VfxShowcaseStation.Mode.Loop, Prefab = LoopVfxBuilder.SummonCirclePath, Dress = st =>
            {
                var pillar = st.gameObject.AddComponent<VfxShowcaseStation>();
                pillar.Kind = VfxShowcaseStation.Mode.Replay; pillar.Prefab = Load(BeamVfxBuilder.SummonPillarPath); pillar.Interval = 3.5f;
            }},
            new Exhibit { Label = "15 Mana Bolt Impact (alone)", Mode = VfxShowcaseStation.Mode.Replay, Prefab = ImpactVfxBuilder.ManaBoltImpactPath, Dress = st =>
            {
                st.transform.position += new Vector3(0f, 0.6f, 0f);
                st.Interval = 1.8f;
            }},

            // Row 4: gold + ambient
            new Exhibit { Label = "16 Level Up", Mode = VfxShowcaseStation.Mode.Replay, Prefab = GoldVfxBuilder.LevelUpPath, Dress = st =>
            {
                Capsule("Player", st.transform.position);
                st.Interval = 2.6f;
            }},
            new Exhibit { Label = "17 Wave Clear", Mode = VfxShowcaseStation.Mode.Replay, Prefab = GoldVfxBuilder.WaveClearPath, Dress = st => st.Interval = 3.6f },
            new Exhibit { Label = "18 Floating Dust (whole scene)", Mode = VfxShowcaseStation.Mode.Loop, Prefab = LoopVfxBuilder.FloatingDustPath, Dress = st =>
            {
                st.transform.position = new Vector3(0f, 0f, SpacingZ * 1.5f); // centre of the grid
            }},
        };

        // ---- Build ----------------------------------------------------------

        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(SceneDir)) AssetDatabase.CreateFolder(VfxBuildKit.RootDir, "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            var exhibits = Exhibits();
            int rows = Mathf.CeilToInt(exhibits.Length / (float)PerRow);
            BuildEnvironment(rows);
            var cam = BuildCamera(rows);

            for (int i = 0; i < exhibits.Length; i++)
            {
                var e = exhibits[i];
                int row = i / PerRow, col = i % PerRow;
                int inRow = Mathf.Min(PerRow, exhibits.Length - row * PerRow);
                float x0 = -SpacingX * (inRow - 1) * 0.5f;

                var go = new GameObject(e.Label);
                go.transform.position = new Vector3(x0 + col * SpacingX, 0f, row * SpacingZ);
                var st = go.AddComponent<VfxShowcaseStation>();
                st.Kind = e.Mode;
                st.Prefab = Load(e.Prefab);
                // Labels sit low in front of each station so they never overlap the row behind.
                Label(e.Label, go.transform.position + new Vector3(0f, 0.25f, -1.9f), cam);
                e.Dress?.Invoke(st);
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[VFX] Built showcase scene {ScenePath} ({exhibits.Length} exhibits)");
        }

        /// <summary>
        /// Batch check: open the showcase scene, spawn every station's prefab at 0.3 s of simulation and
        /// write one frame from the scene camera, so the layout and labels can be reviewed headlessly.
        /// </summary>
        public static void RenderCheck(string outDir)
        {
            if (!File.Exists(ScenePath)) Build();
            EditorSceneManager.OpenScene(ScenePath);
            var cam = Camera.main;
            if (cam == null) { Debug.LogError("[VFX] Showcase: no MainCamera."); return; }

            var spawned = new List<GameObject>();
            foreach (var st in Object.FindObjectsByType<VfxShowcaseStation>(FindObjectsSortMode.None))
            {
                if (st.Prefab == null) continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(st.Prefab);
                inst.transform.SetPositionAndRotation(st.transform.position, st.transform.rotation);
                float t = st.Kind == VfxShowcaseStation.Mode.Core || st.Kind == VfxShowcaseStation.Mode.Loop ? 3f : 0.3f;
                foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>()) ps.Simulate(t, false, true, false);
                foreach (var b in inst.GetComponentsInChildren<VfxBeam>()) { b.Restart(); b.Tick(0.15f); }
                spawned.Add(inst);
            }

            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;

            Directory.CreateDirectory(outDir);
            var file = Path.Combine(outDir, "Showcase_layout.png");
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Debug.Log($"[VFX] wrote {file}");

            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            foreach (var go in spawned) Object.DestroyImmediate(go);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        // ---- Helpers --------------------------------------------------------

        static GameObject Load(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) Debug.LogError($"[VFX] Showcase: prefab missing {path}. Run DesertTower > VFX > Build All first.");
            return prefab;
        }

        static void BuildEnvironment(int rows)
        {
            float depth = rows * SpacingZ + 4f;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, 0f, (rows - 1) * SpacingZ * 0.5f);
            ground.transform.localScale = new Vector3(3.6f, 1f, depth / 10f);
            ground.GetComponent<Renderer>().sharedMaterial = LitMaterial("M_Showcase_Ground", Sand);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 0.9f;
            sun.color = new Color(1f, 0.93f, 0.82f);
            sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.20f, 0.24f);

            var volume = new GameObject("Volume (Bloom)").AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 0.9f;
            bloom.intensity.value = 0.8f;
            bloom.scatter.value = 0.6f;
            AssetDatabase.CreateAsset(profile, SceneDir + "/VFX_Showcase_Volume.asset");
            volume.sharedProfile = profile;
        }

        static Camera BuildCamera(int rows)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.10f, 0.14f);
            cam.fieldOfView = 52f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 120f;
            cam.allowHDR = true;
            float midZ = (rows - 1) * SpacingZ * 0.5f;
            camGo.transform.position = new Vector3(0f, 6f + rows * 2.6f, -9f - rows * 2.2f);
            camGo.transform.LookAt(new Vector3(0f, 0.3f, midZ));
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            camGo.AddComponent<AudioListener>();
            return cam;
        }

        static GameObject Box(string name, Vector3 center, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = LitMaterial("M_Showcase_Prop", Prop);
            return go;
        }

        static GameObject Capsule(string name, Vector3 feet)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.position = feet + new Vector3(0f, 0.9f, 0f);
            go.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            go.GetComponent<Renderer>().sharedMaterial = LitMaterial("M_Showcase_Prop", Prop);
            return go;
        }

        static void Label(string text, Vector3 pos, Camera cam)
        {
            var go = new GameObject("Label");
            go.transform.position = pos;
            go.transform.rotation = cam.transform.rotation; // face the camera
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 48;
            tm.characterSize = 0.05f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.95f, 0.92f, 0.85f);
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
        }

        static Material LitMaterial(string name, Color color)
        {
            string path = $"{SceneDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
