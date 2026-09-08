using DesertTower.VFX;
using UnityEditor;
using UnityEngine;
using static DesertTower.VFX.Editor.VfxBuildKit;

namespace DesertTower.VFX.Editor
{
    /// <summary>
    /// Looping ambient / state effects (SandGuard VFX 제작계획 v0.2):
    /// #18 소환진 대기 — 바닥 원 2겹 역회전 + 틸 큐브 상승 + 룬 글로우 펄스 + 라이트,
    /// #10 마나 충전 — 코어→램프로 흐르는 틸 큐브 스트림 (attractor Target = the lamp),
    /// #36 횃불 — 주황 큐브 + 불꽃 스프라이트 + 깜빡이는 라이트,
    /// #38 떠다니는 모래 먼지 — 씬 전체 베이지 작은 큐브 부유.
    /// </summary>
    public static class LoopVfxBuilder
    {
        public const string SummonCirclePath = PrefabDir + "/VFX_Summon_Circle.prefab";
        public const string ManaChargePath = PrefabDir + "/VFX_Mana_Charge.prefab";
        public const string TorchPath = PrefabDir + "/VFX_Torch.prefab";
        public const string FloatingDustPath = PrefabDir + "/VFX_Floating_Dust.prefab";

        [MenuItem("DesertTower/VFX/Build Summon Circle")]
        public static void BuildSummonCircleFromMenu() => EditorGUIUtility.PingObject(BuildSummonCircle());

        [MenuItem("DesertTower/VFX/Build Mana Charge")]
        public static void BuildManaChargeFromMenu() => EditorGUIUtility.PingObject(BuildManaCharge());

        [MenuItem("DesertTower/VFX/Build Torch")]
        public static void BuildTorchFromMenu() => EditorGUIUtility.PingObject(BuildTorch());

        [MenuItem("DesertTower/VFX/Build Floating Dust")]
        public static void BuildFloatingDustFromMenu() => EditorGUIUtility.PingObject(BuildFloatingDust());

        // ------------------------------------------------------------------
        // #18 Summon circle: place on the ground where the magic circle is set.
        // ------------------------------------------------------------------
        public static GameObject BuildSummonCircle()
        {
            var s = GetShared();
            var root = new GameObject("VFX_Summon_Circle");
            AddHub(root);

            var outer = Child(root, "Circle Outer");
            outer.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            GroundQuad(outer, s.Quad, s.MagicCircleTeal, 2.6f, 18f);

            var inner = Child(root, "Circle Inner");
            inner.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            GroundQuad(inner, s.Quad, s.MagicCircleTeal, 1.7f, -30f);

            BuildRise(Child(root, "Rise"), s, radius: 1.0f, rate: 8f, color: Teal);
            BuildGlowPulse(Child(root, "Glow"), s, new Vector3(0f, 0.6f, 0f), 1.4f, 2.0f, Teal);
            BuildLight(Child(root, "Light"), new Vector3(0f, 0.9f, 0f), Teal, 2f, 4f, flickerAmp: 0.2f, flickerSpeed: 2f);

            return SavePrefab(root, SummonCirclePath);
        }

        // ------------------------------------------------------------------
        // #10 Mana charge: place at the core base; set VfxParticleAttractor.Target to the lamp. Play while
        // the player stands in the core radius; stop when charged, then play VFX_Mana_Charge_Complete at the lamp.
        // ------------------------------------------------------------------
        public static GameObject BuildManaCharge()
        {
            var s = GetShared();
            var root = new GameObject("VFX_Mana_Charge");
            AddHub(root);

            var stream = Child(root, "Stream");
            stream.transform.localPosition = new Vector3(0f, 1.2f, 0f); // from the crystal
            var ps = AddSystem(stream, out var r, loop: true);
            var main = ps.main;
            main.duration = 1f;
            main.startLifetime = 3f;                 // the attractor consumes them long before this
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
            main.startColor = Teal;
            main.maxParticles = 80;
            RandomRotation3D(main);

            Rate(ps, 22f);
            SphereShape(ps, 0.5f);
            Tumble(ps, Mathf.PI * 1.2f);
            Size(ps, InOut(0.1f, 0.9f));
            ColorRamp(ps, Color.white, Color.white, new Color(0.8f, 0.8f, 0.8f), 0.3f);
            UseMesh(r, s.Cube, s.CubeWhite);

            var attractor = stream.AddComponent<VfxParticleAttractor>();
            attractor.Delay = 0.2f;
            attractor.Acceleration = 28f;
            attractor.MaxSpeed = 9f;
            attractor.ArriveRadius = 0.3f;
            attractor.MaxPullTime = 3f;
            attractor.TargetOffset = new Vector3(0f, 1.0f, 0f); // lamp head

            BuildGlowPulse(Child(root, "Glow"), s, new Vector3(0f, 1.2f, 0f), 1.2f, 1.6f, Teal);

            return SavePrefab(root, ManaChargePath);
        }

        // ------------------------------------------------------------------
        // #36 Torch: place at the torch head. One prefab, reused everywhere.
        // ------------------------------------------------------------------
        public static GameObject BuildTorch()
        {
            var s = GetShared();
            var root = new GameObject("VFX_Torch");
            AddHub(root);

            // Stepped flame sprites standing on the head.
            {
                var ps = AddSystem(Child(root, "Flame"), out var r, loop: true);
                var main = ps.main;
                main.duration = 1f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.5f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
                main.startSize3D = true;
                main.startSizeX = new ParticleSystem.MinMaxCurve(0.2f, 0.26f);
                main.startSizeY = new ParticleSystem.MinMaxCurve(0.4f, 0.52f);
                main.startSizeZ = 1f;
                main.maxParticles = 12;
                main.prewarm = true;

                Rate(ps, 10f);
                ConeShape(ps, 6f, 0.05f);
                ps.gameObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                Size(ps, InOut(0.2f, 0.5f));
                ColorRampFade(ps, new Color(1f, 0.95f, 0.75f), Yellow, Orange, 0.3f, 0.5f);
                UseBillboard(r, s.Flame);
                r.pivot = new Vector3(0f, 0.5f, 0f);
            }
            // Orange cubes drifting up.
            {
                var ps = AddSystem(Child(root, "Cubes"), out var r, loop: true);
                var main = ps.main;
                main.duration = 1f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.07f);
                main.gravityModifier = -0.1f;
                main.maxParticles = 12;
                main.prewarm = true;
                RandomRotation3D(main);

                Rate(ps, 6f);
                ConeShape(ps, 12f, 0.06f);
                ps.gameObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                Noise(ps, 0.2f, 0.6f, 0.6f);
                Tumble(ps);
                Size(ps, HoldThenDrop(0.5f));
                ColorRamp(ps, Yellow, Orange, RedDark, 0.4f);
                UseMesh(r, s.Cube, s.CubeWhite);
            }
            BuildLight(Child(root, "Light"), new Vector3(0f, 0.25f, 0f), Orange, 1.6f, 4f, flickerAmp: 0.4f, flickerSpeed: 12f);

            return SavePrefab(root, TorchPath);
        }

        // ------------------------------------------------------------------
        // #38 Floating dust: one instance at the level centre; a 20×6×20 box of slow beige motes.
        // ------------------------------------------------------------------
        public static GameObject BuildFloatingDust()
        {
            var s = GetShared();
            var root = new GameObject("VFX_Floating_Dust");
            var ps = AddSystem(root, out var r, loop: true);
            var main = ps.main;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startColor = new Color(0.42f, 0.34f, 0.22f); // dim: below the bloom threshold
            main.maxParticles = 220;
            main.prewarm = true;
            RandomRotation3D(main);

            Rate(ps, 25f);
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 6f, 20f);
            shape.position = new Vector3(0f, 3f, 0f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.08f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            Noise(ps, 0.25f, 0.15f, 0.2f);
            Tumble(ps, Mathf.PI * 0.3f);
            Size(ps, InOut(0.1f, 0.9f));

            UseMesh(r, s.Cube, s.CubeWhite);
            return SavePrefab(root, FloatingDustPath);
        }

        // ---- Shared pieces ----------------------------------------------------

        static ParticleSystem BuildRise(GameObject go, Shared s, float radius, float rate, Color color)
        {
            var ps = AddSystem(go, out var r, loop: true);
            var main = ps.main;
            main.duration = 3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f);
            main.startColor = color;
            main.maxParticles = 48;
            main.prewarm = true;
            RandomRotation3D(main);

            Rate(ps, rate);
            ConeShape(ps, 0f, radius);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            Noise(ps, 0.15f, 0.4f, 0.3f);
            Tumble(ps, Mathf.PI * 0.5f);
            Size(ps, InOut(0.15f, 0.7f));
            ColorRamp(ps, Color.white, Color.white, new Color(0.4f, 0.4f, 0.4f), 0.5f);

            UseMesh(r, s.Cube, s.CubeWhite);
            return ps;
        }

        static ParticleSystem BuildGlowPulse(GameObject go, Shared s, Vector3 localPos, float sizeMin, float sizeMax, Color color)
        {
            go.transform.localPosition = localPos;
            var ps = AddSystem(go, out var r, loop: true);
            var main = ps.main;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.6f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = color;
            main.maxParticles = 6;
            main.prewarm = true;

            Rate(ps, 5f);
            Size(ps, InOut(0.3f, 0.6f));
            AlphaFade(ps, Color.white, 0.4f);

            UseBillboard(r, s.GlowSoft);
            return ps;
        }

        static Light BuildLight(GameObject go, Vector3 localPos, Color color, float intensity, float range, float flickerAmp, float flickerSpeed)
        {
            go.transform.localPosition = localPos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            var flicker = go.AddComponent<VfxLightFlicker>();
            flicker.Amplitude = flickerAmp;
            flicker.Speed = flickerSpeed;
            return light;
        }
    }
}
