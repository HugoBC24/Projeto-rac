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
        [SerializeField] private bool hasShotgun = false;
        [SerializeField] private int activeSlot = 1;

        [Header("UI Reference")]
        [SerializeField] private AmmoHUD ammoHUD;

        public bool HasShotgun => hasShotgun;
        public int ActiveSlot => activeSlot;
        public IWeapon CurrentWeapon => activeSlot == 2 ? (IWeapon)shotgunWeapon : (IWeapon)slingshotWeapon;

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
                if ((kb[Key.Digit1].wasPressedThisFrame || kb[Key.Numpad1].wasPressedThisFrame) && activeSlot != 1)
                {
                    SelectSlot(1);
                    return;
                }

                if ((kb[Key.Digit2].wasPressedThisFrame || kb[Key.Numpad2].wasPressedThisFrame) && activeSlot != 2)
                {
                    SelectSlot(2);
                    return;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.1f && activeSlot != 1)
                {
                    SelectSlot(1);
                }
                else if (scroll < -0.1f && activeSlot != 2 && hasShotgun)
                {
                    SelectSlot(2);
                }
            }
        }

        public void SelectSlot(int slot, bool force = false)
        {
            if (!force && slot == activeSlot) return;

            if (slot == 2 && !hasShotgun)
            {
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
        }

        public void UnlockShotgun(bool autoEquip = true)
        {
            hasShotgun = true;

            if (autoEquip)
            {
                SelectSlot(2);
            }
        }
    }
}
