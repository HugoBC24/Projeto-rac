using UnityEngine;
using ProjectRac.Interaction;

namespace ProjectRac.Weapons
{
    [RequireComponent(typeof(Collider))]
    public class SlingshotPickup : MonoBehaviour, IInteractable
    {
        [Header("Pickup Settings")]
        [SerializeField] private int initialAmmo = 6;
        [SerializeField] private string prompt = "[E] Pegar Estilingue";
        [SerializeField] private AudioClip pickupSound;

        private bool _isPickedUp;

        public void Configure(int ammo)
        {
            initialAmmo = Mathf.Max(0, ammo);
        }

        public string GetInteractionPrompt() => prompt;

        public void Interact()
        {
            if (_isPickedUp) return;

            var inventory = FindAnyObjectByType<PlayerWeaponInventory>();
            if (inventory == null) return;

            _isPickedUp = true;
            inventory.UnlockSlingshot(initialAmmo, autoEquip: true);

            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, 1.0f);
            }

            Destroy(gameObject);
        }
    }
}
