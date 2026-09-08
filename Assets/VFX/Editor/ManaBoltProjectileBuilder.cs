using DesertTower.VFX;
using UnityEditor;
using UnityEngine;
using static DesertTower.VFX.Editor.VfxBuildKit;

namespace DesertTower.VFX.Editor
{
    /// <summary>
    /// #1 마나탄 투사체 (SandGuard VFX 제작계획 v0.2): 틸 큐브 코어 + 글로우 스프라이트 + 큐브 트레일 + 포인트 라이트,
    /// 발사 시 총구 플래시. The prefab's local +Z is the travel direction; parent it to (or move it with)
    /// the projectile. The muzzle flash is a play-on-awake burst, so it fires once at spawn.
    /// Pairs with VFX_ManaBolt_Impact on hit.
    /// </summary>
    public static class ManaBoltProjectileBuilder
    {
        public const string PrefabPath = PrefabDir + "/VFX_ManaBolt_Projectile.prefab";

        [MenuItem("DesertTower/VFX/Build Mana Bolt Projectile")]
        public static void BuildFromMenu() => EditorGUIUtility.PingObject(Build());

        [MenuItem("DesertTower/VFX/Build Mana Bolt Projectile + Place In Scene")]
        public static void BuildAndPlace() => PlaceInScene(Build());

        public static GameObject Build()
        {
            var s = GetShared();
            var root = new GameObject("VFX_ManaBolt_Projectile");
            AddHub(root);

            BuildCore(Child(root, "Core"), s);
            BuildGlow(Child(root, "Glow"), s);
            BuildTrail(Child(root, "Trail"), s);
            BuildLight(Child(root, "Light"));
            BuildMuzzleFlash(Child(root, "MuzzleFlash"), s);
            BuildMuzzleSparks(Child(root, "MuzzleSparks"), s);

            return SavePrefab(root, PrefabPath);
        }

        // Solid tumbling cube. A MeshRenderer, not a particle, so it never flickers and always sits at the origin.
        static void BuildCore(GameObject go, Shared s)
        {
            go.transform.localScale = Vector3.one * 0.28f;
            go.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = s.Cube;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = s.CubeTeal;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var spin = go.AddComponent<VfxSpin>();
            spin.DegreesPerSecond = new Vector3(0f, 420f, 260f);
        }

        // Persistent additive halo: a short-lived sprite re-emitted continuously in local space.
        static void BuildGlow(GameObject go, Shared s)
        {
            var ps = AddSystem(go, out var r, loop: true);
            var main = ps.main;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.1f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.0f);
            main.startColor = Teal;
            main.maxParticles = 4;
            main.prewarm = true;

            Rate(ps, 20f);
            Size(ps, InOut(0.3f, 0.7f));

            UseBillboard(r, s.GlowWhite);
        }

        // Cubes dropped behind the bolt: rate over distance so a fast bolt leaves a denser tail.
        static void BuildTrail(GameObject go, Shared s)
        {
            var ps = AddSystem(go, out var r, loop: true);
            var main = ps.main;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.45f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.16f);
            main.maxParticles = 48;
            RandomRotation3D(main);

            Rate(ps, perSecond: 6f, perUnit: 2.5f);
            SphereShape(ps, 0.08f);
            Tumble(ps, Mathf.PI * 1.5f);
            Size(ps, Linear(1f, 0f));
            ColorRamp(ps, Color.white, Teal, TealDark, 0.25f);

            UseMesh(r, s.Cube, s.CubeWhite);
        }

        static void BuildLight(GameObject go)
        {
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Teal;
            light.range = 3f;
            light.intensity = 3f;
            light.shadows = LightShadows.None;
        }

        // One-shot at spawn: a white sprite pop.
        static void BuildMuzzleFlash(GameObject go, Shared s)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = 0.08f;
            main.startSpeed = 0f;
            main.startSize = 0.9f;
            main.maxParticles = 2;

            Burst(ps, 1);
            Size(ps, HoldThenDrop(0.4f));

            UseBillboard(r, s.GlowWhite);
            r.sortingFudge = -10f;
        }

        // One-shot at spawn: cubes thrown forward along +Z that the bolt then overtakes.
        static void BuildMuzzleSparks(GameObject go, Shared s)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.1f);
            main.maxParticles = 12;
            RandomRotation3D(main);

            Burst(ps, 8);
            ConeShape(ps, 25f, 0.05f); // cone emits along local +Z = travel direction
            Drag(ps, 0.3f, 2f);
            Tumble(ps);
            Size(ps, Linear(1f, 0f));
            ColorRamp(ps, Color.white, Teal, TealDark, 0.3f);

            UseMesh(r, s.Cube, s.CubeWhite);
        }
    }
}
