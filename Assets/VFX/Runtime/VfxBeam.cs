using UnityEngine;

namespace DesertTower.VFX
{
    /// <summary>
    /// Drives a unit beam mesh (see VfxBuildKit.UnitBeamMesh: diameter 1, z from 0 to 1) along this
    /// object's local +Z: sets its length and width, scrolls the stripe texture, and for one-shots
    /// animates the width in and out over <see cref="Duration"/>. No custom shader — the scroll is a
    /// texture offset on a MaterialPropertyBlock. Gameplay sets <see cref="Length"/> (pierce range,
    /// pillar height) before or right after spawning.
    /// </summary>
    public sealed class VfxBeam : MonoBehaviour
    {
        public Renderer Target;
        public float Length = 6f;
        public float Radius = 0.25f;
        [Tooltip("Seconds the beam lives. 0 = loop forever at full width.")]
        public float Duration = 0.35f;
        [Tooltip("Width multiplier over the normalised lifetime (one-shots only).")]
        public AnimationCurve Width = new AnimationCurve(
            new Keyframe(0f, 0.2f, 0f, 8f), new Keyframe(0.15f, 1f, 0f, 0f), new Keyframe(0.6f, 0.9f, 0f, 0f), new Keyframe(1f, 0f, -4f, 0f));
        public Color Color = Color.white;
        [Tooltip("HDR multiplier applied to Color (the material's own base colour is replaced).")]
        public float Intensity = 2.5f;
        [Tooltip("Stripe repeats along the beam per unit length.")]
        public float StripesPerUnit = 1f;
        [Tooltip("Stripe scroll speed in texture repeats per second. Negative = toward the origin.")]
        public float ScrollSpeed = 3f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");

        MaterialPropertyBlock _block;
        float _t;

        void OnEnable() => Restart();

        /// <summary>Rewind to the start (also used by editor previews, where OnEnable does not run).</summary>
        public void Restart()
        {
            _t = 0f;
            if (Target != null) Target.enabled = true;
            Apply();
        }

        void Update() => Tick(Time.deltaTime);

        /// <summary>Advance by <paramref name="dt"/>. Public so previews can step it outside play mode.</summary>
        public void Tick(float dt)
        {
            _t += dt;
            Apply();
        }

        public void SetLength(float length)
        {
            Length = length;
            Apply();
        }

        void Apply()
        {
            float u = Duration > 0f ? Mathf.Clamp01(_t / Duration) : 0f;
            float w = Duration > 0f ? Width.Evaluate(u) : 1f;
            transform.localScale = new Vector3(Radius * 2f * w, Radius * 2f * w, Length);

            if (Target == null) return;
            _block ??= new MaterialPropertyBlock();
            Target.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, Color * Intensity);
            _block.SetVector(BaseMapStId, new Vector4(1f, Length * StripesPerUnit, 0f, -_t * ScrollSpeed));
            Target.SetPropertyBlock(_block);

            if (Duration > 0f && _t >= Duration) Target.enabled = false;
        }
    }
}
