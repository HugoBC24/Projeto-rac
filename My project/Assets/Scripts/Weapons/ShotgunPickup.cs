using UnityEngine;
using ProjectRac.Interaction;

namespace ProjectRac.Weapons
{
    [RequireComponent(typeof(Collider))]
    public class ShotgunPickup : MonoBehaviour, IInteractable
    {
        [Header("3D Model Customization (Inspector)")]
        [Tooltip("Drag & drop any custom 3D model/prefab here to replace the world pickup model!")]
        [SerializeField] private GameObject customWorldModelPrefab;
        [SerializeField] private Vector3 modelOffset = new Vector3(0f, 0.4f, 0f);
        [SerializeField] private Vector3 modelRotation = new Vector3(12f, 45f, 0f);
        [SerializeField] private Vector3 modelScale = new Vector3(1f, 1f, 1f);

        [Header("Idle Animation Settings")]
        [SerializeField] private float bobFrequency = 2.5f;
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float rotationSpeed = 45f;

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip pickupSound;

        private GameObject _modelInstance;
        private Vector3 _initialLocalPos;
        private bool _isPickedUp;

        private void Awake()
        {
            SetupWorldModel();
            _initialLocalPos = _modelInstance != null ? _modelInstance.transform.localPosition : modelOffset;
        }

        private void SetupWorldModel()
        {
            if (customWorldModelPrefab != null)
            {
                _modelInstance = Instantiate(customWorldModelPrefab, transform);
            }
            else
            {
                _modelInstance = ShotgunWeapon.CreateDefaultShotgunViewmodel(transform);
            }

            _modelInstance.name = "Shotgun_WorldModel";
            _modelInstance.transform.localPosition = modelOffset;
            _modelInstance.transform.localRotation = Quaternion.Euler(modelRotation);
            _modelInstance.transform.localScale = modelScale;
        }

        private void Update()
        {
            if (_modelInstance == null || _isPickedUp) return;

            // Idle floating bob & rotate
            float newY = _initialLocalPos.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            _modelInstance.transform.localPosition = new Vector3(_initialLocalPos.x, newY, _initialLocalPos.z);
            _modelInstance.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        public string GetInteractionPrompt()
        {
            return "[E] Pegar Escopeta";
        }

        public void Interact()
        {
            if (_isPickedUp) return;

            var inventory = FindAnyObjectByType<PlayerWeaponInventory>();
            if (inventory != null)
            {
                _isPickedUp = true;
                inventory.UnlockShotgun(autoEquip: true);

                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position, 1.0f);
                }

                Destroy(gameObject);
            }
        }
    }
}
