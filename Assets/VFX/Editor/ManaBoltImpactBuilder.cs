using System.IO;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;

namespace DesertTower.VFX.Editor
{
    /// <summary>
    /// Builds the "Mana Bolt Impact" VFX prefab (SandGuard VFX 제작계획 v0.2, #2) from code.
    /// Menu: DesertTower > VFX > Build Mana Bolt Impact.
    /// Produces: procedural pixel-glow / ring textures, four URP Particles/Unlit materials,
    /// a teal point-light prefab, and a 5-layer particle prefab (Flash, Core, Shockwave, Debris, Ember).
    /// Safe to re-run: existing assets are overwritten in place so GUIDs stay stable.
    /// </summary>
    public static class ManaBoltImpactBuilder
    {
        const string RootDir = "Assets/VFX";
        const string TexDir = RootDir + "/Textures";
        const string MatDir = RootDir + "/Materials";
        const string PrefabDir = RootDir + "/Prefabs";

        const string GlowTexPath = TexDir + "/T_VFX_PixelGlow_32.png";
        const string RingTexPath = TexDir + "/T_VFX_PixelRing_64.png";

        const string MatCubeTealPath = MatDir + "/M_VFX_Cube_Teal.mat";
        const string MatGlowTealPath = MatDir + "/M_VFX_Glow_Teal.mat";
        const string MatGlowWhitePath = MatDir + "/M_VFX_Glow_White.mat";
        const string MatRingTealPath = MatDir + "/M_VFX_Ring_Teal.mat";

        const string LightPrefabPath = PrefabDir + "/VFX_PointLight_Teal.prefab";
        const string ImpactPrefabPath = PrefabDir + "/VFX_ManaBolt_Impact.prefab";

        const string ShaderName = "Universal Render Pipeline/Particles/Unlit";

        // Palette from SandGuard_VFX_제작계획_v0.2 §2
        static readonly Color Teal = new Color(0.18f, 0.90f, 0.84f, 1f);      // #2EE6D6
        static readonly Color TealDark = new Color(0.04f, 0.30f, 0.34f, 1f);

        [MenuItem("DesertTower/VFX/Build Mana Bolt Impact")]
        public static void Build()
        {
            var prefab = BuildPrefab();
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[VFX] Built {ImpactPrefabPath}");
        }

        [MenuItem("DesertTower/VFX/Build Mana Bolt Impact + Place In Scene")]
        public static void BuildAndPlace()
        {
            var prefab = BuildPrefab();
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = prefab.name + " (Preview)";
            var view = SceneView.lastActiveSceneView;
            instance.transform.position = view != null ? view.pivot : Vector3.zero;
            Undo.RegisterCreatedObjectUndo(instance, "Place VFX preview");
            Selection.activeGameObject = instance;
            var ps = instance.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play(true);
            Debug.Log("[VFX] Placed preview. Select it and press Play in the Particle Effect overlay to replay.");
        }

        // ------------------------------------------------------------------
        // Prefab
        // ------------------------------------------------------------------

        static GameObject BuildPrefab()
        {
            EnsureFolders();

            var glowTex = BuildGlowTexture();
            var ringTex = BuildRingTexture();

            var matCubeTeal = BuildMaterial(MatCubeTealPath, null, Teal * 2.5f, opaque: true);
            var matGlowTeal = BuildMaterial(MatGlowTealPath, glowTex, Teal * 2.0f, opaque: false);
            var matGlowWhite = BuildMaterial(MatGlowWhitePath, glowTex, Color.white * 4.0f, opaque: false);
            var matRingTeal = BuildMaterial(MatRingTealPath, ringTex, Teal * 2.0f, opaque: false);

            var lightPrefab = BuildLightPrefab();
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

            var root = new GameObject("VFX_ManaBolt_Impact");
            try
            {
                BuildCore(root, cube, matCubeTeal, lightPrefab);
                BuildFlash(Child(root, "Flash"), matGlowWhite);
                BuildShockwave(Child(root, "Shockwave"), matRingTeal);
                BuildDebris(Child(root, "Debris"), cube, matCubeTeal);
                BuildEmber(Child(root, "Ember"), matGlowTeal);

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, ImpactPrefabPath);
                AssetDatabase.SaveAssets();
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        // ---- Layer 2: Core (root system) -----------------------------------
        static void BuildCore(GameObject go, Mesh cube, Material mat, Light lightPrefab)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1.0f;
            main.startLifetime = 0.15f;
            main.startSpeed = 0f;
            main.startSize = 0.35f;
            main.maxParticles = 8;
            SetRandomRotation3D(main);

            Burst(ps, 4);
            Size(ps, Punch(0.5f, 1f, 0f));
            ColorGradient(ps, Color.white, Teal, Teal);

            MeshRenderer(r, cube, mat);

            var lights = ps.lights;
            lights.enabled = true;
            lights.light = lightPrefab;
            lights.ratio = 0.25f;
            lights.maxLights = 1;
            lights.useRandomDistribution = false;
            lights.useParticleColor = true;
            lights.sizeAffectsRange = false;
            lights.alphaAffectsIntensity = false;
            lights.range = 3f;
            lights.intensity = new ParticleSystem.MinMaxCurve(4f, Falloff());
        }

        // ---- Layer 1: Flash -------------------------------------------------
        static void BuildFlash(GameObject go, Material mat)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1.0f;
            main.startLifetime = 0.12f;   // ~7 frames at 60 fps: 3 held, 4 shrinking
            main.startSpeed = 0f;
            main.startSize = 1.4f;
            main.maxParticles = 2;

            Burst(ps, 1);
            Size(ps, HoldThenDrop(0.4f));

            BillboardRenderer(r, mat, ParticleSystemRenderMode.Billboard);
            r.sortingFudge = -10f; // draw on top of siblings
        }

        // ---- Layer 3: Shockwave ---------------------------------------------
        static void BuildShockwave(GameObject go, Material mat)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1.0f;
            main.startLifetime = 0.3f;
            main.startSpeed = 0f;
            main.startSize = 2.2f;
            main.maxParticles = 2;

            Burst(ps, 1);
            Size(ps, EaseOut(0.15f, 1f));
            AlphaFade(ps, Color.white);

            BillboardRenderer(r, mat, ParticleSystemRenderMode.HorizontalBillboard);
            go.transform.localPosition = new Vector3(0f, 0.02f, 0f); // lift off the ground to avoid z-fight
        }

        // ---- Layer 4: Debris ------------------------------------------------
        static void BuildDebris(GameObject go, Mesh cube, Material mat)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1.0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.15f);
            main.gravityModifier = 0.9f;
            main.maxParticles = 32;
            SetRandomRotation3D(main);

            Burst(ps, 24);

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var limit = ps.limitVelocityOverLifetime; // air drag so cubes stay near the hit point
            limit.enabled = true;
            limit.dampen = 0.25f;
            limit.limit = 1.5f;

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            rot.y = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            rot.z = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);

            Size(ps, HoldThenDrop(0.6f));
            ColorGradient(ps, Color.white, Teal, TealDark);

            MeshRenderer(r, cube, mat);
        }

        // ---- Layer 5: Ember / afterglow -------------------------------------
        static void BuildEmber(GameObject go, Material mat)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1.0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            main.maxParticles = 16;

            Burst(ps, 14);

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.3f;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // cone points up

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.5f;
            noise.scrollSpeed = 0.5f;

            Size(ps, Linear(1f, 0f));
            AlphaFade(ps, Color.white);

            BillboardRenderer(r, mat, ParticleSystemRenderMode.Billboard);
        }

        // ------------------------------------------------------------------
        // Particle helpers
        // ------------------------------------------------------------------

        static ParticleSystem AddSystem(GameObject go, out ParticleSystemRenderer renderer)
        {
            var ps = go.AddComponent<ParticleSystem>();
            renderer = go.GetComponent<ParticleSystemRenderer>();

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None; // pooled by VfxService later
            main.startColor = Color.white;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = false;

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        static void Burst(ParticleSystem ps, int count)
        {
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        }

        static void SetRandomRotation3D(ParticleSystem.MainModule main)
        {
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        }

        static void Size(ParticleSystem ps, AnimationCurve curve)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        static void ColorGradient(ParticleSystem ps, Color a, Color b, Color c)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.35f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = g;
        }

        static void AlphaFade(ParticleSystem ps, Color tint)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(tint, 0f), new GradientColorKey(tint, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = g;
        }

        static void MeshRenderer(ParticleSystemRenderer r, Mesh mesh, Material mat)
        {
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = mesh;
            r.sharedMaterial = mat;
            r.alignment = ParticleSystemRenderSpace.World;
            r.enableGPUInstancing = true;
        }

        static void BillboardRenderer(ParticleSystemRenderer r, Material mat, ParticleSystemRenderMode mode)
        {
            r.renderMode = mode;
            r.sharedMaterial = mat;
        }

        // Curves ------------------------------------------------------------
        static AnimationCurve Linear(float from, float to) => AnimationCurve.Linear(0f, from, 1f, to);

        static AnimationCurve EaseOut(float from, float to)
        {
            var c = new AnimationCurve(new Keyframe(0f, from, 0f, (to - from) * 4f), new Keyframe(1f, to, 0f, 0f));
            return c;
        }

        /// <summary>Starts at <paramref name="start"/>, pops to <paramref name="peak"/> at 20%, eases to <paramref name="end"/>.</summary>
        static AnimationCurve Punch(float start, float peak, float end)
        {
            return new AnimationCurve(
                new Keyframe(0f, start, 0f, (peak - start) * 6f),
                new Keyframe(0.2f, peak, 0f, 0f),
                new Keyframe(1f, end, (end - peak) * 0.5f, 0f));
        }

        /// <summary>Holds full size until <paramref name="holdUntil"/>, then shrinks to 0.</summary>
        static AnimationCurve HoldThenDrop(float holdUntil)
        {
            return new AnimationCurve(
                new Keyframe(0f, 1f, 0f, 0f),
                new Keyframe(holdUntil, 1f, 0f, 0f),
                new Keyframe(1f, 0f, -2f, 0f));
        }

        static AnimationCurve Falloff() => new AnimationCurve(new Keyframe(0f, 1f, 0f, -1f), new Keyframe(1f, 0f, -1f, 0f));

        // ------------------------------------------------------------------
        // Materials
        // ------------------------------------------------------------------

        static Material BuildMaterial(string path, Texture2D baseMap, Color hdrColor, bool opaque)
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
                throw new FileNotFoundException($"Shader not found: {ShaderName}. Is URP installed?");

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = mat == null;
            if (isNew) mat = new Material(shader);
            else mat.shader = shader;

            mat.SetTexture("_BaseMap", baseMap);
            mat.SetColor("_BaseColor", hdrColor);
            mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            mat.SetFloat("_SoftParticlesEnabled", 0f);
            mat.SetFloat("_ColorMode", 0f); // Multiply with particle color

            if (opaque)
            {
                mat.SetFloat("_Surface", 0f); // Opaque
                mat.SetFloat("_Blend", 0f);
            }
            else
            {
                mat.SetFloat("_Surface", 1f); // Transparent
                mat.SetFloat("_Blend", 2f);   // Additive (BaseShaderGUI.BlendMode.Additive)
            }

            BaseShaderGUI.SetupMaterialBlendMode(mat);
            BaseShaderGUI.SetMaterialKeywords(mat);

            if (isNew) AssetDatabase.CreateAsset(mat, path);
            else EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------
        // Light prefab (for the Lights module)
        // ------------------------------------------------------------------

        static Light BuildLightPrefab()
        {
            var go = new GameObject("VFX_PointLight_Teal");
            try
            {
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = Teal;
                light.range = 3f;
                light.intensity = 4f;
                light.shadows = LightShadows.None;
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, LightPrefabPath);
                return prefab.GetComponent<Light>();
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // ------------------------------------------------------------------
        // Procedural pixel textures (point-filtered so the glow reads as blocks)
        // ------------------------------------------------------------------

        static Texture2D BuildGlowTexture()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size - 0.5f, dy = (y + 0.5f) / size - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f; // 0 center → 1 edge
                float a = Mathf.Clamp01(1f - d);
                a = a * a;                 // soft falloff
                a = Posterize(a, 5);       // blocky steps
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            return SavePng(tex, GlowTexPath);
        }

        static Texture2D BuildRingTexture()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size - 0.5f, dy = (y + 0.5f) / size - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float band = 1f - Mathf.Abs(d - 0.82f) / 0.14f; // ring centred at 82% radius
                float a = Posterize(Mathf.Clamp01(band), 3);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            return SavePng(tex, RingTexPath);
        }

        static float Posterize(float v, int steps) => Mathf.Round(v * steps) / steps;

        static Texture2D SavePng(Texture2D tex, string path)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ------------------------------------------------------------------

        static void EnsureFolders()
        {
            EnsureFolder("Assets", "VFX");
            EnsureFolder(RootDir, "Textures");
            EnsureFolder(RootDir, "Materials");
            EnsureFolder(RootDir, "Prefabs");
        }

        static void EnsureFolder(string parent, string name)
        {
            var full = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(full)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
