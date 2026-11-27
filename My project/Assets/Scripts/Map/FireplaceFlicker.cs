using UnityEngine;

namespace ProjectRac.Map
{
    // Subtle warm flame flicker for the Sala de Estar fireplace light.
    [RequireComponent(typeof(Light))]
    public class FireplaceFlicker : MonoBehaviour
    {
        [SerializeField] private float baseIntensity = 9.5f;
        [SerializeField] private float flickerAmplitude = 1.6f;
        [SerializeField] private float flickerSpeed = 6.5f;

        private Light _light;
        private float _seed;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _seed = Random.Range(0f, 100f);
            if (_light != null && baseIntensity <= 0f) baseIntensity = _light.intensity;
        }

        private void Update()
        {
            if (_light == null) return;
            float n = Mathf.PerlinNoise(_seed, Time.time * flickerSpeed);
            _light.intensity = baseIntensity + (n - 0.5f) * 2f * flickerAmplitude;
        }
    }
}
