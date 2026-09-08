using System.IO;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;

namespace DesertTower.VFX.Editor
{
    /// <summary>
    /// Shared building blocks for every code-built VFX prefab (SandGuard VFX 제작계획 v0.2 §2, §5).
    /// Palette, URP particle materials, procedural pixel textures, the shared point-light prefab
    /// and the particle-module helpers. Every builder in this folder goes through here so the
    /// style rules (HDR emissive, ease-out size curves, cube rotation) live in one place.
    /// </summary>
    public static class VfxBuildKit
    {
        public const string RootDir = "Assets/VFX";
        public const string TexDir = RootDir + "/Textures";
        public const string MatDir = RootDir + "/Materials";
        public const string PrefabDir = RootDir + "/Prefabs";

        const string ShaderName = "Universal Render Pipeline/Particles/Unlit";

        // ---- Palette (§2) ---------------------------------------------------
        public static readonly Color Teal = Hex("2EE6D6");
        public static readonly Color TealDark = new Color(0.04f, 0.30f, 0.34f);
        public static readonly Color Orange = Hex("FF8A2A");
        public static readonly Color Yellow = Hex("FFD34D");
        public static readonly Color Beige = Hex("D9B77A");
        public static readonly Color BeigeDark = new Color(0.36f, 0.28f, 0.16f);
        public static readonly Color Red = Hex("E0402A");
        public static readonly Color RedDark = new Color(0.30f, 0.06f, 0.04f);
        public static readonly Color Smoke = new Color(0.08f, 0.07f, 0.08f);
        public static readonly Color Gold = Hex("F2C14E");
        public static readonly Color GoldDark = new Color(0.40f, 0.28f, 0.08f);

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;

        // ---- Shared assets --------------------------------------------------
        // Materials are white so the particle colour (start colour × colour over lifetime) decides the hue.
        // HDR intensity lives in the material so Bloom still catches it (§2 "이미시브 강도 2~6").
        public const string MatCubeWhitePath = MatDir + "/M_VFX_Cube_White.mat";
        public const string MatCubeTealPath = MatDir + "/M_VFX_Cube_Teal.mat";   // for plain MeshRenderers (no particle colour)
        public const string MatGlowWhitePath = MatDir + "/M_VFX_Glow_White.mat";
        public const string MatGlowSoftPath = MatDir + "/M_VFX_Glow_Soft.mat";   // lower intensity, for smoke / afterglow
        public const string MatMeshAdditivePath = MatDir + "/M_VFX_Mesh_Additive.mat"; // untextured additive, for ring / beam meshes
        public const string MatSmokePath = MatDir + "/M_VFX_Smoke.mat";           // alpha-blended, CFXR smoke cloud 2×2
        public const string MatFireFlipbookPath = MatDir + "/M_VFX_Fire_Flipbook.mat"; // CFXR fire small anim 3×3 (8 frames)
        public const string MatFlamePath = MatDir + "/M_VFX_Flame.mat";           // CFXR stepped flame, single sprite
        public const string MatBeamPath = MatDir + "/M_VFX_Beam.mat";             // additive stripes, scrolled by VfxBeam
        public const string MatMagicCircleTealPath = MatDir + "/M_VFX_MagicCircle_Teal.mat"; // Hovl rune ring, for flat quads
        public const string LightPrefabPath = PrefabDir + "/VFX_PointLight.prefab";
        public const string GlowTexPath = TexDir + "/T_VFX_PixelGlow_32.png";
        public const string BeamTexPath = TexDir + "/T_VFX_BeamStripes_16x64.png";
        public const string FireFlipbookTexPath = TexDir + "/T_VFX_FireFlipbook_3x3.png";
        public const string FlameTexPath = TexDir + "/T_VFX_Flame_Stepped.png";
        public const string MagicCircleTexPath = TexDir + "/T_VFX_MagicCircle.png";

        // ---- Borrowed pack assets (§6: Cartoon FX Remaster FREE, Hovl Magic Effects Pack) ----
        // Textures are converted into Assets/VFX/Textures at build time (CFXR fire masks carry the shape in
        // luminance, not alpha); meshes are referenced in place and fitted by their bounds, so pack units don't matter.
        public const string CfxrFireAnimPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/cfxr fire small anim.png";
        public const string CfxrFlameSteppedPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/cfxr flamme stepped.png";
        public const string CfxrSmokeCloudPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/cfxr smoke cloud x4.png";
        public const string HovlTorusPath = "Assets/Hovl Studio/Magic effects pack/Models/Torus1.fbx";
        public const string HovlCrystalPath = "Assets/Hovl Studio/Magic effects pack/Models/Crystal1.fbx";
        public const string HovlCylinderPath = "Assets/Hovl Studio/Magic effects pack/Models/Cylinder.fbx";
        public const string HovlMagicCirclePath = "Assets/Hovl Studio/Magic effects pack/Textures/MagicCircle.png";
        public const int FireFlipbookTiles = 3;   // 3×3 sheet
        public const int FireFlipbookFrames = 8;  // last cell is empty

        public sealed class Shared
        {
            public Mesh Cube, Quad, Torus, Crystal, Cylinder;
            public Material CubeWhite, CubeTeal, GlowWhite, GlowSoft, MeshAdditive, Smoke, FireFlipbook, Flame, Beam, MagicCircleTeal;
            public Light LightPrefab;
        }

        static Shared _shared;

        /// <summary>Builds (or refreshes) the textures, materials and light prefab every effect uses.</summary>
        public static Shared BuildShared()
        {
            EnsureFolders();
            var glowTex = BuildGlowTexture();
            var beamTex = BuildBeamStripesTexture();
            var fireTex = ConvertMaskToAlpha(CfxrFireAnimPath, FireFlipbookTexPath, posterizeSteps: 4);
            var flameTex = ConvertMaskToAlpha(CfxrFlameSteppedPath, FlameTexPath, posterizeSteps: 5);
            var circleTex = ConvertMaskToAlpha(HovlMagicCirclePath, MagicCircleTexPath, posterizeSteps: 4);
            var smokeTex = LoadPackAsset<Texture2D>(CfxrSmokeCloudPath);
            _shared = new Shared
            {
                Cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx"),
                Quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx"),
                Torus = LoadPackAsset<Mesh>(HovlTorusPath),
                Crystal = LoadPackAsset<Mesh>(HovlCrystalPath),
                Cylinder = LoadPackAsset<Mesh>(HovlCylinderPath),
                Beam = BuildMaterial(MatBeamPath, beamTex, Color.white * 2.5f, opaque: false),
                MagicCircleTeal = BuildMaterial(MatMagicCircleTealPath, circleTex, Teal * 2.5f, opaque: false),
                CubeWhite = BuildMaterial(MatCubeWhitePath, null, Color.white * 2.5f, opaque: true),
                CubeTeal = BuildMaterial(MatCubeTealPath, null, Teal * 2.5f, opaque: true),
                GlowWhite = BuildMaterial(MatGlowWhitePath, glowTex, Color.white * 4.0f, opaque: false),
                GlowSoft = BuildMaterial(MatGlowSoftPath, glowTex, Color.white * 1.2f, opaque: false),
                MeshAdditive = BuildMaterial(MatMeshAdditivePath, null, Color.white * 2.0f, opaque: false),
                Smoke = BuildMaterial(MatSmokePath, smokeTex, Color.white, opaque: false, additive: false),
                FireFlipbook = BuildMaterial(MatFireFlipbookPath, fireTex, Color.white * 2.2f, opaque: false),
                Flame = BuildMaterial(MatFlamePath, flameTex, Color.white * 3.0f, opaque: false),
                LightPrefab = BuildLightPrefab(),
            };
            return _shared;
        }

        static T LoadPackAsset<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException($"[VFX] Pack asset missing: {path}. The VFX builders borrow from Cartoon FX Remaster FREE and Hovl Magic Effects Pack (see Assets/VFX/README.md).");
            return asset;
        }

        // ---- Mesh fitting ---------------------------------------------------
        /// <summary>Axis (0 = X, 1 = Y, 2 = Z) along which the mesh is thinnest / longest.</summary>
        public static int ThinAxis(Mesh m) { var s = m.bounds.size; return s.x <= s.y && s.x <= s.z ? 0 : s.y <= s.z ? 1 : 2; }
        public static int LongAxis(Mesh m) { var s = m.bounds.size; return s.x >= s.y && s.x >= s.z ? 0 : s.y >= s.z ? 1 : 2; }

        /// <summary>Rotation that turns the given local axis onto +Y (flat ring: pass ThinAxis; upright crystal: pass LongAxis).</summary>
        public static Quaternion AxisToUp(int axis) =>
            axis == 0 ? Quaternion.Euler(0f, 0f, 90f) : axis == 2 ? Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity;

        /// <summary>Rotation that turns the given local axis onto +Z (beam meshes: pass LongAxis).</summary>
        public static Quaternion AxisToForward(int axis) =>
            axis == 0 ? Quaternion.Euler(0f, -90f, 0f) : axis == 1 ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;

        /// <summary>
        /// Child MeshRenderer that presents <paramref name="mesh"/> as a unit beam: diameter 1, running from
        /// local z = 0 to z = 1 on the parent. The parent's scale then means (width, width, length).
        /// </summary>
        public static MeshRenderer UnitBeamMesh(GameObject parent, Mesh mesh, Material mat)
        {
            int axis = LongAxis(mesh);
            var size = mesh.bounds.size;
            float len = size[axis];
            float wide = Mathf.Max(0.0001f, Mathf.Max(axis == 0 ? size.y : size.x, axis == 2 ? size.y : size.z));
            var scale = Vector3.one / wide;
            scale[axis] = 1f / len;
            var rot = AxisToForward(axis);

            var pivot = Child(parent, "Mesh");
            pivot.transform.localRotation = rot;
            pivot.transform.localScale = scale;
            pivot.transform.localPosition = -(rot * Vector3.Scale(mesh.bounds.center, scale)) + new Vector3(0f, 0f, 0.5f);

            pivot.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = pivot.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mr;
        }

        /// <summary>Flat quad on the ground (normal +Y) with a material; spins about its normal with VfxSpin.</summary>
        public static MeshRenderer GroundQuad(GameObject go, Mesh quad, Material mat, float size, float degreesPerSecond)
        {
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = Vector3.one * size;
            go.AddComponent<MeshFilter>().sharedMesh = quad;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var spin = go.AddComponent<VfxSpin>();
            spin.DegreesPerSecond = new Vector3(0f, 0f, degreesPerSecond); // local Z is the quad normal after the tilt
            return mr;
        }

        /// <summary>Several bursts of the same count, <paramref name="interval"/> seconds apart.</summary>
        public static void Bursts(ParticleSystem ps, int count, int times, float interval)
        {
            var list = new ParticleSystem.Burst[times];
            for (int i = 0; i < times; i++) list[i] = new ParticleSystem.Burst(i * interval, (short)count);
            ps.emission.SetBursts(list);
        }

        /// <summary>Planes collision against <paramref name="ground"/> (local y = 0, normal +Y) for cubes that must land.</summary>
        public static void GroundCollision(ParticleSystem ps, Transform ground)
        {
            var col = ps.collision;
            col.enabled = true;
            col.type = ParticleSystemCollisionType.Planes;
            col.mode = ParticleSystemCollisionMode.Collision3D;
            col.AddPlane(ground);
            col.bounce = 0.25f;
            col.dampen = 0.45f;
            col.lifetimeLoss = 0f;
            col.radiusScale = 0.5f; // cube half-extent, so they rest on the plane rather than sink half-way
        }

        /// <summary>Uniform scale that makes the mesh's largest extent equal <paramref name="size"/>.</summary>
        public static float FitScale(Mesh m, float size)
        {
            var s = m.bounds.size;
            return size / Mathf.Max(s.x, Mathf.Max(s.y, s.z));
        }

        /// <summary>Texture sheet animation: play <paramref name="frames"/> cells of a square grid once over the lifetime.</summary>
        public static void Flipbook(ParticleSystem ps, int tiles, int frames)
        {
            var tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Grid;
            tsa.numTilesX = tiles;
            tsa.numTilesY = tiles;
            tsa.animation = ParticleSystemAnimationType.WholeSheet;
            tsa.cycleCount = 1;
            float last = (float)frames / (tiles * tiles); // frameOverTime is normalised over the whole sheet
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, last - 0.001f));
        }

        /// <summary>Texture sheet used as a random still: one cell per particle, no animation.</summary>
        public static void RandomTile(ParticleSystem ps, int tilesX, int tilesY)
        {
            var tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Grid;
            tsa.numTilesX = tilesX;
            tsa.numTilesY = tilesY;
            tsa.animation = ParticleSystemAnimationType.WholeSheet;
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 0.999f); // random-between-constants: fixed per particle
        }

        public static Shared GetShared() => _shared ?? BuildShared();

        // ---- Prefab scaffolding ---------------------------------------------
        /// <summary>
        /// Empty ParticleSystem on a prefab root whose layers are all children. It emits nothing, but it
        /// gives the root a Particle Effect overlay in the editor (Play/Restart runs the children) and a
        /// single handle for Play/Stop(withChildren) at runtime.
        /// </summary>
        public static ParticleSystem AddHub(GameObject root)
        {
            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = 0;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            root.GetComponent<ParticleSystemRenderer>().enabled = false;
            return ps;
        }

        public static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        public static GameObject SavePrefab(GameObject root, string path)
        {
            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                AssetDatabase.SaveAssets();
                Debug.Log($"[VFX] Built {path}");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        public static void PlaceInScene(GameObject prefab)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = prefab.name + " (Preview)";
            var view = SceneView.lastActiveSceneView;
            instance.transform.position = view != null ? view.pivot : Vector3.zero;
            Undo.RegisterCreatedObjectUndo(instance, "Place VFX preview");
            Selection.activeGameObject = instance;
            foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>()) ps.Play(false);
            Debug.Log("[VFX] Placed preview. Select it and press Play in the Particle Effect overlay to replay.");
        }

        // ---- Particle systems -----------------------------------------------
        public static ParticleSystem AddSystem(GameObject go, out ParticleSystemRenderer renderer, bool loop = false)
        {
            var ps = go.AddComponent<ParticleSystem>();
            renderer = go.GetComponent<ParticleSystemRenderer>();

            var main = ps.main;
            main.loop = loop;
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

        public static void Burst(ParticleSystem ps, int count, float time = 0f)
        {
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(time, (short)count) });
        }

        public static void Rate(ParticleSystem ps, float perSecond, float perUnit = 0f)
        {
            var emission = ps.emission;
            emission.rateOverTime = perSecond;
            emission.rateOverDistance = perUnit;
        }

        public static void RandomRotation3D(ParticleSystem.MainModule main)
        {
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        }

        /// <summary>Cube tumble, ±180°/s on every axis (§2 회전).</summary>
        public static void Tumble(ParticleSystem ps, float radiansPerSecond = Mathf.PI)
        {
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-radiansPerSecond, radiansPerSecond);
            rot.y = new ParticleSystem.MinMaxCurve(-radiansPerSecond, radiansPerSecond);
            rot.z = new ParticleSystem.MinMaxCurve(-radiansPerSecond, radiansPerSecond);
        }

        public static void Size(ParticleSystem ps, AnimationCurve curve)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        public static void SphereShape(ParticleSystem ps, float radius)
        {
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
        }

        public static void ConeShape(ParticleSystem ps, float angle, float radius)
        {
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = radius;
        }

        public static void CircleShape(ParticleSystem ps, float radius, float thickness = 1f, bool burstSpread = false)
        {
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = thickness;
            shape.arc = 360f;
            shape.arcMode = burstSpread ? ParticleSystemShapeMultiModeValue.BurstSpread : ParticleSystemShapeMultiModeValue.Random;
        }

        public static void Drag(ParticleSystem ps, float dampen, float limit)
        {
            var lim = ps.limitVelocityOverLifetime;
            lim.enabled = true;
            lim.dampen = dampen;
            lim.limit = limit;
        }

        public static void Noise(ParticleSystem ps, float strength, float frequency, float scroll)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = strength;
            noise.frequency = frequency;
            noise.scrollSpeed = scroll;
        }

        /// <summary>Three-stop colour ramp, full alpha. Multiplies the particle start colour.</summary>
        public static void ColorRamp(ParticleSystem ps, Color a, Color b, Color c, float mid = 0.35f)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, mid), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = g;
        }

        /// <summary>Three-stop colour ramp that also fades alpha to 0 from <paramref name="fadeFrom"/>. For additive sprites.</summary>
        public static void ColorRampFade(ParticleSystem ps, Color a, Color b, Color c, float mid = 0.35f, float fadeFrom = 0.6f)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, mid), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, fadeFrom), new GradientAlphaKey(0f, 1f) });
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = g;
        }

        public static void AlphaFade(ParticleSystem ps, Color tint, float holdUntil = 0.3f)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(tint, 0f), new GradientColorKey(tint, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, holdUntil), new GradientAlphaKey(0f, 1f) });
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = g;
        }

        public static void UseMesh(ParticleSystemRenderer r, Mesh mesh, Material mat)
        {
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = mesh;
            r.sharedMaterial = mat;
            r.alignment = ParticleSystemRenderSpace.World;
            r.enableGPUInstancing = true;
        }

        public static void UseBillboard(ParticleSystemRenderer r, Material mat, ParticleSystemRenderMode mode = ParticleSystemRenderMode.Billboard)
        {
            r.renderMode = mode;
            r.sharedMaterial = mat;
        }

        /// <summary>Lights module: one point light per <paramref name="ratio"/> particles, coloured by the particle.</summary>
        public static void ParticleLight(ParticleSystem ps, Light prefab, float intensity, float range, float ratio = 0.25f, int maxLights = 1)
        {
            var lights = ps.lights;
            lights.enabled = true;
            lights.light = prefab;
            lights.ratio = ratio;
            lights.maxLights = maxLights;
            lights.useRandomDistribution = false;
            lights.useParticleColor = true;
            lights.sizeAffectsRange = false;
            lights.alphaAffectsIntensity = false;
            lights.range = range;
            lights.intensity = new ParticleSystem.MinMaxCurve(intensity, Falloff());
        }

        // ---- Curves ---------------------------------------------------------
        public static AnimationCurve Linear(float from, float to) => AnimationCurve.Linear(0f, from, 1f, to);

        public static AnimationCurve EaseOut(float from, float to) =>
            new AnimationCurve(new Keyframe(0f, from, 0f, (to - from) * 4f), new Keyframe(1f, to, 0f, 0f));

        /// <summary>Starts at <paramref name="start"/>, pops to <paramref name="peak"/> at 20%, eases to <paramref name="end"/>.</summary>
        public static AnimationCurve Punch(float start, float peak, float end) =>
            new AnimationCurve(
                new Keyframe(0f, start, 0f, (peak - start) * 6f),
                new Keyframe(0.2f, peak, 0f, 0f),
                new Keyframe(1f, end, (end - peak) * 0.5f, 0f));

        /// <summary>Holds full size until <paramref name="holdUntil"/>, then shrinks to 0.</summary>
        public static AnimationCurve HoldThenDrop(float holdUntil) =>
            new AnimationCurve(
                new Keyframe(0f, 1f, 0f, 0f),
                new Keyframe(holdUntil, 1f, 0f, 0f),
                new Keyframe(1f, 0f, -2f, 0f));

        /// <summary>Grows in fast, holds, shrinks out. For loop particles that must not pop.</summary>
        public static AnimationCurve InOut(float growUntil = 0.15f, float shrinkFrom = 0.7f) =>
            new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 1f / growUntil),
                new Keyframe(growUntil, 1f, 0f, 0f),
                new Keyframe(shrinkFrom, 1f, 0f, 0f),
                new Keyframe(1f, 0f, -1f / (1f - shrinkFrom), 0f));

        public static AnimationCurve Falloff() => new AnimationCurve(new Keyframe(0f, 1f, 0f, -1f), new Keyframe(1f, 0f, -1f, 0f));

        // ---- Materials ------------------------------------------------------
        public static Material BuildMaterial(string path, Texture2D baseMap, Color hdrColor, bool opaque, bool additive = true)
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
                mat.SetFloat("_Blend", additive ? 2f : 0f);   // BaseShaderGUI.BlendMode: 2 = Additive, 0 = Alpha
            }

            BaseShaderGUI.SetupMaterialBlendMode(mat);
            BaseShaderGUI.SetMaterialKeywords(mat);

            if (isNew) AssetDatabase.CreateAsset(mat, path);
            else EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---- Light prefab (for the Lights module; colour comes from the particle) ----
        static Light BuildLightPrefab()
        {
            var go = new GameObject("VFX_PointLight");
            try
            {
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = Color.white;
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

        // ---- Procedural pixel textures (point-filtered so the glow reads as blocks) ----
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

        /// <summary>Bands across V so a scrolled V offset reads as energy flowing along a beam; the gaps stay faintly lit.</summary>
        static Texture2D BuildBeamStripesTexture()
        {
            const int w = 16, h = 64, band = 8;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                bool on = (y / band) % 2 == 0;
                float a = on ? 1f : 0.3f;
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            var saved = SavePng(tex, BeamTexPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(BeamTexPath);
            importer.wrapMode = TextureWrapMode.Repeat; // scrolling needs wrap
            importer.SaveAndReimport();
            return saved;
        }

        /// <summary>
        /// CFXR fire textures carry the shape in luminance with no alpha channel. Copy them into our
        /// texture folder as white-with-alpha sprites (posterized so they read as pixel steps) so the
        /// standard URP particle shader can use them. Reads the PNG bytes directly, so the source
        /// importer settings don't matter.
        /// </summary>
        static Texture2D ConvertMaskToAlpha(string sourcePath, string destPath, int posterizeSteps)
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException($"[VFX] Pack texture missing: {sourcePath} (see Assets/VFX/README.md for the packs the builders borrow from).");
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            src.LoadImage(File.ReadAllBytes(sourcePath));
            var pixels = src.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                float a = Posterize(Mathf.Max(p.r, Mathf.Max(p.g, p.b)), posterizeSteps);
                pixels[i] = new Color(1f, 1f, 1f, a);
            }
            var dst = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            dst.SetPixels(pixels);
            Object.DestroyImmediate(src);
            return SavePng(dst, destPath);
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

        // ---------------------------------------------------------------------
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
