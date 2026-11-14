using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ProjectRac.Player;

namespace ProjectRac.Effects
{
    public class StaminaExhaustionPostProcess : MonoBehaviour
    {
        [Header("Stamina Reference")]
        [SerializeField] private PlayerStamina playerStamina;

        [Header("Threshold Settings")]
        [Tooltip("Stamina level below which exhaustion post-processing activates.")]
        [SerializeField] private float staminaThreshold = 35f;
        [Tooltip("Speed of fading the post-processing effects in and out.")]
        [SerializeField] private float transitionSpeed = 3.5f;
        [Tooltip("When true, intensity scales progressively from threshold down to 0.")]
        [SerializeField] private bool progressiveScaling = true;

        [Header("Effect Intensity Maximums (Reached at 0 Stamina)")]
        [Tooltip("Negative value for barrel distortion (visual disorientation).")]
        [Range(-1f, 0f)] [SerializeField] private float maxLensDistortion = -0.40f;
        [Range(0f, 1f)] [SerializeField] private float maxVignetteIntensity = 0.48f;
        [Range(0.01f, 1f)] [SerializeField] private float vignetteSmoothness = 0.40f;
        [Range(0f, 1f)] [SerializeField] private float maxFilmGrainIntensity = 0.65f;
        [SerializeField] private FilmGrainLookup grainType = FilmGrainLookup.Medium1;

        [Header("Breathing / Heartbeat Pulse (Extreme Exhaustion)")]
        [SerializeField] private bool enablePulseAtZero = true;
        [SerializeField] private float pulseFrequency = 4.0f;
        [SerializeField] private float pulseAmplitude = 0.06f;

        private Volume _volume;
        private VolumeProfile _runtimeProfile;
        private LensDistortion _lensDistortion;
        private Vignette _vignette;
        private FilmGrain _filmGrain;

        private float _currentWeight;
        private float _targetWeight;

        public float CurrentWeight => _currentWeight;
        public float TargetWeight => _targetWeight;

        private void Awake()
        {
            ResolveReferences();
            SetupPostProcessingVolume();
        }

        private void OnEnable()
        {
            if (playerStamina != null)
            {
                playerStamina.OnStaminaChanged += HandleStaminaChanged;
                HandleStaminaChanged(playerStamina.CurrentStamina, playerStamina.MaxStamina);
            }
        }

        private void OnDisable()
        {
            if (playerStamina != null)
            {
                playerStamina.OnStaminaChanged -= HandleStaminaChanged;
            }
        }

        private void OnDestroy()
        {
            if (_runtimeProfile != null)
            {
                Destroy(_runtimeProfile);
            }
        }

        public void BindPlayerStamina(PlayerStamina stamina)
        {
            if (playerStamina != null)
            {
                playerStamina.OnStaminaChanged -= HandleStaminaChanged;
            }

            playerStamina = stamina;

            if (playerStamina != null && enabled)
            {
                playerStamina.OnStaminaChanged += HandleStaminaChanged;
                HandleStaminaChanged(playerStamina.CurrentStamina, playerStamina.MaxStamina);
            }
        }

        private void ResolveReferences()
        {
            if (playerStamina == null)
            {
                playerStamina = GetComponentInParent<PlayerStamina>();
                if (playerStamina == null)
                {
                    playerStamina = FindAnyObjectByType<PlayerStamina>();
                }
            }
        }

        private void SetupPostProcessingVolume()
        {
            if (!TryGetComponent<Volume>(out _volume))
            {
                _volume = gameObject.AddComponent<Volume>();
            }

            _volume.isGlobal = true;
            _volume.priority = 25f; // Prioritize over base ambient scene volumes

            _runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            _runtimeProfile.name = "StaminaExhaustionProfile";

            // 1. Negative Lens Distortion (Barrel distortion simulating exhaustion)
            _lensDistortion = _runtimeProfile.Add<LensDistortion>(true);
            _lensDistortion.intensity.overrideState = true;
            _lensDistortion.intensity.value = maxLensDistortion;
            _lensDistortion.scale.overrideState = true;
            _lensDistortion.scale.value = 1.0f;

            // 2. Dark Vignette (Tunnel vision peripheral darkening)
            _vignette = _runtimeProfile.Add<Vignette>(true);
            _vignette.intensity.overrideState = true;
            _vignette.intensity.value = maxVignetteIntensity;
            _vignette.smoothness.overrideState = true;
            _vignette.smoothness.value = vignetteSmoothness;
            _vignette.color.overrideState = true;
            _vignette.color.value = Color.black;

            // 3. Film Grain (Visual noise and strain)
            _filmGrain = _runtimeProfile.Add<FilmGrain>(true);
            _filmGrain.type.overrideState = true;
            _filmGrain.type.value = grainType;
            _filmGrain.intensity.overrideState = true;
            _filmGrain.intensity.value = maxFilmGrainIntensity;
            _filmGrain.response.overrideState = true;
            _filmGrain.response.value = 0.8f;

            _volume.profile = _runtimeProfile;
            _volume.weight = 0f;
        }

        private void Update()
        {
            if (_volume == null) return;

            // Smoothly move towards target exhaustion weight
            _currentWeight = Mathf.MoveTowards(_currentWeight, _targetWeight, transitionSpeed * Time.deltaTime);

            float finalWeight = _currentWeight;

            // Subtle pulsing breathing rhythm when stamina is near depletion
            if (enablePulseAtZero && _currentWeight > 0.65f && playerStamina != null && playerStamina.CurrentStamina <= 5f)
            {
                float pulse = Mathf.Sin(Time.time * pulseFrequency) * pulseAmplitude;
                finalWeight = Mathf.Clamp01(_currentWeight + pulse);
            }

            _volume.weight = finalWeight;
        }

        private void HandleStaminaChanged(float currentStamina, float maxStamina)
        {
            if (currentStamina >= staminaThreshold)
            {
                _targetWeight = 0f;
            }
            else
            {
                if (progressiveScaling)
                {
                    _targetWeight = Mathf.Clamp01((staminaThreshold - currentStamina) / staminaThreshold);
                }
                else
                {
                    _targetWeight = 1.0f;
                }
            }
        }
    }
}
