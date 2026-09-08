using DesertTower.VFX;
using UnityEditor;
using UnityEngine;
using static DesertTower.VFX.Editor.VfxBuildKit;

namespace DesertTower.VFX.Editor
{
    /// <summary>
    /// #17 화염 코브라 화염 (SandGuard VFX 제작계획 v0.2): 주황·노랑 큐브 원뿔 분사 + 화염 스프라이트 코어
    /// + 입 포인트 라이트 깜빡임 + 불붙은 적에 불씨 루프 2s. Flame sprites come from Cartoon FX Remaster FREE
    /// (fire flipbook, stepped flame) converted by VfxBuildKit. The hit impact is ImpactVfxBuilder.FireImpact.
    /// </summary>
    public static class FlameBreathBuilder
    {
        public const string BreathPath = PrefabDir + "/VFX_FlameCobra_Breath.prefab";
        public const string BurningPath = PrefabDir + "/VFX_Burning_Loop.prefab";

        const float BurningDuration = 2f;

        [MenuItem("DesertTower/VFX/Build Flame Cobra Breath")]
        public static void BuildBreathFromMenu() => EditorGUIUtility.PingObject(BuildBreath());

        [MenuItem("DesertTower/VFX/Build Flame Cobra Breath + Place In Scene")]
        public static void BuildBreathAndPlace() => PlaceInScene(BuildBreath());

        [MenuItem("DesertTower/VFX/Build Burning Loop")]
        public static void BuildBurningFromMenu() => EditorGUIUtility.PingObject(BuildBurning());

        [MenuItem("DesertTower/VFX/Build Burning Loop + Place In Scene")]
        public static void BuildBurningAndPlace() => PlaceInScene(BuildBurning());

        // ------------------------------------------------------------------
        // Breath: looping cone along local +Z from the cobra's mouth. Play/Stop with the attack.
        // ------------------------------------------------------------------
        public static GameObject BuildBreath()
        {
            var s = GetShared();
            var root = new GameObject("VFX_FlameCobra_Breath");
            AddHub(root);

            BuildBreathFlames(Child(root, "Flames"), s);
            BuildBreathCubes(Child(root, "Cubes"), s);
            BuildBreathEmbers(Child(root, "Embers"), s);
            BuildMouthLight(Child(root, "Light"), new Vector3(0f, 0f, 0.4f), 3f, 4f);

            return SavePrefab(root, BreathPath);
        }

        // Animated fire blobs that grow as they travel (CFXR fire small anim, 8 frames over the lifetime).
        static void BuildBreathFlames(GameObject go, Shared s)
        {
            var ps = AddSystem(go, out var r, loop: true);
            var main = ps.main;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.7f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            main.maxParticles = 48;

            Rate(ps, 45f);
            ConeShape(ps, 12f, 0.08f);
            Flipbook(ps, FireFlipbookTiles, FireFlipbookFrames);
            Size(ps, EaseOut(0.45f, 1.2f));
            // Starts pale yellow rather than white so the dense root of the stream doesn't blow out under Bloom.
            ColorRampFade(ps, new Color(1f, 0.95f, 0.75f), Yellow, Orange, 0.3f, 0.55f);

            UseBillboard(r, s.FireFlipbook);
        }

        // Yellow → orange → dark cubes riding the stream and drifting up as they cool.
        static void BuildBreathCubes(GameObject go, Shared s)
        {
            var ps = AddSystem(go, out var r, loop: true);
            var main = ps.main;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
            main.gravityModifier = -0.25f;
            main.maxParticles = 40;
            RandomRotation3D(main);

            Rate(ps, 28f);
            ConeShape(ps, 14f, 0.05f);
            Drag(ps, 0.15f, 3f);
            Tumble(ps);
            Size(ps, HoldThenDrop(0.5f));
            ColorRamp(ps, Yellow, Orange, RedDark, 0.4f);

            UseMesh(r, s.Cube, s.CubeWhite);
        }

        // Sparse embers that peel off the stream and float up.
        static void BuildBreathEmbers(GameObject go, Shared s)
        {
            var ps = AddSystem(go, out var r, loop: true);
            var main = ps.main;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
            main.startColor = Orange;
            main.gravityModifier = -0.15f;
            main.maxParticles = 24;

            Rate(ps, 14f);
            ConeShape(ps, 20f, 0.1f);
            Noise(ps, 0.4f, 0.6f, 0.8f);
            Size(ps, Linear(1f, 0f));
            AlphaFade(ps, Color.white, 0.4f);

            UseBillboard(r, s.GlowWhite);
        }

        static void BuildMouthLight(GameObject go, Vector3 localPos, float intensity, float range)
        {
            go.transform.localPosition = localPos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Orange;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            var flicker = go.AddComponent<VfxLightFlicker>();
            flicker.Amplitude = 0.35f;
            flicker.Speed = 9f;
        }

        // ------------------------------------------------------------------
        // Burning: 2-second one-shot on an enemy that was hit. Spawn at the feet; flames rise through
        // the body volume. The light stays on, so destroy the instance ~1s after the systems finish.
        // ------------------------------------------------------------------
        public static GameObject BuildBurning()
        {
            var s = GetShared();
            var root = new GameObject("VFX_Burning_Loop");
            var hub = AddHub(root);
            var hubMain = hub.main;
            hubMain.loop = false;
            hubMain.duration = BurningDuration;

            BuildBurningFlames(Child(root, "Flames"), s);
            BuildBurningCubes(Child(root, "Cubes"), s);
            BuildBurningEmbers(Child(root, "Embers"), s);
            BuildMouthLight(Child(root, "Light"), new Vector3(0f, 1.0f, 0f), 2f, 3f);

            return SavePrefab(root, BurningPath);
        }

        // Stepped flame sprites standing up through the character box.
        static void BuildBurningFlames(GameObject go, Shared s)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = BurningDuration;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(0.3f, 0.42f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(0.6f, 0.84f);
            main.startSizeZ = 1f;
            main.maxParticles = 32;

            Rate(ps, 18f);
            BodyBox(ps, go);
            Size(ps, InOut(0.2f, 0.5f));
            ColorRampFade(ps, Color.white, Yellow, Orange, 0.3f, 0.5f);

            UseBillboard(r, s.Flame);
            r.pivot = new Vector3(0f, 0.5f, 0f); // base on the emission point
        }

        static void BuildBurningCubes(GameObject go, Shared s)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = BurningDuration;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.maxParticles = 24;
            RandomRotation3D(main);

            Rate(ps, 9f);
            BodyBox(ps, go);
            Tumble(ps);
            Size(ps, HoldThenDrop(0.5f));
            ColorRamp(ps, Yellow, Orange, RedDark, 0.4f);

            UseMesh(r, s.Cube, s.CubeWhite);
        }

        static void BuildBurningEmbers(GameObject go, Shared s)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = BurningDuration;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.12f);
            main.startColor = Orange;
            main.maxParticles = 32;

            Rate(ps, 14f);
            BodyBox(ps, go);
            Noise(ps, 0.3f, 0.5f, 0.6f);
            Size(ps, Linear(1f, 0f));
            AlphaFade(ps, Color.white, 0.4f);

            UseBillboard(r, s.GlowWhite);
        }

        // Emit upward from the *surface* of a box slightly larger than a 1.8-unit character, so the
        // flames sit on the skin instead of being hidden inside the mesh (the box's local +Z becomes world +Y).
        static void BodyBox(ParticleSystem ps, GameObject go)
        {
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.BoxShell;
            shape.scale = new Vector3(0.75f, 0.75f, 1.5f);
            go.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        }
    }
}
