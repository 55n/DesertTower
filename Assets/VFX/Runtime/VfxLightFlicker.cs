using UnityEngine;

namespace DesertTower.VFX
{
    /// <summary>Fire-style light flicker: intensity wanders with smooth noise around the authored value.</summary>
    [RequireComponent(typeof(Light))]
    public sealed class VfxLightFlicker : MonoBehaviour
    {
        [Tooltip("Fraction of the base intensity the flicker can add or remove.")]
        [Range(0f, 1f)] public float Amplitude = 0.35f;
        [Tooltip("Noise speed. Higher = faster flicker.")]
        public float Speed = 9f;

        Light _light;
        float _base, _seed;

        void Awake()
        {
            _light = GetComponent<Light>();
            _base = _light.intensity;
            _seed = Random.value * 100f;
        }

        void Update() => Tick(Time.time);

        /// <summary>Set the light for absolute time <paramref name="t"/>. Public so previews can pose it.</summary>
        public void Tick(float t)
        {
            if (_light == null) { _light = GetComponent<Light>(); _base = _light.intensity; }
            float n = Mathf.PerlinNoise(_seed + t * Speed, _seed * 0.37f) * 2f - 1f;
            _light.intensity = _base * (1f + n * Amplitude);
        }
    }
}
