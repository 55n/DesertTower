using DesertTower.VFX;
using UnityEditor;
using UnityEngine;
using static DesertTower.VFX.Editor.VfxBuildKit;

namespace DesertTower.VFX.Editor
{
    /// <summary>
    /// Gold-palette celebration one-shots (SandGuard VFX 제작계획 v0.2):
    /// #7 레벨업 — 금 링 3겹 상승 + 금 큐브 나선 상승 + 플래시 + 라이트 (spawn at the player's feet),
    /// #27 웨이브 클리어 — 코어 금 링 3겹 확산 + 금 큐브 낙하(컨페티) + 플래시 (spawn at the core base).
    /// </summary>
    public static class GoldVfxBuilder
    {
        public const string LevelUpPath = PrefabDir + "/VFX_LevelUp.prefab";
        public const string WaveClearPath = PrefabDir + "/VFX_Wave_Clear.prefab";

        [MenuItem("DesertTower/VFX/Build Level Up")]
        public static void BuildLevelUpFromMenu() => EditorGUIUtility.PingObject(BuildLevelUp());

        [MenuItem("DesertTower/VFX/Build Wave Clear")]
        public static void BuildWaveClearFromMenu() => EditorGUIUtility.PingObject(BuildWaveClear());

        // ------------------------------------------------------------------
        public static GameObject BuildLevelUp()
        {
            var s = GetShared();
            var root = new GameObject("VFX_LevelUp");
            var flash = BuildFlash(root, s, height: 0.9f, size: 1.4f);
            ParticleLight(flash, s.LightPrefab, 4f, 3.5f, ratio: 1f);
            BuildRisingRings(Child(root, "Rings"), s);
            BuildSpiral(Child(root, "Spiral"), s);
            return SavePrefab(root, LevelUpPath);
        }

        public static GameObject BuildWaveClear()
        {
            var s = GetShared();
            var root = new GameObject("VFX_Wave_Clear");
            var ground = Child(root, "GroundPlane").transform;
            var flash = BuildFlash(root, s, height: 1.2f, size: 1.8f);
            ParticleLight(flash, s.LightPrefab, 5f, 6f, ratio: 1f);
            BuildExpandingRings(Child(root, "Rings"), s);
            BuildConfetti(Child(root, "Confetti"), s, ground);
            return SavePrefab(root, WaveClearPath);
        }

        // ---- Shared pieces ----------------------------------------------------

        // Root system: one gold-white flash sprite that also carries the light.
        static ParticleSystem BuildFlash(GameObject go, Shared s, float height, float size)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1.5f;
            main.startLifetime = 0.14f;
            main.startSpeed = 0f;
            main.startSize = size;
            main.startColor = new Color(1f, 0.95f, 0.75f);
            main.maxParticles = 2;

            Burst(ps, 1);
            SphereShape(ps, 0.01f);
            var shape = ps.shape;
            shape.position = new Vector3(0f, height, 0f);
            Size(ps, HoldThenDrop(0.4f));

            UseBillboard(r, s.GlowWhite);
            r.sortingFudge = -10f;
            return ps;
        }

        static void TorusParticle(ParticleSystem.MainModule main, Shared s, float diameter)
        {
            main.startSize = FitScale(s.Torus, diameter);
            var euler = AxisToUp(ThinAxis(s.Torus)).eulerAngles * Mathf.Deg2Rad;
            main.startRotation3D = true;
            main.startRotationX = euler.x;
            main.startRotationY = euler.y;
            main.startRotationZ = euler.z;
        }

        // #7: three rings, 0.1 s apart, rising from the feet and widening slightly.
        static void BuildRisingRings(GameObject go, Shared s)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1.5f;
            main.startLifetime = 0.7f;
            main.startSpeed = 2.6f;
            main.startColor = Gold;
            main.maxParticles = 4;
            TorusParticle(main, s, 1.5f);

            Bursts(ps, 1, 3, 0.1f);
            ConeShape(ps, 0f, 0.01f);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // rise along +Y
            Size(ps, EaseOut(0.6f, 1.15f));
            AlphaFade(ps, Color.white, 0.35f);

            UseMesh(r, s.Torus, s.MeshAdditive);
        }

        // #7: gold cubes spiralling up around the player (orbital + linear velocity in local space).
        static void BuildSpiral(GameObject go, Shared s)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.2f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.15f);
            main.startColor = Gold;
            main.maxParticles = 32;
            RandomRotation3D(main);

            Burst(ps, 24);
            CircleShape(ps, 0.55f, thickness: 0.2f, burstSpread: true);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // circle lies flat; local +Z = world up

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.orbitalZ = 7f;   // around the local normal (world up)
            vel.z = 2.4f;        // and up it

            Tumble(ps, Mathf.PI * 0.8f);
            Size(ps, HoldThenDrop(0.6f));
            ColorRamp(ps, Color.white, Color.white, GoldDark, 0.6f);

            UseMesh(r, s.Cube, s.CubeWhite);
        }

        // #27: three big ground rings, 0.12 s apart, expanding from the core.
        static void BuildExpandingRings(GameObject go, Shared s)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1.5f;
            main.startLifetime = 0.7f;
            main.startSpeed = 0f;
            main.startColor = Gold;
            main.maxParticles = 4;
            TorusParticle(main, s, 6f);

            Bursts(ps, 1, 3, 0.12f);
            Size(ps, EaseOut(0.15f, 1f));
            AlphaFade(ps, Color.white, 0.3f);

            UseMesh(r, s.Torus, s.MeshAdditive);
            go.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        }

        // #27: gold cubes raining from above the core and settling on the ground.
        static void BuildConfetti(GameObject go, Shared s, Transform ground)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 2.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f);
            main.startColor = Gold;
            main.gravityModifier = 0.35f;
            main.maxParticles = 48;
            RandomRotation3D(main);

            Bursts(ps, 14, 3, 0.15f);
            CircleShape(ps, 1.6f, thickness: 1f);
            go.transform.localPosition = new Vector3(0f, 4f, 0f);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // circle flat, emitting up a little
            Noise(ps, 0.35f, 0.4f, 0.5f);
            Tumble(ps);
            Size(ps, HoldThenDrop(0.85f));
            ColorRamp(ps, Color.white, Color.white, new Color(0.7f, 0.6f, 0.35f), 0.7f);
            GroundCollision(ps, ground);

            UseMesh(r, s.Cube, s.CubeWhite);
        }
    }
}
