using UnityEngine;
using ProjectRac.Player;
using ProjectRac.UI;

namespace ProjectRac.Weapons
{
    public class PlayerWeaponInventory : MonoBehaviour
    {
        [Header("Weapon References")]
        [SerializeField] private SlingshotWeapon slingshotWeapon;
        [SerializeField] private ShotgunWeapon shotgunWeapon;

        [Header("Inventory Status")]
        [SerializeField] private bool hasShotgun = false;
        [SerializeField] private int activeSlot = 1;

        [Header("UI Reference")]
        [SerializeField] private WeaponHUD weaponHUD;

        public bool HasShotgun => hasShotgun;
        public int ActiveSlot => activeSlot;
        public IWeapon CurrentWeapon => activeSlot == 2 ? (IWeapon)shotgunWeapon : (IWeapon)slingshotWeapon;

        private PlayerInputHandler _inputHandler;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            // Initialize with default Slot 1 (Slingshot)
            SelectSlot(activeSlot, force: true);
        }

        public void BindWeapons(SlingshotWeapon slingshot, ShotgunWeapon shotgun, WeaponHUD hud = null)
        {
            slingshotWeapon = slingshot;
            shotgunWeapon = shotgun;
            if (hud != null) weaponHUD = hud;

            SelectSlot(activeSlot, force: true);
        }

        public void BindWeaponHUD(WeaponHUD hud)
        {
            weaponHUD = hud;
            weaponHUD?.UpdateActiveSlot(activeSlot, hasShotgun);
        }

        private void ResolveReferences()
        {
            if (_inputHandler == null)
            {
                _inputHandler = GetComponent<PlayerInputHandler>();
                if (_inputHandler == null) _inputHandler = GetComponentInParent<PlayerInputHandler>();
            }

            if (slingshotWeapon == null)
            {
                slingshotWeapon = GetComponentInChildren<SlingshotWeapon>(true);
            }

            if (shotgunWeapon == null)
            {
                shotgunWeapon = GetComponentInChildren<ShotgunWeapon>(true);
            }

            if (weaponHUD == null)
            {
                weaponHUD = FindAnyObjectByType<WeaponHUD>();
            }
        }

        private void Update()
        {
            if (_inputHandler == null) return;

            if (_inputHandler.QuickSlot1Triggered && activeSlot != 1)
            {
                SelectSlot(1);
            }
            else if (_inputHandler.QuickSlot2Triggered && activeSlot != 2)
            {
                SelectSlot(2);
            }
        }

        public void SelectSlot(int slot, bool force = false)
        {
            if (!force && slot == activeSlot) return;

            if (slot == 2 && !hasShotgun)
            {
                // Shotgun not yet acquired
                weaponHUD?.FlashLockedSlot(2);
                return;
            }

            activeSlot = slot;

            if (activeSlot == 1)
            {
                shotgunWeapon?.Unequip();
                slingshotWeapon?.Equip();
            }
            else if (activeSlot == 2)
            {
                slingshotWeapon?.Unequip();
                shotgunWeapon?.Equip();
            }

            weaponHUD?.UpdateActiveSlot(activeSlot, hasShotgun);
        }

        public void UnlockShotgun(bool autoEquip = true)
        {
            hasShotgun = true;

            if (autoEquip)
            {
                SelectSlot(2);
            }
            else
            {
                weaponHUD?.UpdateActiveSlot(activeSlot, hasShotgun);
            }
        }
    }
}
