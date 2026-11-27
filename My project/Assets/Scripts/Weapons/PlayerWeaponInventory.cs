using UnityEngine;
using UnityEngine.InputSystem;
using ProjectRac.UI;

namespace ProjectRac.Weapons
{
    public class PlayerWeaponInventory : MonoBehaviour
    {
        [Header("Weapon References")]
        [SerializeField] private SlingshotWeapon slingshotWeapon;
        [SerializeField] private ShotgunWeapon shotgunWeapon;

        [Header("Inventory Status")]
        [SerializeField] private bool hasSlingshot = false;
        [SerializeField] private bool hasShotgun = false;
        [SerializeField] private int activeSlot = 0;

        [Header("UI Reference")]
        [SerializeField] private AmmoHUD ammoHUD;

        public bool HasSlingshot => hasSlingshot;
        public bool HasShotgun => hasShotgun;
        public int ActiveSlot => activeSlot;
        public IWeapon CurrentWeapon => activeSlot == 2 ? (IWeapon)shotgunWeapon : (activeSlot == 1 ? (IWeapon)slingshotWeapon : null);

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            SelectSlot(activeSlot, force: true);
        }

        public void BindWeapons(SlingshotWeapon slingshot, ShotgunWeapon shotgun, AmmoHUD hud = null)
        {
            slingshotWeapon = slingshot;
            shotgunWeapon = shotgun;
            if (hud != null) ammoHUD = hud;

            SelectSlot(activeSlot, force: true);
        }

        public void BindAmmoHUD(AmmoHUD hud)
        {
            ammoHUD = hud;
            if (slingshotWeapon != null) slingshotWeapon.BindAmmoHUD(hud);
            if (shotgunWeapon != null) shotgunWeapon.BindAmmoHUD(hud);
        }

        private void ResolveReferences()
        {
            if (slingshotWeapon == null)
            {
                slingshotWeapon = GetComponentInChildren<SlingshotWeapon>(true);
            }

            if (shotgunWeapon == null)
            {
                shotgunWeapon = GetComponentInChildren<ShotgunWeapon>(true);
            }

            if (ammoHUD == null)
            {
                ammoHUD = FindAnyObjectByType<AmmoHUD>();
            }
        }

        private void Update()
        {
            HandleWeaponSwitchInput();
        }

        private void HandleWeaponSwitchInput()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if ((kb[Key.Digit1].wasPressedThisFrame || kb[Key.Numpad1].wasPressedThisFrame) && hasSlingshot && activeSlot != 1)
                {
                    SelectSlot(1);
                    return;
                }

                if ((kb[Key.Digit2].wasPressedThisFrame || kb[Key.Numpad2].wasPressedThisFrame) && hasShotgun && activeSlot != 2)
                {
                    SelectSlot(2);
                    return;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.1f && hasSlingshot && activeSlot != 1)
                {
                    SelectSlot(1);
                }
                else if (scroll < -0.1f && hasShotgun && activeSlot != 2)
                {
                    SelectSlot(2);
                }
            }
        }

        public void SelectSlot(int slot, bool force = false)
        {
            if (!force && slot == activeSlot) return;
            if (slot == 1 && !hasSlingshot) slot = hasShotgun ? 2 : 0;
            if (slot == 2 && !hasShotgun) slot = hasSlingshot ? 1 : 0;

            activeSlot = slot;

            if (activeSlot == 1)
            {
                shotgunWeapon?.Unequip();
                slingshotWeapon?.Equip();
                ammoHUD?.SetVisible(true);
            }
            else if (activeSlot == 2)
            {
                slingshotWeapon?.Unequip();
                shotgunWeapon?.Equip();
                ammoHUD?.SetVisible(true);
            }
            else
            {
                slingshotWeapon?.Unequip();
                shotgunWeapon?.Unequip();
                ammoHUD?.SetVisible(false);
            }
        }

        public void UnlockSlingshot(int initialAmmo = 6, bool autoEquip = true)
        {
            hasSlingshot = true;
            if (slingshotWeapon != null && initialAmmo > 0) slingshotWeapon.AddAmmo(initialAmmo);
            if (autoEquip) SelectSlot(1, force: true);
        }

        public void UnlockShotgun(int totalAmmo = 30, bool autoEquip = true)
        {
            hasShotgun = true;
            if (shotgunWeapon != null && totalAmmo > 0) shotgunWeapon.SetTotalAmmo(totalAmmo);
            if (autoEquip) SelectSlot(2, force: true);
        }

        public void SetStartingLoadout(bool startSlingshot, bool startShotgun)
        {
            hasSlingshot = startSlingshot;
            hasShotgun = startShotgun;
            activeSlot = startSlingshot ? 1 : (startShotgun ? 2 : 0);
        }
    }
}
