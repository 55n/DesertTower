using UnityEngine;

namespace DesertTower.VFX
{
    /// <summary>
    /// Pulls this system's particles toward a target and kills them on arrival. Used for
    /// "경험치 금 큐브가 플레이어로 흡수" (#23) and later for the mana stream (#10) and demolish refund (#16).
    /// Set <see cref="Target"/> right after spawning; with no target the particles keep their normal motion.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class VfxParticleAttractor : MonoBehaviour
    {
        public Transform Target;
        [Tooltip("World-space offset added to the target position (e.g. chest height).")]
        public Vector3 TargetOffset = new Vector3(0f, 0.9f, 0f);
        [Tooltip("Seconds after birth before the pull starts, so the burst can spread first.")]
        public float Delay = 0.35f;
        [Tooltip("Acceleration toward the target, units/s².")]
        public float Acceleration = 40f;
        [Tooltip("Speed cap once the pull is active.")]
        public float MaxSpeed = 14f;
        [Tooltip("Particles closer than this are consumed.")]
        public float ArriveRadius = 0.3f;
        [Tooltip("Safety: a particle still in flight this long after the pull started is consumed anyway.")]
        public float MaxPullTime = 1.5f;

        ParticleSystem _ps;
        ParticleSystem.Particle[] _buffer;

        void Awake() => _ps = GetComponent<ParticleSystem>();

        void LateUpdate() => Tick(Time.deltaTime);

        /// <summary>Advance by <paramref name="dt"/>. Public so previews can step it outside play mode.</summary>
        public void Tick(float dt)
        {
            if (Target == null || dt <= 0f) return;
            if (_ps == null) _ps = GetComponent<ParticleSystem>();

            int max = _ps.main.maxParticles;
            if (_buffer == null || _buffer.Length < max) _buffer = new ParticleSystem.Particle[max];
            int count = _ps.GetParticles(_buffer);
            if (count == 0) return;

            Vector3 goal = Target.position + TargetOffset;
            bool local = _ps.main.simulationSpace == ParticleSystemSimulationSpace.Local;
            if (local) goal = transform.InverseTransformPoint(goal);

            for (int i = 0; i < count; i++)
            {
                ref var p = ref _buffer[i];
                float age = p.startLifetime - p.remainingLifetime;
                if (age < Delay) continue;

                Vector3 toGoal = goal - p.position;
                float dist = toGoal.magnitude;
                if (dist <= ArriveRadius || age > Delay + MaxPullTime)
                {
                    p.remainingLifetime = 0f;
                    continue;
                }

                // Steer toward a velocity that lands on the goal, never past it, so nothing orbits.
                float speed = Mathf.Min(MaxSpeed, dist / Mathf.Max(dt, 1e-4f));
                Vector3 desired = toGoal / dist * speed;
                p.velocity = Vector3.MoveTowards(p.velocity, desired, Acceleration * dt);
                // Keep the particle alive until it arrives; the pull always wins over lifetime.
                if (p.remainingLifetime < 0.2f) p.remainingLifetime = 0.2f;
            }

            _ps.SetParticles(_buffer, count);
        }
    }
}
