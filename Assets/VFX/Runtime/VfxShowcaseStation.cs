using UnityEngine;

namespace DesertTower.VFX
{
    /// <summary>
    /// One exhibit in the VFX showcase scene (Assets/VFX/Scenes/VFX_Showcase.unity). Spawns its prefab at
    /// this transform and keeps it going in play mode: one-shots respawn on a timer, the projectile flies a
    /// track and lands its impact, the core sweeps its stability, the enemy death gets a random tint.
    /// Play-mode only — everything that needs scripts (attractor, spin, flicker, scale-in) runs here.
    /// </summary>
    public sealed class VfxShowcaseStation : MonoBehaviour
    {
        public enum Mode
        {
            Replay,         // destroy + respawn every Interval
            Loop,           // spawn once, leave running
            PulseLoop,      // loop that plays for PulseOn seconds, stops for PulseOff (breath attacks)
            Projectile,     // fly +Z for Track units at Speed, spawn ImpactPrefab, respawn
            Core,           // CoreAmbientVfx stability sweeps 100 → 0 → 100 over CorePeriod
            EnemyDeath,     // Replay + random VfxTint + attractor toward Target
            BuildComplete,  // Replay + Facility scales in with VfxScaleIn
        }

        public Mode Kind = Mode.Replay;
        public GameObject Prefab;
        [Tooltip("Projectile mode: spawned where the flight ends.")]
        public GameObject ImpactPrefab;
        [Tooltip("Where any VfxParticleAttractor in the prefab pulls to (XP → player, mana stream → lamp, enemy cubes → core).")]
        public Transform Target;
        [Tooltip("BuildComplete: the object that rises out of the ground.")]
        public GameObject Facility;

        public float Interval = 2.5f;
        public float PulseOn = 2f, PulseOff = 1.2f;
        public float Speed = 8f;
        public float Track = 6f;
        public float CorePeriod = 10f;
        public Color[] TintPalette =
        {
            new Color(0.85f, 0.72f, 0.48f), new Color(0.55f, 0.35f, 0.25f),
            new Color(0.35f, 0.45f, 0.55f), new Color(0.65f, 0.25f, 0.25f),
        };

        GameObject _inst;
        float _timer;
        bool _pulseOn = true;

        void Start() => Spawn();

        void Spawn()
        {
            if (_inst != null) Destroy(_inst);
            if (Prefab == null) return;
            _inst = Instantiate(Prefab, transform.position, transform.rotation, transform);
            _timer = 0f;

            if (Target != null)
                foreach (var a in _inst.GetComponentsInChildren<VfxParticleAttractor>()) a.Target = Target;

            if (Kind == Mode.EnemyDeath)
            {
                var tint = _inst.GetComponent<VfxTint>();
                if (tint != null && TintPalette.Length > 0) tint.Apply(TintPalette[Random.Range(0, TintPalette.Length)]);
            }
            else if (Kind == Mode.BuildComplete && Facility != null)
            {
                if (Facility.GetComponent<VfxScaleIn>() == null) Facility.AddComponent<VfxScaleIn>();
            }
        }

        void Update()
        {
            _timer += Time.deltaTime;
            switch (Kind)
            {
                case Mode.Replay:
                case Mode.EnemyDeath:
                case Mode.BuildComplete:
                    if (_timer >= Interval) Spawn();
                    break;

                case Mode.PulseLoop:
                    if (_inst == null) break;
                    if (_pulseOn && _timer >= PulseOn)
                    {
                        foreach (var ps in _inst.GetComponentsInChildren<ParticleSystem>()) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                        _pulseOn = false; _timer = 0f;
                    }
                    else if (!_pulseOn && _timer >= PulseOff)
                    {
                        foreach (var ps in _inst.GetComponentsInChildren<ParticleSystem>()) ps.Play(false);
                        _pulseOn = true; _timer = 0f;
                    }
                    break;

                case Mode.Projectile:
                    if (_inst == null) break;
                    _inst.transform.position += _inst.transform.forward * (Speed * Time.deltaTime);
                    if (Vector3.Distance(_inst.transform.position, transform.position) >= Track)
                    {
                        if (ImpactPrefab != null)
                            Destroy(Instantiate(ImpactPrefab, _inst.transform.position, Quaternion.identity, transform), 3f);
                        Spawn();
                    }
                    break;

                case Mode.Core:
                    if (_inst == null) break;
                    var core = _inst.GetComponent<CoreAmbientVfx>();
                    if (core != null) core.SetStability(100f * Mathf.PingPong(_timer / CorePeriod * 2f, 1f));
                    break;
            }
        }
    }
}
