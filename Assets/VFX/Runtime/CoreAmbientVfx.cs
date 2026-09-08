using UnityEngine;

namespace DesertTower.VFX
{
    /// <summary>
    /// Core ambient effect controller (VFX 제작계획 #9). Drives the crystal pulse, particle colours,
    /// loop speed, smoke and light from a 0–100 stability value in three stages:
    /// 100–60 teal / 60–30 yellow / 30 and below red with black smoke.
    /// Gameplay calls <see cref="SetStability"/>; everything visual follows from that.
    /// </summary>
    [ExecuteAlways]
    public sealed class CoreAmbientVfx : MonoBehaviour
    {
        [System.Serializable]
        public struct Stage
        {
            public string Name;
            public Color Color;
            [Tooltip("Multiplier on the loop systems' simulation speed.")]
            public float Speed;
            [Tooltip("Crystal pulse frequency, Hz.")]
            public float PulseHz;
            [Tooltip("Crystal emissive brightness range (min, max) — multiplies the material HDR colour.")]
            public Vector2 Pulse;
            [Tooltip("Smoke particles per second (0 = off).")]
            public float SmokeRate;
            public float LightIntensity;
        }

        [Range(0f, 100f)] public float Stability = 100f;
        public float StageYellowBelow = 60f;
        public float StageRedBelow = 30f;

        public Stage Stable;
        public Stage Strained;
        public Stage Critical;

        [Header("Wiring (set by the builder)")]
        public Renderer Crystal;
        public Light Light;
        public ParticleSystem[] ColoredSystems;
        public ParticleSystem Smoke;

        [Tooltip("Seconds to blend colour and speed when the stage changes.")]
        public float BlendTime = 0.6f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        MaterialPropertyBlock _block;
        Color _currentColor;
        float _currentSpeed = 1f, _currentLight, _time;
        bool _initialised;

        public Stage CurrentStage =>
            Stability <= StageRedBelow ? Critical :
            Stability <= StageYellowBelow ? Strained : Stable;

        public void SetStability(float value)
        {
            Stability = Mathf.Clamp(value, 0f, 100f);
        }

        void OnEnable()
        {
            _initialised = false;
        }

        void Update() => Tick(Application.isPlaying ? Time.deltaTime : 0f);

        /// <summary>Advance the pulse by <paramref name="dt"/>. Public so previews can step it outside play mode.</summary>
        public void Tick(float dt)
        {
            var stage = CurrentStage;
            if (!_initialised)
            {
                _currentColor = stage.Color;
                _currentSpeed = stage.Speed;
                _currentLight = stage.LightIntensity;
                _initialised = true;
            }
            else if (dt > 0f)
            {
                float k = BlendTime <= 0f ? 1f : Mathf.Clamp01(dt / BlendTime);
                _currentColor = Color.Lerp(_currentColor, stage.Color, k);
                _currentSpeed = Mathf.Lerp(_currentSpeed, stage.Speed, k);
                _currentLight = Mathf.Lerp(_currentLight, stage.LightIntensity, k);
            }

            _time += dt * _currentSpeed;
            float pulse = Mathf.Lerp(stage.Pulse.x, stage.Pulse.y, 0.5f + 0.5f * Mathf.Sin(_time * stage.PulseHz * Mathf.PI * 2f));

            if (Crystal != null)
            {
                _block ??= new MaterialPropertyBlock();
                Crystal.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, _currentColor * pulse);
                Crystal.SetPropertyBlock(_block);
            }

            if (Light != null)
            {
                Light.color = _currentColor;
                Light.intensity = _currentLight * Mathf.Lerp(0.85f, 1.15f, (pulse - stage.Pulse.x) / Mathf.Max(0.001f, stage.Pulse.y - stage.Pulse.x));
            }

            if (ColoredSystems != null)
            {
                foreach (var ps in ColoredSystems)
                {
                    if (ps == null) continue;
                    var main = ps.main;
                    main.startColor = _currentColor;
                    main.simulationSpeed = _currentSpeed;
                }
            }

            if (Smoke != null)
            {
                var emission = Smoke.emission;
                emission.rateOverTime = stage.SmokeRate;
            }
        }
    }
}
