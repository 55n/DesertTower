using DesertTower.VFX;
using UnityEditor;
using UnityEngine;
using static DesertTower.VFX.Editor.VfxBuildKit;

namespace DesertTower.VFX.Editor
{
    /// <summary>
    /// Beam effects built from the Hovl cylinder + VfxBeam (texture-offset scroll, no custom shader):
    /// #3 관통 마법 — 틸→흰 빔 along local +Z, cubes along its length, muzzle flash (spawn at the caster, aim +Z;
    /// gameplay sets VfxBeam.Length to the pierce range and plays VFX_ManaBolt_Impact on each enemy passed),
    /// #19 소환수 등장 — 틸 빛기둥 0.6 s + 링 + 플래시 + 라이트 (spawn at the summon point on the ground;
    /// the creature's own dissolve-in waits for the character meshes).
    /// </summary>
    public static class BeamVfxBuilder
    {
        public const string PierceBeamPath = PrefabDir + "/VFX_Pierce_Beam.prefab";
        public const string SummonPillarPath = PrefabDir + "/VFX_Summon_Pillar.prefab";

        [MenuItem("DesertTower/VFX/Build Pierce Beam")]
        public static void BuildPierceBeamFromMenu() => EditorGUIUtility.PingObject(BuildPierceBeam());

        [MenuItem("DesertTower/VFX/Build Summon Pillar")]
        public static void BuildSummonPillarFromMenu() => EditorGUIUtility.PingObject(BuildSummonPillar());

        // ------------------------------------------------------------------
        public static GameObject BuildPierceBeam()
        {
            var s = GetShared();
            var root = new GameObject("VFX_Pierce_Beam");
            AddHub(root);

            var beam = Beam(Child(root, "Beam"), s, length: 6f, radius: 0.14f, duration: 0.35f, scroll: 8f, color: Teal, intensity: 1.8f);
            beam.Width = new AnimationCurve(
                new Keyframe(0f, 0.3f, 0f, 10f), new Keyframe(0.12f, 1f, 0f, 0f), new Keyframe(0.5f, 0.85f, 0f, 0f), new Keyframe(1f, 0f, -5f, 0f));

            BuildBeamCubes(Child(root, "Cubes"), s, length: 6f);
            var flash = BuildFlash(Child(root, "MuzzleFlash"), s, 1.1f);
            ParticleLight(flash, s.LightPrefab, 3.5f, 3f, ratio: 1f);

            return SavePrefab(root, PierceBeamPath);
        }

        public static GameObject BuildSummonPillar()
        {
            var s = GetShared();
            var root = new GameObject("VFX_Summon_Pillar");
            AddHub(root);

            var pillarGo = Child(root, "Pillar");
            pillarGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // beam +Z → world up
            var beam = Beam(pillarGo, s, length: 4.5f, radius: 0.55f, duration: 0.6f, scroll: -5f, color: Teal, intensity: 1.5f);
            beam.Width = new AnimationCurve(
                new Keyframe(0f, 0.15f, 0f, 8f), new Keyframe(0.15f, 1f, 0f, 0f), new Keyframe(0.65f, 0.9f, 0f, 0f), new Keyframe(1f, 0f, -5f, 0f));

            BuildRing(Child(root, "Ring"), s, 3.0f);
            BuildRisingBurst(Child(root, "Rise"), s);
            var flash = BuildFlash(Child(root, "Flash"), s, 1.6f);
            flash.gameObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            ParticleLight(flash, s.LightPrefab, 4f, 5f, ratio: 1f);

            return SavePrefab(root, SummonPillarPath);
        }

        // ---- Pieces ---------------------------------------------------------

        static VfxBeam Beam(GameObject go, Shared s, float length, float radius, float duration, float scroll, Color color, float intensity)
        {
            var mr = UnitBeamMesh(go, s.Cylinder, s.Beam);
            var beam = go.AddComponent<VfxBeam>();
            beam.Target = mr;
            beam.Length = length;
            beam.Radius = radius;
            beam.Duration = duration;
            beam.ScrollSpeed = scroll;
            beam.Color = color;
            beam.Intensity = intensity;
            beam.StripesPerUnit = 1f;
            return beam;
        }

        // Cubes strewn along the beam's length, tumbling and fading teal → dark.
        static void BuildBeamCubes(GameObject go, Shared s, float length)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
            main.maxParticles = 48;
            RandomRotation3D(main);

            Burst(ps, 40);
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.25f, 0.25f, length);
            shape.position = new Vector3(0f, 0f, length * 0.5f);
            shape.randomDirectionAmount = 1f;
            Tumble(ps, Mathf.PI * 1.5f);
            Size(ps, HoldThenDrop(0.5f));
            ColorRamp(ps, Color.white, Teal, TealDark, 0.3f);

            UseMesh(r, s.Cube, s.CubeWhite);
        }

        static ParticleSystem BuildFlash(GameObject go, Shared s, float size)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1f;
            main.startLifetime = 0.12f;
            main.startSpeed = 0f;
            main.startSize = size;
            main.maxParticles = 2;

            Burst(ps, 1);
            Size(ps, HoldThenDrop(0.4f));

            UseBillboard(r, s.GlowWhite);
            r.sortingFudge = -10f;
            return ps;
        }

        static void BuildRing(GameObject go, Shared s, float diameter)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1f;
            main.startLifetime = 0.45f;
            main.startSpeed = 0f;
            main.startColor = Teal;
            main.startSize = FitScale(s.Torus, diameter);
            main.maxParticles = 2;
            var euler = AxisToUp(ThinAxis(s.Torus)).eulerAngles * Mathf.Deg2Rad;
            main.startRotation3D = true;
            main.startRotationX = euler.x;
            main.startRotationY = euler.y;
            main.startRotationZ = euler.z;

            Burst(ps, 1);
            Size(ps, EaseOut(0.15f, 1f));
            AlphaFade(ps, Color.white, 0.3f);

            UseMesh(r, s.Torus, s.MeshAdditive);
            go.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        }

        static void BuildRisingBurst(GameObject go, Shared s)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
            main.startColor = Teal;
            main.maxParticles = 32;
            RandomRotation3D(main);

            Burst(ps, 24);
            ConeShape(ps, 6f, 0.6f);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            Drag(ps, 0.2f, 3f);
            Tumble(ps);
            Size(ps, HoldThenDrop(0.6f));
            ColorRamp(ps, Color.white, Color.white, new Color(0.4f, 0.4f, 0.4f), 0.5f);

            UseMesh(r, s.Cube, s.CubeWhite);
        }
    }
}
