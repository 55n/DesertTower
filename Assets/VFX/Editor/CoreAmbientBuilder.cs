using DesertTower.VFX;
using UnityEditor;
using UnityEngine;
using static DesertTower.VFX.Editor.VfxBuildKit;

namespace DesertTower.VFX.Editor
{
    /// <summary>
    /// #9 코어 앰비언트 (SandGuard VFX 제작계획 v0.2): 코어 크리스탈 이미시브 펄스 + 틸 큐브 느린 상승 루프
    /// + 궤도 도는 큐브 5개 + 글로우 + 포인트 라이트. 안정도 3단계(틸/노랑/빨강+연기)는 CoreAmbientVfx가 구동한다.
    /// Loop-type prefab (§3 Base Loop + Glow + Sparks). Place at the core's base; the crystal floats at y = 1.2.
    /// </summary>
    public static class CoreAmbientBuilder
    {
        public const string PrefabPath = PrefabDir + "/VFX_Core_Ambient.prefab";

        const float CrystalHeight = 1.2f;
        const float OrbitRadius = 1.2f;
        const float OrbitPeriod = 6f;

        [MenuItem("DesertTower/VFX/Build Core Ambient")]
        public static void BuildFromMenu() => EditorGUIUtility.PingObject(Build());

        [MenuItem("DesertTower/VFX/Build Core Ambient + Place In Scene")]
        public static void BuildAndPlace() => PlaceInScene(Build());

        public static GameObject Build()
        {
            var s = GetShared();
            var root = new GameObject("VFX_Core_Ambient");
            AddHub(root);

            var crystal = BuildCrystal(Child(root, "Crystal"), s);
            var rise = BuildRise(Child(root, "Rise"), s);
            var orbit = BuildOrbit(Child(root, "Orbit"), s);
            var glow = BuildGlow(Child(root, "Glow"), s);
            var smoke = BuildSmoke(Child(root, "Smoke"), s);
            var light = BuildLight(Child(root, "Light"));

            var ctrl = root.AddComponent<CoreAmbientVfx>();
            ctrl.Crystal = crystal;
            ctrl.Light = light;
            ctrl.ColoredSystems = new[] { rise, orbit, glow };
            ctrl.Smoke = smoke;
            ctrl.Stable = new CoreAmbientVfx.Stage
            {
                Name = "Stable", Color = Teal, Speed = 1f, PulseHz = 0.8f,
                Pulse = new Vector2(0.7f, 1.2f), SmokeRate = 0f, LightIntensity = 2f,
            };
            ctrl.Strained = new CoreAmbientVfx.Stage
            {
                Name = "Strained", Color = Yellow, Speed = 1.6f, PulseHz = 1.8f,
                Pulse = new Vector2(0.6f, 1.4f), SmokeRate = 0f, LightIntensity = 2.5f,
            };
            ctrl.Critical = new CoreAmbientVfx.Stage
            {
                Name = "Critical", Color = Red, Speed = 2.4f, PulseHz = 3.5f,
                Pulse = new Vector2(0.4f, 1.6f), SmokeRate = 8f, LightIntensity = 3f,
            };

            return SavePrefab(root, PrefabPath);
        }

        // Floating crystal (Hovl Crystal1, fitted to 1 unit tall and centred so it spins about its own axis).
        // Colour comes from CoreAmbientVfx via a property block.
        static Renderer BuildCrystal(GameObject go, Shared s)
        {
            go.transform.localPosition = new Vector3(0f, CrystalHeight, 0f);
            var spin = go.AddComponent<VfxSpin>();
            spin.DegreesPerSecond = new Vector3(0f, 40f, 0f);

            var mesh = s.Crystal;
            float scale = FitScale(mesh, 1.0f);
            var rot = AxisToUp(LongAxis(mesh));
            var pivot = Child(go, "Mesh");
            pivot.transform.localRotation = rot;
            pivot.transform.localScale = Vector3.one * scale;
            pivot.transform.localPosition = -(rot * (mesh.bounds.center * scale));

            var mf = pivot.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = pivot.AddComponent<MeshRenderer>();
            mr.sharedMaterial = s.CubeWhite;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mr;
        }

        // Slow columns of cubes rising from a disc around the base.
        static ParticleSystem BuildRise(GameObject go, Shared s)
        {
            var ps = AddSystem(go, out var r, loop: true);
            var main = ps.main;
            main.duration = 3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
            main.startColor = Teal;
            main.maxParticles = 40;
            main.prewarm = true;
            RandomRotation3D(main);

            Rate(ps, 6f);
            ConeShape(ps, 0f, 0.9f);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // cone points up
            Noise(ps, 0.15f, 0.4f, 0.3f);
            Tumble(ps, Mathf.PI * 0.5f);
            Size(ps, InOut(0.15f, 0.7f));
            ColorRamp(ps, Color.white, Color.white, new Color(0.4f, 0.4f, 0.4f), 0.5f);

            UseMesh(r, s.Cube, s.CubeWhite);
            return ps;
        }

        // Five cubes on a ring, evenly spaced (BurstSpread), orbiting exactly one revolution per lifetime
        // so the replacement burst lands where the old cubes died and the loop is seamless.
        static ParticleSystem BuildOrbit(GameObject go, Shared s)
        {
            go.transform.localPosition = new Vector3(0f, CrystalHeight, 0f);
            var ps = AddSystem(go, out var r, loop: true);
            var main = ps.main;
            main.duration = OrbitPeriod;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = OrbitPeriod;
            main.startSpeed = 0f;
            main.startSize = 0.14f;
            main.startColor = Teal;
            main.maxParticles = 10;
            main.prewarm = true;
            RandomRotation3D(main);

            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5, 5, 0, OrbitPeriod) });

            CircleShape(ps, OrbitRadius, thickness: 0f, burstSpread: true);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // circle lies flat

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.orbitalZ = Mathf.PI * 2f / OrbitPeriod; // orbit around the circle's normal (local Z, world Y after the tilt)

            Tumble(ps, Mathf.PI * 0.4f);
            Size(ps, InOut(0.08f, 0.92f));

            UseMesh(r, s.Cube, s.CubeWhite);
            return ps;
        }

        // Soft halo around the crystal.
        static ParticleSystem BuildGlow(GameObject go, Shared s)
        {
            go.transform.localPosition = new Vector3(0f, CrystalHeight, 0f);
            var ps = AddSystem(go, out var r, loop: true);
            var main = ps.main;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.6f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(1.4f, 1.8f);
            main.startColor = Teal;
            main.maxParticles = 6;
            main.prewarm = true;

            Rate(ps, 5f);
            Size(ps, InOut(0.3f, 0.6f));
            AlphaFade(ps, Color.white, 0.4f);

            UseBillboard(r, s.GlowSoft);
            return ps;
        }

        // Dark smoke, emission rate driven by CoreAmbientVfx (0 unless critical).
        static ParticleSystem BuildSmoke(GameObject go, Shared s)
        {
            go.transform.localPosition = new Vector3(0f, CrystalHeight * 0.5f, 0f);
            var ps = AddSystem(go, out var r, loop: true);
            var main = ps.main;
            main.duration = 2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startColor = new Color(0.22f, 0.17f, 0.19f, 0.7f); // dark and a little translucent: wisps, not blobs
            main.maxParticles = 40;

            Rate(ps, 0f);
            ConeShape(ps, 15f, 0.45f);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // cone points up
            Noise(ps, 0.25f, 0.3f, 0.4f);
            Size(ps, EaseOut(0.5f, 1.2f));
            AlphaFade(ps, Color.white, 0.2f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            RandomTile(ps, 2, 2); // CFXR smoke cloud x4

            UseBillboard(r, s.Smoke);
            return ps;
        }

        static Light BuildLight(GameObject go)
        {
            go.transform.localPosition = new Vector3(0f, CrystalHeight, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Teal;
            light.range = 4.5f;
            light.intensity = 2f;
            light.shadows = LightShadows.None;
            return light;
        }
    }
}
