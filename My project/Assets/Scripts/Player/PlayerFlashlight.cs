using UnityEngine;
using UnityEngine.InputSystem;
using ProjectRac.UI;

namespace ProjectRac.Player
{
    public class PlayerFlashlight : MonoBehaviour
    {
        [Header("Flashlight Settings")]
        [SerializeField] private Light spotLight;
        [SerializeField] private Transform swayPivot;
        [SerializeField] private bool startOn = true;
        [SerializeField] private Key toggleKey = Key.F;

        [Header("Handheld Sway")]
        [SerializeField] private float rotationLagSpeed = 12f;

        [Header("Audio")]
        [SerializeField] private AudioClip toggleSound;

        public bool IsOn { get; private set; }

        private AudioSource _audioSource;
        private Quaternion _smoothedWorldRot;
        private bool _rotInitialized;

        private void Awake()
        {
            EnsureLightSetup();
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 0f;
                _audioSource.playOnAwake = false;
            }

            SetOn(startOn, playAudio: false);
        }

        private void LateUpdate()
        {
            HandleToggleInput();
            UpdateHandheldLag();
        }

        public void Configure(Light lightComp, Transform pivot, bool enabledByDefault = true)
        {
            spotLight = lightComp;
            swayPivot = pivot;
            startOn = enabledByDefault;
        }

        public void Toggle()
        {
            SetOn(!IsOn, playAudio: true);
        }

        public void SetOn(bool on, bool playAudio = false)
        {
            IsOn = on;
            if (spotLight != null) spotLight.enabled = IsOn;
            if (playAudio && toggleSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(toggleSound, 0.7f);
            }
        }

        private void HandleToggleInput()
        {
            if (NoteInspectionUI.IsInspecting) return;

            var kb = Keyboard.current;
            if (kb != null && kb[toggleKey].wasPressedThisFrame)
            {
                Toggle();
            }
        }

        private void UpdateHandheldLag()
        {
            if (swayPivot == null) return;

            if (!_rotInitialized)
            {
                _smoothedWorldRot = transform.rotation;
                _rotInitialized = true;
            }

            float t = 1f - Mathf.Exp(-rotationLagSpeed * Time.deltaTime);
            _smoothedWorldRot = Quaternion.Slerp(_smoothedWorldRot, transform.rotation, t);
            swayPivot.rotation = _smoothedWorldRot;
        }

        private void EnsureLightSetup()
        {
            if (swayPivot == null)
            {
                Transform existing = transform.Find("FlashlightPivot");
                if (existing != null) swayPivot = existing;
            }

            if (spotLight == null && swayPivot != null)
            {
                spotLight = swayPivot.GetComponentInChildren<Light>(true);
            }
        }
    }
}
