using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using ProjectRac.Interaction;
using ProjectRac.Player;
using ProjectRac.Weapons;

namespace ProjectRac.UI
{
    public class NoteInspectionUI : MonoBehaviour
    {
        [Header("UI Component References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text hintText;

        public static bool IsInspecting { get; private set; }
        public static event Action<InspectableNote> OnNoteClosed;

        private FirstPersonController _fpsController;
        private FirstPersonCameraLook _cameraLook;
        private PlayerWeaponInventory _weaponInventory;
        private MonoBehaviour _pausedWeaponBehaviour;
        private CrosshairHUD _crosshairHUD;
        private AudioSource _audioSource;
        private InspectableNote _currentNote;
        private int _openedFrame = -1;

        private void Awake()
        {
            IsInspecting = false;
            if (panelRoot == null) panelRoot = gameObject;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 0f;
                _audioSource.playOnAwake = false;
            }

            ResolveTextReferences();
            SetPanelVisible(false);
        }

        private void OnDisable()
        {
            if (IsInspecting)
            {
                RestorePlayerControl();
                IsInspecting = false;
            }
        }

        private void Update()
        {
            if (!IsInspecting) return;

            // Ignore the same frame that opened the note so pressing E does not open and immediately close it.
            if (Time.frameCount <= _openedFrame) return;

            var kb = Keyboard.current;
            if (kb != null && (kb[Key.E].wasPressedThisFrame || kb[Key.Escape].wasPressedThisFrame))
            {
                CloseNote();
            }
        }

        public void BindComponents(GameObject root, Text title, Text body, Text hint)
        {
            panelRoot = root;
            titleText = title;
            bodyText = body;
            hintText = hint;
            SetPanelVisible(false);
        }

        public void OpenNote(string title, string body, AudioClip clip = null, InspectableNote sourceNote = null)
        {
            ResolveTextReferences();
            ResolvePlayerReferences();

            if (titleText != null) titleText.text = string.IsNullOrEmpty(title) ? "NOTA" : title.ToUpper();
            if (bodyText != null) bodyText.text = body ?? "";
            if (hintText != null) hintText.text = "[E] ou [ESC] Fechar Nota";

            _currentNote = sourceNote;
            IsInspecting = true;
            _openedFrame = Time.frameCount;

            PausePlayerControl();
            SetPanelVisible(true);

            if (clip != null && _audioSource != null) _audioSource.PlayOneShot(clip, 0.85f);
        }

        public void CloseNote()
        {
            if (!IsInspecting) return;

            InspectableNote closedNote = _currentNote;
            _currentNote = null;
            IsInspecting = false;
            SetPanelVisible(false);
            RestorePlayerControl();
            OnNoteClosed?.Invoke(closedNote);
        }

        private void PausePlayerControl()
        {
            if (_fpsController != null) _fpsController.enabled = false;
            if (_cameraLook != null) _cameraLook.enabled = false;

            if (_weaponInventory != null)
            {
                _pausedWeaponBehaviour = _weaponInventory.CurrentWeapon as MonoBehaviour;
                if (_pausedWeaponBehaviour != null) _pausedWeaponBehaviour.enabled = false;
                _weaponInventory.enabled = false;
            }

            _crosshairHUD?.SetTargetingInteractable(false, "");
        }

        private void RestorePlayerControl()
        {
            if (_fpsController != null) _fpsController.enabled = true;
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
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRoot != null && panelRoot != gameObject)
            {
                panelRoot.SetActive(visible);
                return;
            }

            // Keep this MonoBehaviour active so Update() can still listen for E/Esc while toggling child visuals.
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(visible);
            }
        }

        private void ResolveTextReferences()
        {
            if (titleText == null) titleText = transform.Find("Backdrop/PaperCard/TitleText")?.GetComponent<Text>();
            if (bodyText == null) bodyText = transform.Find("Backdrop/PaperCard/BodyText")?.GetComponent<Text>();
            if (hintText == null) hintText = transform.Find("Backdrop/PaperCard/HintText")?.GetComponent<Text>();
        }

        private void ResolvePlayerReferences()
        {
            if (_fpsController == null) _fpsController = FindAnyObjectByType<FirstPersonController>();
            if (_cameraLook == null) _cameraLook = FindAnyObjectByType<FirstPersonCameraLook>();
            if (_weaponInventory == null) _weaponInventory = FindAnyObjectByType<PlayerWeaponInventory>();
            if (_crosshairHUD == null) _crosshairHUD = FindAnyObjectByType<CrosshairHUD>();
        }
    }
}
