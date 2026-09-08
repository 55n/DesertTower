using DesertTower.VFX;
using UnityEditor;
using UnityEngine;
using static DesertTower.VFX.Editor.VfxBuildKit;

namespace DesertTower.VFX.Editor
{
    /// <summary>
    /// Parameters for one impact-type effect. The layer structure is fixed (§3: Flash, Core,
    /// Shockwave, Debris, Ember + light); a spec only decides colours, counts and sizes.
    /// <see cref="Scale"/> = 1 is the mana-bolt baseline (radius 0.6, §2 "일반 피격").
    /// </summary>
    public enum DebrisShape { Sphere, ConeUp, Box }

    public sealed class ImpactSpec
    {
        public string PrefabName;
        public float Scale = 1f;

        // Origin. Impacts spawn at the hit point (0). Ground effects spawn at the feet: lift the
        // flash/core/ember to body height and let the debris collide with the local y = 0 plane.
        public float BodyHeight = 0f;
        public bool GroundCollision;

        // 1. Flash — one white sprite, ~0.1s, drawn on top.
        public float FlashSize = 1.4f;
        public Color FlashColor = Color.white;

        // 2. Core — a few tumbling cubes that pop and vanish. Carries the light.
        public int CoreCount = 4;
        public float CoreSize = 0.35f;
        public Color CoreA = Color.white, CoreB = Teal, CoreC = Teal;

        // 3. Shockwave — ground ring, ease-out expansion. Count > 1 stacks delayed rings.
        public bool Shockwave = true;
        public float ShockwaveSize = 2.2f;
        public Color ShockwaveColor = Color.white;
        public int ShockwaveCount = 1;
        public float ShockwaveInterval = 0.12f;

        // 4. Debris — radial cubes with gravity.
        public int DebrisCount = 24;
        public Vector2 DebrisSize = new Vector2(0.07f, 0.15f);
        public Vector2 DebrisSpeed = new Vector2(2f, 4f);
        public Vector2 DebrisLife = new Vector2(0.5f, 0.8f);
        public float DebrisGravity = 0.9f;
        public Color DebrisStart = Color.white;   // start colour (what VfxTint replaces)
        public Color DebrisA = Color.white, DebrisB = Teal, DebrisC = TealDark;
        public DebrisShape DebrisShape = DebrisShape.Sphere;
        public float DebrisConeAngle = 55f;                          // ConeUp only
        public Vector3 DebrisBox = new Vector3(0.6f, 1.6f, 0.6f);    // Box only: the character volume, centred at BodyHeight
        public bool TintableDebris;               // adds VfxTint so gameplay can recolour the debris
        public bool DebrisAttractor;              // adds VfxParticleAttractor on the debris (enemy pulled into the core)

        // 5. Ember / afterglow — slow rising sprites (or cubes, or CFXR stepped flames) that linger.
        public int EmberCount = 14;
        public bool EmberCubes;
        public bool EmberFlame;                   // stepped flame sprites (fire impact); EmberSize is the flame width, height = 2×
        public Vector2 EmberSize = new Vector2(0.12f, 0.22f);
        public Vector2 EmberSpeed = new Vector2(0.3f, 0.8f);
        public Vector2 EmberLife = new Vector2(1.0f, 1.5f);
        public Color EmberColor = Teal;
        public bool EmberAttractor;               // adds VfxParticleAttractor (XP cubes flying to the player)
        public float EmberConeAngle = -1f;        // -1 = default (15° cubes / 30° sprites); small = column

        // 6. Dust — CFXR smoke tiles, alpha-blended, rising slowly (sand clouds). 0 = layer omitted.
        public int DustCount = 0;
        public Color DustColor = new Color(0.62f, 0.50f, 0.32f, 0.85f);
        public Vector2 DustSize = new Vector2(0.5f, 0.9f);
        public Vector2 DustSpeed = new Vector2(0.6f, 1.2f);
        public Vector2 DustLife = new Vector2(0.8f, 1.4f);

        // Light (colour follows the core particles: white flash → CoreB)
        public float LightIntensity = 4f;
        public float LightRange = 3f;
    }

    /// <summary>
    /// Builds the 5-layer impact prefabs from an <see cref="ImpactSpec"/>:
    /// #2 Mana Bolt Impact, #23 Enemy Death, #14 Build Complete (SandGuard VFX 제작계획 v0.2).
    /// Safe to re-run: existing assets are overwritten in place so GUIDs stay stable.
    /// </summary>
    public static class ImpactVfxBuilder
    {
        public const string ManaBoltImpactPath = PrefabDir + "/VFX_ManaBolt_Impact.prefab";
        public const string EnemyDeathPath = PrefabDir + "/VFX_Enemy_Death.prefab";
        public const string BuildCompletePath = PrefabDir + "/VFX_Build_Complete.prefab";
        public const string FireImpactPath = PrefabDir + "/VFX_Fire_Impact.prefab";
        public const string SandBurstPath = PrefabDir + "/VFX_Sand_Burst.prefab";
        public const string EnemySpawnPath = PrefabDir + "/VFX_Enemy_Spawn.prefab";
        public const string ManaChargeCompletePath = PrefabDir + "/VFX_Mana_Charge_Complete.prefab";
        public const string CoreDamageEnemyPath = PrefabDir + "/VFX_Core_Damage_Enemy.prefab";
        public const string CoreDamageFlashPath = PrefabDir + "/VFX_Core_Damage_Flash.prefab";

        /// <summary>Every impact spec with its prefab path, in build order.</summary>
        public static (ImpactSpec spec, string path)[] All() => new[]
        {
            (ManaBoltImpact(), ManaBoltImpactPath),
            (EnemyDeath(), EnemyDeathPath),
            (BuildComplete(), BuildCompletePath),
            (FireImpact(), FireImpactPath),
            (SandBurst(), SandBurstPath),
            (EnemySpawn(), EnemySpawnPath),
            (ManaChargeComplete(), ManaChargeCompletePath),
            (CoreDamageEnemy(), CoreDamageEnemyPath),
            (CoreDamageFlash(), CoreDamageFlashPath),
        };

        // ---- Specs ----------------------------------------------------------

        /// <summary>#2 마나탄 착탄: Impact_Base 틸, 반경 0.6.</summary>
        public static ImpactSpec ManaBoltImpact() => new ImpactSpec
        {
            PrefabName = "VFX_ManaBolt_Impact",
        };

        /// <summary>
        /// #23 적 사망: 캐릭터 색 큐브 15개 낙하 + 작은 플래시 + 경험치 금 큐브가 플레이어로 흡수.
        /// Spawn at the enemy's feet. The body volume crumbles into cubes that land on the ground;
        /// call VfxTint.Apply(enemyColor) and set the attractor Target to the player.
        /// </summary>
        public static ImpactSpec EnemyDeath() => new ImpactSpec
        {
            PrefabName = "VFX_Enemy_Death",
            BodyHeight = 0.9f,
            GroundCollision = true,
            FlashSize = 0.9f,
            CoreCount = 2, CoreSize = 0.25f, CoreA = Color.white, CoreB = Beige, CoreC = BeigeDark,
            Shockwave = false,
            DebrisCount = 15,
            DebrisSize = new Vector2(0.14f, 0.24f),
            DebrisSpeed = new Vector2(0.6f, 1.8f),
            DebrisLife = new Vector2(0.9f, 1.3f),
            DebrisGravity = 1.0f,
            DebrisStart = Beige,                     // default enemy tint; VfxTint.Apply() overrides
            // Character-coloured, not glowing: the ramp keeps the HDR cube material below the bloom threshold.
            DebrisA = new Color(0.55f, 0.55f, 0.55f), DebrisB = new Color(0.45f, 0.45f, 0.45f), DebrisC = new Color(0.2f, 0.2f, 0.2f),
            DebrisShape = DebrisShape.Box,
            TintableDebris = true,
            EmberCount = 6, EmberCubes = true,
            EmberSize = new Vector2(0.1f, 0.14f),
            EmberSpeed = new Vector2(0.8f, 1.4f),
            EmberLife = new Vector2(1.4f, 1.8f),
            EmberColor = Gold,
            EmberAttractor = true,
            LightIntensity = 2.5f, LightRange = 2.5f,
        };

        /// <summary>
        /// #14 건설 완료: 모래 링 확산 + 금 큐브 상승 + 플래시. Spawn at the slot centre on the ground.
        /// (시설 솟아오름은 VfxScaleIn을 시설에 붙인다.)
        /// </summary>
        public static ImpactSpec BuildComplete() => new ImpactSpec
        {
            PrefabName = "VFX_Build_Complete",
            Scale = 1.3f,
            BodyHeight = 0.5f,
            GroundCollision = true,
            FlashSize = 1.6f, FlashColor = new Color(1f, 0.97f, 0.85f),
            CoreCount = 3, CoreSize = 0.28f, CoreA = Color.white, CoreB = Gold, CoreC = Gold,
            Shockwave = true, ShockwaveSize = 2.8f, ShockwaveColor = Beige,
            // Sand puff: dim, wide and low.
            DebrisCount = 30,
            DebrisSize = new Vector2(0.06f, 0.12f),
            DebrisSpeed = new Vector2(1.2f, 2.2f),
            DebrisLife = new Vector2(0.5f, 0.8f),
            DebrisGravity = 0.8f,
            DebrisA = Beige * 0.6f, DebrisB = BeigeDark, DebrisC = BeigeDark * 0.6f,
            DebrisShape = DebrisShape.ConeUp,
            DebrisConeAngle = 70f,
            // Gold column rising from the slot.
            EmberCount = 14, EmberCubes = true,
            EmberSize = new Vector2(0.07f, 0.12f),
            EmberSpeed = new Vector2(0.8f, 1.6f),
            EmberLife = new Vector2(1.0f, 1.4f),
            EmberColor = Gold,
            LightIntensity = 3.5f, LightRange = 4f,
        };

        /// <summary>#17 화염 코브라 — 적 피격 시 주황 Impact. Spawn at the hit point; pair with VFX_Burning_Loop on the enemy.</summary>
        public static ImpactSpec FireImpact() => new ImpactSpec
        {
            PrefabName = "VFX_Fire_Impact",
            FlashSize = 1.2f, FlashColor = new Color(1f, 0.95f, 0.7f),
            CoreCount = 4, CoreSize = 0.3f, CoreA = Color.white, CoreB = Yellow, CoreC = Orange,
            Shockwave = true, ShockwaveSize = 1.9f, ShockwaveColor = Orange,
            DebrisCount = 20,
            DebrisSize = new Vector2(0.06f, 0.13f),
            DebrisSpeed = new Vector2(1.5f, 3.5f),
            DebrisLife = new Vector2(0.5f, 0.9f),
            DebrisGravity = 0.5f,
            DebrisA = Color.white, DebrisB = Yellow, DebrisC = RedDark,
            EmberCount = 8, EmberFlame = true,
            EmberSize = new Vector2(0.22f, 0.34f),
            EmberSpeed = new Vector2(0.5f, 1.1f),
            EmberLife = new Vector2(0.6f, 1.0f),
            EmberColor = Orange,
            LightIntensity = 3.5f, LightRange = 3f,
        };

        /// <summary>#4 모래 폭발: 베이지+주황, 반경 2.5, 모래 기둥 상승 큐브 40개, 링 2겹. Spawn at the hit point on the ground.</summary>
        public static ImpactSpec SandBurst() => new ImpactSpec
        {
            PrefabName = "VFX_Sand_Burst",
            Scale = 2.3f,
            GroundCollision = true,
            BodyHeight = 0.3f,
            FlashSize = 1.8f, FlashColor = new Color(1f, 0.95f, 0.8f),
            CoreCount = 5, CoreSize = 0.3f, CoreA = Color.white, CoreB = Orange, CoreC = BeigeDark,
            Shockwave = true, ShockwaveSize = 2.2f, ShockwaveColor = Beige, ShockwaveCount = 2, ShockwaveInterval = 0.14f,
            DebrisCount = 36,
            DebrisSize = new Vector2(0.06f, 0.12f),
            DebrisSpeed = new Vector2(1.6f, 3f),
            DebrisLife = new Vector2(0.6f, 1.0f),
            DebrisGravity = 0.9f,
            DebrisA = Orange * 0.75f, DebrisB = Beige * 0.55f, DebrisC = BeigeDark * 0.7f,
            DebrisShape = DebrisShape.ConeUp, DebrisConeAngle = 65f,
            // Sand column: many cubes straight up, slow to fall.
            EmberCount = 40, EmberCubes = true, EmberConeAngle = 7f,
            EmberSize = new Vector2(0.06f, 0.11f),
            EmberSpeed = new Vector2(2.2f, 3.6f),
            EmberLife = new Vector2(0.9f, 1.3f),
            EmberColor = Beige * 0.7f,
            DustCount = 10,
            DustSize = new Vector2(0.35f, 0.6f),
            LightIntensity = 4f, LightRange = 5f,
        };

        /// <summary>#22 적 스폰(입구): 모래 먼지 구름 + 빨강 링. Spawn at the entrance floor.</summary>
        public static ImpactSpec EnemySpawn() => new ImpactSpec
        {
            PrefabName = "VFX_Enemy_Spawn",
            GroundCollision = true,
            BodyHeight = 0.2f,
            FlashSize = 0.6f, FlashColor = Red,
            CoreCount = 2, CoreSize = 0.2f, CoreA = Red, CoreB = Red, CoreC = RedDark,
            Shockwave = true, ShockwaveSize = 2.0f, ShockwaveColor = Red,
            DebrisCount = 10,
            DebrisSize = new Vector2(0.05f, 0.1f),
            DebrisSpeed = new Vector2(1f, 2f),
            DebrisLife = new Vector2(0.5f, 0.8f),
            DebrisGravity = 0.8f,
            DebrisA = Beige * 0.6f, DebrisB = BeigeDark, DebrisC = BeigeDark * 0.6f,
            DebrisShape = DebrisShape.ConeUp, DebrisConeAngle = 70f,
            EmberCount = 0,
            DustCount = 12,
            DustSize = new Vector2(0.6f, 1.0f),
            DustSpeed = new Vector2(0.4f, 0.9f),
            DustLife = new Vector2(1.0f, 1.6f),
            LightIntensity = 2f, LightRange = 3f,
        };

        /// <summary>#10 마나 충전 완료: 램프에서 틸 플래시 + 링. Spawn at the lamp.</summary>
        public static ImpactSpec ManaChargeComplete() => new ImpactSpec
        {
            PrefabName = "VFX_Mana_Charge_Complete",
            FlashSize = 1.2f,
            CoreCount = 3, CoreSize = 0.22f,
            Shockwave = true, ShockwaveSize = 1.6f, ShockwaveColor = Teal,
            DebrisCount = 0,
            EmberCount = 8,
            EmberSize = new Vector2(0.08f, 0.14f),
            EmberSpeed = new Vector2(0.5f, 1.0f),
            EmberLife = new Vector2(0.6f, 1.0f),
            LightIntensity = 3f, LightRange = 3f,
        };

        /// <summary>
        /// #11 코어 피해 (적 쪽): 적이 빨간 큐브로 분해되며 코어로 빨려듦. Spawn at the enemy's feet and set the
        /// attractor Target to the core. Pair with VFX_Core_Damage_Flash at the core.
        /// </summary>
        public static ImpactSpec CoreDamageEnemy() => new ImpactSpec
        {
            PrefabName = "VFX_Core_Damage_Enemy",
            BodyHeight = 0.9f,
            FlashSize = 0.9f, FlashColor = Red,
            CoreCount = 2, CoreSize = 0.25f, CoreA = Color.white, CoreB = Red, CoreC = RedDark,
            Shockwave = false,
            DebrisCount = 18,
            DebrisSize = new Vector2(0.12f, 0.2f),
            DebrisSpeed = new Vector2(0.4f, 1.2f),
            DebrisLife = new Vector2(1.2f, 1.6f),
            DebrisGravity = 0.15f,
            DebrisStart = Red,
            DebrisA = Color.white, DebrisB = Color.white, DebrisC = new Color(0.6f, 0.6f, 0.6f),
            DebrisShape = DebrisShape.Box,
            DebrisAttractor = true,
            EmberCount = 0,
            LightIntensity = 2.5f, LightRange = 3f,
        };

        /// <summary>#11 코어 피해 (코어 쪽): 코어 빨강 플래시 + 링. Spawn at the core base; flash sits at crystal height.</summary>
        public static ImpactSpec CoreDamageFlash() => new ImpactSpec
        {
            PrefabName = "VFX_Core_Damage_Flash",
            BodyHeight = 1.2f,
            FlashSize = 2.2f, FlashColor = new Color(1f, 0.55f, 0.45f),
            CoreCount = 6, CoreSize = 0.3f, CoreA = Color.white, CoreB = Red, CoreC = RedDark,
            Shockwave = true, ShockwaveSize = 3.2f, ShockwaveColor = Red, ShockwaveCount = 2, ShockwaveInterval = 0.1f,
            DebrisCount = 16,
            DebrisSize = new Vector2(0.06f, 0.12f),
            DebrisSpeed = new Vector2(2f, 4f),
            DebrisLife = new Vector2(0.4f, 0.7f),
            DebrisGravity = 0.3f,
            DebrisA = Color.white, DebrisB = Red, DebrisC = RedDark,
            EmberCount = 0,
            LightIntensity = 5f, LightRange = 5f,
        };

        // ---- Menu -----------------------------------------------------------

        [MenuItem("DesertTower/VFX/Build Sand Burst")]
        public static void BuildSandBurst() => Ping(Build(SandBurst(), SandBurstPath));

        [MenuItem("DesertTower/VFX/Build Enemy Spawn")]
        public static void BuildEnemySpawn() => Ping(Build(EnemySpawn(), EnemySpawnPath));

        [MenuItem("DesertTower/VFX/Build Mana Charge Complete")]
        public static void BuildManaChargeComplete() => Ping(Build(ManaChargeComplete(), ManaChargeCompletePath));

        [MenuItem("DesertTower/VFX/Build Core Damage (enemy + flash)")]
        public static void BuildCoreDamage()
        {
            Build(CoreDamageEnemy(), CoreDamageEnemyPath);
            Ping(Build(CoreDamageFlash(), CoreDamageFlashPath));
        }

        [MenuItem("DesertTower/VFX/Build Fire Impact")]
        public static void BuildFireImpact() => Ping(Build(FireImpact(), FireImpactPath));

        [MenuItem("DesertTower/VFX/Build Fire Impact + Place In Scene")]
        public static void BuildFireImpactAndPlace() => PlaceInScene(Build(FireImpact(), FireImpactPath));

        [MenuItem("DesertTower/VFX/Build Mana Bolt Impact")]
        public static void BuildManaBoltImpact() => Ping(Build(ManaBoltImpact(), ManaBoltImpactPath));

        [MenuItem("DesertTower/VFX/Build Mana Bolt Impact + Place In Scene")]
        public static void BuildManaBoltImpactAndPlace() => PlaceInScene(Build(ManaBoltImpact(), ManaBoltImpactPath));

        [MenuItem("DesertTower/VFX/Build Enemy Death")]
        public static void BuildEnemyDeath() => Ping(Build(EnemyDeath(), EnemyDeathPath));

        [MenuItem("DesertTower/VFX/Build Enemy Death + Place In Scene")]
        public static void BuildEnemyDeathAndPlace() => PlaceInScene(Build(EnemyDeath(), EnemyDeathPath));

        [MenuItem("DesertTower/VFX/Build Facility Complete")]
        public static void BuildBuildComplete() => Ping(Build(BuildComplete(), BuildCompletePath));

        [MenuItem("DesertTower/VFX/Build Facility Complete + Place In Scene")]
        public static void BuildBuildCompleteAndPlace() => PlaceInScene(Build(BuildComplete(), BuildCompletePath));

        static void Ping(GameObject prefab) => EditorGUIUtility.PingObject(prefab);

        // ---- Build ----------------------------------------------------------

        public static GameObject Build(ImpactSpec spec, string path)
        {
            var s = GetShared();
            float k = spec.Scale;

            var root = new GameObject(spec.PrefabName);
            Transform ground = null;
            if (spec.GroundCollision) ground = Child(root, "GroundPlane").transform; // local y = 0, normal = +Y

            BuildCore(root, spec, k, s);
            BuildFlash(Child(root, "Flash"), spec, k, s);
            if (spec.Shockwave) BuildShockwave(Child(root, "Shockwave"), spec, k, s);
            var debris = spec.DebrisCount > 0 ? BuildDebris(Child(root, "Debris"), spec, k, s, ground) : null;
            var ember = spec.EmberCount > 0 ? BuildEmber(Child(root, "Ember"), spec, k, s) : null;
            if (spec.DustCount > 0) BuildDust(Child(root, "Dust"), spec, k, s);

            if (spec.TintableDebris && debris != null)
            {
                var tint = root.AddComponent<VfxTint>();
                tint.Systems = new[] { debris };
            }
            if (spec.EmberAttractor && ember != null)
                ember.gameObject.AddComponent<VfxParticleAttractor>();
            if (spec.DebrisAttractor && debris != null)
            {
                var a = debris.gameObject.AddComponent<VfxParticleAttractor>();
                a.Delay = 0.45f;
                a.Acceleration = 30f;
                a.MaxSpeed = 12f;
                a.TargetOffset = new Vector3(0f, 1.2f, 0f); // core crystal height
                a.MaxPullTime = 2f;
            }

            return SavePrefab(root, path);
        }

        // ---- Layer 2: Core (root system, carries the light) -----------------
        static ParticleSystem BuildCore(GameObject go, ImpactSpec spec, float k, Shared s)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1.0f;
            main.startLifetime = 0.15f;
            main.startSpeed = 0f;
            main.startSize = spec.CoreSize * k;
            main.maxParticles = Mathf.Max(2, spec.CoreCount * 2);
            RandomRotation3D(main);

            Burst(ps, spec.CoreCount);
            Size(ps, Punch(0.5f, 1f, 0f));
            ColorRamp(ps, spec.CoreA, spec.CoreB, spec.CoreC);
            main.startColor = Color.white;

            if (spec.BodyHeight > 0f)
            {
                // The core is the root system, so lift its emitter rather than the transform.
                SphereShape(ps, 0.01f);
                var shape = ps.shape;
                shape.position = new Vector3(0f, spec.BodyHeight, 0f);
            }

            UseMesh(r, s.Cube, s.CubeWhite);

            // The light takes the core particle's colour, so it flashes white and settles on CoreB.
            ParticleLight(ps, s.LightPrefab, spec.LightIntensity, spec.LightRange * k, ratio: 1f / Mathf.Max(1, spec.CoreCount));
            return ps;
        }

        // ---- Layer 1: Flash -------------------------------------------------
        static void BuildFlash(GameObject go, ImpactSpec spec, float k, Shared s)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1.0f;
            main.startLifetime = 0.12f;   // ~7 frames at 60 fps: 3 held, 4 shrinking
            main.startSpeed = 0f;
            main.startSize = spec.FlashSize * k;
            main.startColor = spec.FlashColor;
            main.maxParticles = 2;

            Burst(ps, 1);
            Size(ps, HoldThenDrop(0.4f));

            UseBillboard(r, s.GlowWhite);
            r.sortingFudge = -10f; // draw on top of siblings
            go.transform.localPosition = new Vector3(0f, spec.BodyHeight, 0f);
        }

        // ---- Layer 3: Shockwave (Hovl Torus1, laid flat, additive so it can fade) ----
        static void BuildShockwave(GameObject go, ImpactSpec spec, float k, Shared s)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1.0f;
            main.startLifetime = 0.3f;
            main.startSpeed = 0f;
            main.startSize = FitScale(s.Torus, spec.ShockwaveSize * k); // mesh particles: size is a scale on the mesh
            main.startColor = spec.ShockwaveColor;
            main.maxParticles = 2;

            // Lay the ring flat whatever axis the FBX uses as its normal.
            var euler = AxisToUp(ThinAxis(s.Torus)).eulerAngles * Mathf.Deg2Rad;
            main.startRotation3D = true;
            main.startRotationX = euler.x;
            main.startRotationY = euler.y;
            main.startRotationZ = euler.z;

            if (spec.ShockwaveCount > 1)
            {
                main.maxParticles = spec.ShockwaveCount + 1;
                main.duration = Mathf.Max(1f, spec.ShockwaveCount * spec.ShockwaveInterval + 0.5f);
                Bursts(ps, 1, spec.ShockwaveCount, spec.ShockwaveInterval);
            }
            else Burst(ps, 1);
            Size(ps, EaseOut(0.15f, 1f));
            AlphaFade(ps, Color.white);

            UseMesh(r, s.Torus, s.MeshAdditive);
            go.transform.localPosition = new Vector3(0f, 0.04f, 0f); // lift off the ground to avoid z-fight
        }

        // ---- Layer 4: Debris ------------------------------------------------
        static ParticleSystem BuildDebris(GameObject go, ImpactSpec spec, float k, Shared s, Transform ground)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1.0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(spec.DebrisLife.x, spec.DebrisLife.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(spec.DebrisSpeed.x * k, spec.DebrisSpeed.y * k);
            main.startSize = new ParticleSystem.MinMaxCurve(spec.DebrisSize.x * k, spec.DebrisSize.y * k);
            main.startColor = spec.DebrisStart;
            main.gravityModifier = spec.DebrisGravity;
            main.maxParticles = spec.DebrisCount + 8;
            RandomRotation3D(main);

            Burst(ps, spec.DebrisCount);

            switch (spec.DebrisShape)
            {
                case DebrisShape.ConeUp:
                    ConeShape(ps, spec.DebrisConeAngle, 0.25f * k);
                    go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // cone points up
                    break;
                case DebrisShape.Box:
                {
                    // The character volume crumbles: emit throughout the box, every direction, no preferred axis.
                    var shape = ps.shape;
                    shape.enabled = true;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = spec.DebrisBox;
                    shape.randomDirectionAmount = 1f;
                    go.transform.localPosition = new Vector3(0f, spec.BodyHeight, 0f);
                    break;
                }
                default:
                    SphereShape(ps, 0.15f * k);
                    break;
            }

            Drag(ps, 0.25f, 1.5f * k); // air drag so cubes stay near the hit point
            Tumble(ps);
            Size(ps, HoldThenDrop(0.6f));
            ColorRamp(ps, spec.DebrisA, spec.DebrisB, spec.DebrisC);

            if (ground != null) GroundCollision(ps, ground);

            UseMesh(r, s.Cube, s.CubeWhite);
            return ps;
        }

        // ---- Layer 6: Dust (CFXR smoke tiles, sand-tinted, alpha-blended) ---
        static void BuildDust(GameObject go, ImpactSpec spec, float k, Shared s)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1.0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(spec.DustLife.x, spec.DustLife.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(spec.DustSpeed.x * k, spec.DustSpeed.y * k);
            main.startSize = new ParticleSystem.MinMaxCurve(spec.DustSize.x * k, spec.DustSize.y * k);
            main.startColor = spec.DustColor;
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.maxParticles = spec.DustCount + 4;

            Burst(ps, spec.DustCount);
            ConeShape(ps, 60f, 0.35f * k);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // cone points up
            go.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            Drag(ps, 0.3f, 1f);
            Noise(ps, 0.2f, 0.3f, 0.3f);
            Size(ps, EaseOut(0.5f, 1.3f));
            AlphaFade(ps, Color.white, 0.25f);
            RandomTile(ps, 2, 2);

            UseBillboard(r, s.Smoke);
            r.sortingFudge = 5f; // behind the cubes and rings
        }

        // ---- Layer 5: Ember / afterglow -------------------------------------
        static ParticleSystem BuildEmber(GameObject go, ImpactSpec spec, float k, Shared s)
        {
            var ps = AddSystem(go, out var r);
            var main = ps.main;
            main.duration = 1.0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(spec.EmberLife.x, spec.EmberLife.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(spec.EmberSpeed.x * k, spec.EmberSpeed.y * k);
            main.startSize = new ParticleSystem.MinMaxCurve(spec.EmberSize.x * k, spec.EmberSize.y * k);
            main.startColor = spec.EmberColor;
            main.maxParticles = spec.EmberCount + 4;

            Burst(ps, spec.EmberCount);

            ConeShape(ps, spec.EmberConeAngle >= 0f ? spec.EmberConeAngle : spec.EmberCubes ? 15f : 30f, 0.3f * k);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // cone points up
            go.transform.localPosition = new Vector3(0f, spec.BodyHeight, 0f);

            Noise(ps, 0.3f, 0.5f, 0.5f);

            if (spec.EmberCubes)
            {
                RandomRotation3D(main);
                Tumble(ps, Mathf.PI * 0.6f);
                Size(ps, InOut(0.1f, 0.6f));
                ColorRamp(ps, Color.white, Color.white, new Color(0.5f, 0.5f, 0.5f), 0.6f);
                UseMesh(r, s.Cube, s.CubeWhite);
            }
            else if (spec.EmberFlame)
            {
                // CFXR stepped flame is a 1:2 sprite with the base at the bottom edge.
                main.startSize3D = true;
                main.startSizeX = new ParticleSystem.MinMaxCurve(spec.EmberSize.x * k, spec.EmberSize.y * k);
                main.startSizeY = new ParticleSystem.MinMaxCurve(spec.EmberSize.x * k * 2f, spec.EmberSize.y * k * 2f);
                main.startSizeZ = 1f;
                main.startColor = Color.white;
                Size(ps, InOut(0.15f, 0.5f));
                ColorRampFade(ps, Color.white, spec.EmberColor, RedDark, 0.3f, 0.5f);
                UseBillboard(r, s.Flame);
                r.pivot = new Vector3(0f, 0.5f, 0f); // stand on the emission point
            }
            else
            {
                Size(ps, Linear(1f, 0f));
                AlphaFade(ps, Color.white);
                UseBillboard(r, s.GlowWhite);
            }
            return ps;
        }
    }
}
