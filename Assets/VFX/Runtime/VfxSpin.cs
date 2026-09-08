using UnityEngine;

namespace DesertTower.VFX
{
    /// <summary>Constant local rotation. Used for mesh cores (projectile cube, core crystal) that are not particles.</summary>
    public sealed class VfxSpin : MonoBehaviour
    {
        [Tooltip("Degrees per second around each local axis.")]
        public Vector3 DegreesPerSecond = new Vector3(0f, 180f, 90f);

        void Update() => Tick(Time.deltaTime);

        /// <summary>Advance by <paramref name="dt"/> seconds. Public so editor previews can step it without play mode.</summary>
        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            transform.Rotate(DegreesPerSecond * dt, Space.Self);
        }
    }
}
