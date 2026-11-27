using System.Collections.Generic;
using UnityEngine;
using ProjectRac.Player;
using ProjectRac.Weapons;

namespace ProjectRac.UI
{
    // Isolates player movement, look, input map, HUD, and weapon viewmodel while VN dialogue is active.
    internal sealed class VisualNovelGameplayLock
    {
        private PlayerInputHandler _inputHandler;
        private FirstPersonController _fpsController;
        private FirstPersonCameraLook _cameraLook;
        private PS2CameraFeel _cameraFeel;
        private PlayerWeaponInventory _weaponInventory;
        private MonoBehaviour _pausedWeaponBehaviour;
        private CrosshairHUD _crosshairHUD;

        private readonly List<GameObject> _hiddenHudObjects = new List<GameObject>();
        private readonly List<Renderer> _hiddenWeaponRenderers = new List<Renderer>();

        public void ResolveReferences()
        {
            if (_inputHandler == null) _inputHandler = Object.FindAnyObjectByType<PlayerInputHandler>();
            if (_fpsController == null) _fpsController = Object.FindAnyObjectByType<FirstPersonController>();
            if (_cameraLook == null) _cameraLook = Object.FindAnyObjectByType<FirstPersonCameraLook>();
            if (_cameraFeel == null) _cameraFeel = Object.FindAnyObjectByType<PS2CameraFeel>();
            if (_weaponInventory == null) _weaponInventory = Object.FindAnyObjectByType<PlayerWeaponInventory>();
            if (_crosshairHUD == null) _crosshairHUD = Object.FindAnyObjectByType<CrosshairHUD>();
        }

        public void Pause(Transform dialoguePanelTransform, bool hideHud, bool hideWeapon)
        {
            ResolveReferences();

            if (_inputHandler != null)
            {
                _inputHandler.ClearBufferedInputs();
                _inputHandler.enabled = false;
            }

            if (_fpsController != null) _fpsController.enabled = false;
            if (_cameraFeel != null) _cameraFeel.enabled = false;
            if (_cameraLook != null)
            {
                _cameraLook.enabled = false;
                _cameraLook.UnlockCursor();
            }

            if (_weaponInventory != null)
            {
                _pausedWeaponBehaviour = _weaponInventory.CurrentWeapon as MonoBehaviour;
                if (_pausedWeaponBehaviour != null) _pausedWeaponBehaviour.enabled = false;
                _weaponInventory.enabled = false;
            }

            _crosshairHUD?.SetTargetingInteractable(false, "");

            if (hideHud) HideSiblingHud(dialoguePanelTransform);
            if (hideWeapon) HideActiveWeaponRenderers();
        }

        public void EnforcePostStartPause(bool hideWeapon)
        {
            if (_cameraLook != null) _cameraLook.UnlockCursor();
            if (hideWeapon) HideActiveWeaponRenderers();
        }

        public void Restore()
        {
            if (_inputHandler != null)
            {
                _inputHandler.ClearBufferedInputs();
                _inputHandler.enabled = true;
            }

            if (_fpsController != null) _fpsController.enabled = true;
            if (_cameraFeel != null) _cameraFeel.enabled = true;
            if (_cameraLook != null)
            {
                _cameraLook.enabled = true;
                _cameraLook.LockCursor();
            }

            if (_weaponInventory != null) _weaponInventory.enabled = true;
            if (_pausedWeaponBehaviour != null)
            {
                _pausedWeaponBehaviour.enabled = true;
                _pausedWeaponBehaviour = null;
            }

            for (int i = 0; i < _hiddenHudObjects.Count; i++)
            {
                if (_hiddenHudObjects[i] != null) _hiddenHudObjects[i].SetActive(true);
            }
            _hiddenHudObjects.Clear();

            for (int i = 0; i < _hiddenWeaponRenderers.Count; i++)
            {
                if (_hiddenWeaponRenderers[i] != null) _hiddenWeaponRenderers[i].enabled = true;
            }
            _hiddenWeaponRenderers.Clear();
        }

        private void HideSiblingHud(Transform dialoguePanelTransform)
        {
            Transform canvas = dialoguePanelTransform != null ? dialoguePanelTransform.parent : null;
            if (canvas == null) return;

            for (int i = 0; i < canvas.childCount; i++)
            {
                GameObject child = canvas.GetChild(i).gameObject;
                if (child == dialoguePanelTransform.gameObject || !child.activeSelf) continue;
                if (child.name == "NoteInspectionPanel") continue;
                child.SetActive(false);
                _hiddenHudObjects.Add(child);
            }
        }

        private void HideActiveWeaponRenderers()
        {
            // Weapon viewmodels (Slingshot_Viewmodel, Shotgun_Viewmodel) are parented under FirstPersonCamera.
            if (_cameraLook != null) HideRenderersUnder(_cameraLook.transform);

            if (_weaponInventory != null && _weaponInventory.CurrentWeapon is Component weaponComp)
            {
                HideRenderersUnder(weaponComp.transform);
            }
        }

        private void HideRenderersUnder(Transform root)
        {
            if (root == null) return;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].enabled && !_hiddenWeaponRenderers.Contains(renderers[i]))
                {
                    renderers[i].enabled = false;
                    _hiddenWeaponRenderers.Add(renderers[i]);
                }
            }
        }
    }
}
