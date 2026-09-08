using UnityEngine;

namespace DesertTower.VFX
{
    /// <summary>
    /// "시설이 바닥에서 솟아오름" (VFX 제작계획 #14): scales the object from 0 to its authored scale with an
    /// overshoot ease, then removes itself. Add to a facility right after placing it, or keep it on the
    /// facility prefab so every placement plays it. Restart with <see cref="Play"/>.
    /// </summary>
    public sealed class VfxScaleIn : MonoBehaviour
    {
        public float Duration = 0.4f;
        [Tooltip("How far past 1.0 the scale pops before settling. 0 = plain ease-out.")]
        public float Overshoot = 0.12f;
        [Tooltip("Remove this component when the animation finishes.")]
        public bool DestroyWhenDone = true;

        Vector3 _target;
        float _elapsed;
        bool _playing;

        void OnEnable() => Play();

        public void Play()
        {
            if (!_playing) _target = transform.localScale;
            _elapsed = 0f;
            _playing = true;
            transform.localScale = Vector3.zero;
        }

        void Update()
        {
            if (!_playing) return;
            _elapsed += Time.deltaTime;
            float t = Duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / Duration);
            transform.localScale = _target * Evaluate(t);
            if (t < 1f) return;

            transform.localScale = _target;
            _playing = false;
            if (DestroyWhenDone) Destroy(this);
        }

        // Ease-out back: rises fast, overshoots by ~Overshoot, settles. Overshoot 0 degrades to a cubic ease-out.
        float Evaluate(float t)
        {
            float s = 1.70158f * (Overshoot / 0.1f);
            t -= 1f;
            return 1f + t * t * ((s + 1f) * t + s);
        }
    }
}
