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
    /// Renders VFX prefabs at several simulation times into PNG files, in a throw-away scene
    /// with a URP camera, Bloom volume and a dark ground plane. Used for headless review
    /// (batch mode without -nographics) and from the menu for quick checks.
    /// Output directory: env VFX_PREVIEW_DIR, else &lt;project&gt;/Temp/VfxPreview.
    /// Env VFX_PREVIEW_PREFAB restricts the run to one prefab path; VFX_PREVIEW_TIMES overrides its sample times.
    /// </summary>
    public static class VfxPreviewRenderer
    {
        /// <summary>One prefab to render: sample times, optional motion, optional core-stability sweep.</summary>
        sealed class Job
        {
            public string PrefabPath;
            public float[] Times;
            public Vector3 StartPos = new Vector3(0f, 0.6f, 0f); // chest height of a 1.8 unit enemy
            public Vector3 Velocity;                             // world units / s, for projectiles
            public Vector3 Forward;                              // facing for directional loops (prefab +Z); zero = identity
            public float[] Stabilities;                          // CoreAmbientVfx sweep; null = untouched
            public bool AttractToReference;                      // point VfxParticleAttractor at the reference box
        }

        static readonly float[] ImpactTimes = { 0.04f, 0.12f, 0.25f, 0.45f, 0.8f, 1.2f };

        static readonly Job[] Jobs =
        {
            new Job { PrefabPath = ImpactVfxBuilder.ManaBoltImpactPath, Times = ImpactTimes },
            new Job { PrefabPath = ImpactVfxBuilder.EnemyDeathPath, Times = new[] { 0.04f, 0.2f, 0.5f, 0.9f, 1.3f, 1.7f }, StartPos = Vector3.zero, AttractToReference = true },
            new Job { PrefabPath = ImpactVfxBuilder.BuildCompletePath, Times = new[] { 0.04f, 0.12f, 0.3f, 0.6f, 1.0f, 1.4f }, StartPos = new Vector3(0f, 0.05f, 0f) },
            new Job
            {
                PrefabPath = ManaBoltProjectileBuilder.PrefabPath,
                Times = new[] { 0.05f, 0.15f, 0.3f, 0.5f },
                StartPos = new Vector3(-2.2f, 0.9f, 0f),
                Velocity = new Vector3(8f, 0f, 0f),
            },
            new Job
            {
                PrefabPath = CoreAmbientBuilder.PrefabPath,
                Times = new[] { 4.0f },
                StartPos = Vector3.zero,
                Stabilities = new[] { 100f, 45f, 15f },
            },
            new Job { PrefabPath = ImpactVfxBuilder.FireImpactPath, Times = ImpactTimes },
            new Job
            {
                PrefabPath = FlameBreathBuilder.BreathPath,
                Times = new[] { 0.3f, 0.8f, 1.5f },
                StartPos = new Vector3(-1.8f, 0.9f, 0f),
                Forward = Vector3.right,
            },
            new Job
            {
                PrefabPath = FlameBreathBuilder.BurningPath,
                Times = new[] { 0.4f, 1.2f, 1.9f },
                StartPos = new Vector3(1.6f, 0f, 0.4f), // the reference character's feet
            },
            // ---- batch 3 ----
            new Job { PrefabPath = ImpactVfxBuilder.SandBurstPath, Times = new[] { 0.05f, 0.15f, 0.35f, 0.7f, 1.1f }, StartPos = Vector3.zero },
            new Job { PrefabPath = ImpactVfxBuilder.EnemySpawnPath, Times = new[] { 0.05f, 0.2f, 0.5f, 1.0f }, StartPos = Vector3.zero },
            new Job { PrefabPath = ImpactVfxBuilder.ManaChargeCompletePath, Times = new[] { 0.05f, 0.15f, 0.4f }, StartPos = new Vector3(0f, 1f, 0f) },
            new Job { PrefabPath = ImpactVfxBuilder.CoreDamageEnemyPath, Times = new[] { 0.1f, 0.5f, 0.9f, 1.3f }, StartPos = Vector3.zero, AttractToReference = true },
            new Job { PrefabPath = ImpactVfxBuilder.CoreDamageFlashPath, Times = new[] { 0.05f, 0.15f, 0.35f, 0.6f }, StartPos = Vector3.zero },
            new Job { PrefabPath = GoldVfxBuilder.LevelUpPath, Times = new[] { 0.08f, 0.25f, 0.5f, 0.9f }, StartPos = Vector3.zero },
            new Job { PrefabPath = GoldVfxBuilder.WaveClearPath, Times = new[] { 0.1f, 0.3f, 0.8f, 1.6f, 2.4f }, StartPos = Vector3.zero },
            new Job { PrefabPath = LoopVfxBuilder.SummonCirclePath, Times = new[] { 3f }, StartPos = Vector3.zero },
            new Job { PrefabPath = LoopVfxBuilder.ManaChargePath, Times = new[] { 1.5f, 3f }, StartPos = new Vector3(-1.2f, 0f, 0f), AttractToReference = true },
            new Job { PrefabPath = LoopVfxBuilder.TorchPath, Times = new[] { 2f }, StartPos = new Vector3(0f, 1.4f, 0f) },
            new Job { PrefabPath = LoopVfxBuilder.FloatingDustPath, Times = new[] { 4f }, StartPos = Vector3.zero },
            new Job
            {
                PrefabPath = BeamVfxBuilder.PierceBeamPath,
                Times = new[] { 0.05f, 0.15f, 0.3f },
                StartPos = new Vector3(-2.5f, 0.9f, 0f),
                Forward = Vector3.right,
            },
            new Job { PrefabPath = BeamVfxBuilder.SummonPillarPath, Times = new[] { 0.1f, 0.3f, 0.55f, 0.9f }, StartPos = Vector3.zero },
        };

        [MenuItem("DesertTower/VFX/Render Preview Frames (All)")]
        public static void RenderFromMenu() => RenderAll();

        /// <summary>Batch entry point kept for older scripts: rebuild everything, then render.</summary>
        public static void BuildAndRender() => VfxBatch.BuildAllAndRender();

        public static string OutputDir()
        {
            string outDir = System.Environment.GetEnvironmentVariable("VFX_PREVIEW_DIR");
            if (string.IsNullOrEmpty(outDir)) outDir = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "VfxPreview");
            return outDir;
        }

        public static void RenderAll()
        {
            string outDir = OutputDir();
            Directory.CreateDirectory(outDir);

            IEnumerable<Job> jobs = Jobs;
            string only = System.Environment.GetEnvironmentVariable("VFX_PREVIEW_PREFAB");
            if (!string.IsNullOrEmpty(only))
            {
                var match = System.Array.Find(Jobs, j => j.PrefabPath == only) ?? new Job { PrefabPath = only, Times = ImpactTimes };
                string timesEnv = System.Environment.GetEnvironmentVariable("VFX_PREVIEW_TIMES");
                if (!string.IsNullOrEmpty(timesEnv))
                    match.Times = System.Array.ConvertAll(timesEnv.Split(','), s => float.Parse(s.Trim(), System.Globalization.CultureInfo.InvariantCulture));
                jobs = new[] { match };
            }

            var stage = BuildStage();
            try
            {
                foreach (var job in jobs) Render(job, stage, outDir);
            }
            finally
            {
                stage.Dispose();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            Debug.Log($"[VFX] Preview frames written to {outDir}");
        }

        // ---- Stage ----------------------------------------------------------

        sealed class Stage : System.IDisposable
        {
            public Camera Cam;
            public Transform Reference;
            public RenderTexture Rt;
            public Texture2D Readback;
            public List<Object> Temp = new List<Object>();

            public void Dispose()
            {
                Cam.targetTexture = null;
                Rt.Release();
                foreach (var o in Temp) Object.DestroyImmediate(o);
            }
        }

        static Stage BuildStage()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            var stage = new Stage();

            // Ground: dark sandstone-ish plane so the emissive colours read against something
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 2f;
            var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMat.SetColor("_BaseColor", new Color(0.30f, 0.24f, 0.16f));
            groundMat.SetFloat("_Smoothness", 0.1f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;
            stage.Temp.Add(groundMat);

            // Key light (dim, so emissive particles dominate)
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 0.6f;
            sun.color = new Color(1f, 0.92f, 0.8f);
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.12f, 0.16f);

            // Scale reference: a 1.8 unit "character" box beside the effect (also the attractor target)
            var refBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            refBox.name = "ReferenceCharacter";
            refBox.transform.localScale = new Vector3(0.6f, 1.8f, 0.6f);
            refBox.transform.position = new Vector3(1.6f, 0.9f, 0.4f);
            var refMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            refMat.SetColor("_BaseColor", new Color(0.55f, 0.50f, 0.42f));
            refBox.GetComponent<Renderer>().sharedMaterial = refMat;
            stage.Temp.Add(refMat);
            stage.Reference = refBox.transform;

            // Post-processing volume with Bloom at the spec values (§5-1)
            var volume = new GameObject("Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 0.9f;
            bloom.intensity.value = 0.8f;
            bloom.scatter.value = 0.6f;
            volume.sharedProfile = profile;
            stage.Temp.Add(profile);

            // Camera
            var camGo = new GameObject("PreviewCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.13f);
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            cam.allowHDR = true;
            camGo.transform.position = new Vector3(0f, 2.2f, -4.2f);
            camGo.transform.LookAt(new Vector3(0f, 0.5f, 0f));
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.None;
            stage.Cam = cam;

            const int w = 960, h = 540;
            stage.Rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf);
            cam.targetTexture = stage.Rt;
            stage.Readback = new Texture2D(w, h, TextureFormat.RGBA32, false);
            stage.Temp.Add(stage.Rt);
            stage.Temp.Add(stage.Readback);
            return stage;
        }

        // ---- Render one prefab ----------------------------------------------

        static void Render(Job job, Stage stage, string outDir)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(job.PrefabPath);
            if (prefab == null) { Debug.LogError($"[VFX] Prefab not found: {job.PrefabPath}"); return; }
            string prefix = Path.GetFileNameWithoutExtension(job.PrefabPath).Replace("VFX_", "");

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                instance.transform.position = job.StartPos;
                if (job.Velocity != Vector3.zero)
                    instance.transform.rotation = Quaternion.LookRotation(job.Velocity); // prefab +Z = travel
                else if (job.Forward != Vector3.zero)
                    instance.transform.rotation = Quaternion.LookRotation(job.Forward);

                var attractors = instance.GetComponentsInChildren<VfxParticleAttractor>();
                if (job.AttractToReference)
                    foreach (var a in attractors) a.Target = stage.Reference;
                var beams = instance.GetComponentsInChildren<VfxBeam>();

                var core = instance.GetComponent<CoreAmbientVfx>();
                var stabilities = job.Stabilities != null && core != null ? job.Stabilities : new[] { float.NaN };

                foreach (var stability in stabilities)
                {
                    string stageTag = float.IsNaN(stability) ? "" : $"_s{stability:0}";
                    if (core != null && !float.IsNaN(stability)) core.SetStability(stability);

                    foreach (var t in job.Times)
                    {
                        foreach (var b in beams) b.Restart();
                        SimulateTo(instance, job, t, dt =>
                        {
                            foreach (var a in attractors) a.Tick(dt);
                            foreach (var b in beams) if (dt > 0f) b.Tick(dt);
                            if (core != null) core.Tick(dt);
                        });

                        foreach (var sub in instance.GetComponentsInChildren<ParticleSystem>())
                            Debug.Log($"[VFX] {prefix}{stageTag} t={t:0.000} {sub.name}: alive={sub.particleCount}");

                        stage.Cam.Render();
                        var prev = RenderTexture.active;
                        RenderTexture.active = stage.Rt;
                        stage.Readback.ReadPixels(new Rect(0, 0, stage.Rt.width, stage.Rt.height), 0, 0);
                        stage.Readback.Apply();
                        RenderTexture.active = prev;

                        var file = Path.Combine(outDir, $"{prefix}{stageTag}_t{t:0.00}.png");
                        File.WriteAllBytes(file, stage.Readback.EncodeToPNG());
                        Debug.Log($"[VFX] wrote {file}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>
        /// Restart every system and advance to <paramref name="t"/>. Moving effects and effects with
        /// scripted particles are stepped at 60 Hz so rate-over-distance and the scripts see the motion.
        /// </summary>
        static void SimulateTo(GameObject instance, Job job, float t, System.Action<float> tick)
        {
            var systems = instance.GetComponentsInChildren<ParticleSystem>();
            instance.transform.position = job.StartPos;
            foreach (var ps in systems) ps.Simulate(0f, withChildren: false, restart: true, fixedTimeStep: false);
            tick(0f); // lets CoreAmbientVfx push colours/rates before any particle is born

            const float dt = 1f / 60f;
            for (float elapsed = 0f; elapsed < t - 1e-4f; elapsed += dt)
            {
                float step = Mathf.Min(dt, t - elapsed);
                instance.transform.position += job.Velocity * step;
                foreach (var ps in systems) ps.Simulate(step, withChildren: false, restart: false, fixedTimeStep: false);
                tick(step);
            }
        }
    }
}
