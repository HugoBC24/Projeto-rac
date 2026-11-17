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
        [SerializeField] private Vector3 modelOffset = new Vector3(0f, 0.05f, 0f);
        [SerializeField] private Vector3 modelRotation = new Vector3(0f, 35f, 0f);
        [SerializeField] private Vector3 modelScale = new Vector3(1f, 1f, 1f);

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip pickupSound;

        private GameObject _modelInstance;
        private bool _isPickedUp;

        private void Awake()
        {
            EnsureSolidCollider();
            ResolveWorldModel();
        }

        private void EnsureSolidCollider()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = false;
            }
            else
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(0.55f, 0.35f, 1.05f);
                box.center = new Vector3(0f, 0.12f, 0f);
                box.isTrigger = false;
            }
        }

        public void ResolveWorldModel()
        {
            var existingChild = transform.Find("Shotgun_WorldModel");
            if (existingChild != null)
            {
                _modelInstance = existingChild.gameObject;
                _modelInstance.transform.localPosition = modelOffset;
                _modelInstance.transform.localRotation = Quaternion.Euler(modelRotation);
                _modelInstance.transform.localScale = modelScale;
                return;
            }

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
