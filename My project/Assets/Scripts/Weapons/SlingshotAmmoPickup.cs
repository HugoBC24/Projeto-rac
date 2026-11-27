using UnityEngine;
using ProjectRac.Interaction;

namespace ProjectRac.Weapons
{
    [RequireComponent(typeof(Collider))]
    public class SlingshotAmmoPickup : MonoBehaviour, IInteractable
    {
        [Header("Ammo Settings")]
        [SerializeField] private int ammoAmount = 6;
        [SerializeField] private AudioClip pickupSound;

        private bool _isPickedUp;

        public int AmmoAmount => ammoAmount;

        public void Configure(int amount)
        {
            ammoAmount = Mathf.Max(1, amount);
        }

        public string GetInteractionPrompt() => $"[E] Pegar Bolinhas (+{ammoAmount})";

        public void Interact()
        {
            if (_isPickedUp) return;

            var slingshot = FindAnyObjectByType<SlingshotWeapon>(FindObjectsInactive.Include);
            if (slingshot == null) return;

            if (slingshot.AddAmmo(ammoAmount) <= 0) return;

            _isPickedUp = true;

            var drawer = GetComponentInParent<DrawerController>();
            if (drawer != null)
            {
                drawer.ClearContainedInteractable();
            }

            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, 0.9f);
            }

            Destroy(gameObject);
        }
    }
}
