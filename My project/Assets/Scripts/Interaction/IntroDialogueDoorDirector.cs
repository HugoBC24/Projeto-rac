using System.Collections;
using UnityEngine;
using ProjectRac.Player;
using ProjectRac.UI;

namespace ProjectRac.Interaction
{
    // Directs intro dialogue beats on the main entrance door:
    // - At dialogue 3 (1-based): smoothly turns the player camera toward the door.
    // - At dialogue 5 (1-based): fades in world-space text on the door from alpha 0% to 100% (stays visible).
    public class IntroDialogueDoorDirector : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private VisualNovelDialogueUI visualNovelUI;
        [SerializeField] private Transform doorFocusTarget;
        [SerializeField] private CanvasGroup doorTextCanvasGroup;
        [SerializeField] private UnityEngine.UI.Text doorTextLabel;

        [Header("Dialogue Timing (1-Based Dialogue Numbers)")]
        [SerializeField] private int turnCameraAtDialogue = 3;
        [SerializeField] private int revealTextAtDialogue = 5;
        [SerializeField] private int hideTextAtDialogue = 6;

        [Header("Camera & Fade Settings")]
        [SerializeField] private float cameraTurnDuration = 0.75f;
        [SerializeField] private float cameraPitchToDoor = 2f;
        [SerializeField] private float fadeInDuration = 1.2f;
        [SerializeField] private float fadeOutDuration = 0.45f;
        [SerializeField, TextArea(2, 4)] private string doorMessage = "NAO HA SAIDA";

        public float CurrentDoorTextAlpha => doorTextCanvasGroup != null ? doorTextCanvasGroup.alpha : 0f;
        public bool HasTurnedToDoor => _hasTurnedToDoor;
        public bool HasRevealedText => _hasRevealedText;

        private bool _hasTurnedToDoor;
        private bool _hasRevealedText;
        private Coroutine _fadeRoutine;

        private void Awake()
        {
            if (doorTextLabel != null && !string.IsNullOrEmpty(doorMessage))
            {
                doorTextLabel.text = doorMessage;
            }

            if (doorTextCanvasGroup != null)
            {
                doorTextCanvasGroup.alpha = 0f;
            }
        }

        private void OnEnable()
        {
            ResolveVisualNovelUI();
            if (visualNovelUI != null) visualNovelUI.OnLineStarted += HandleLineStarted;
        }

        private void OnDisable()
        {
            if (visualNovelUI != null) visualNovelUI.OnLineStarted -= HandleLineStarted;
        }

        public void Configure(
            VisualNovelDialogueUI vnUI,
            Transform focusTarget,
            CanvasGroup canvasGroup,
            UnityEngine.UI.Text label,
            string message)
        {
            visualNovelUI = vnUI;
            doorFocusTarget = focusTarget;
            doorTextCanvasGroup = canvasGroup;
            doorTextLabel = label;
            doorMessage = message;
            if (doorTextLabel != null) doorTextLabel.text = doorMessage;
            if (doorTextCanvasGroup != null) doorTextCanvasGroup.alpha = 0f;
        }

        private void HandleLineStarted(int zeroBasedIndex, bool isIntroDialogue)
        {
            if (!isIntroDialogue) return;

            int dialogueNumber = zeroBasedIndex + 1;

            if (dialogueNumber == turnCameraAtDialogue && !_hasTurnedToDoor)
            {
                _hasTurnedToDoor = true;
                TurnCameraToDoor();
            }

            if (dialogueNumber == revealTextAtDialogue && !_hasRevealedText)
            {
                _hasRevealedText = true;
                RevealDoorText();
            }

            if (hideTextAtDialogue > 0 && dialogueNumber == hideTextAtDialogue)
            {
                HideDoorText();
            }
        }

        private void TurnCameraToDoor()
        {
            GameObject playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (playerObj == null || doorFocusTarget == null) return;

            SmoothCameraTurn.TurnToTarget(
                this,
                playerObj.transform,
                doorFocusTarget.position,
                cameraTurnDuration,
                cameraPitchToDoor,
                null);
        }

        private void RevealDoorText()
        {
            if (doorTextCanvasGroup == null) return;
            if (doorTextLabel != null && !string.IsNullOrEmpty(doorMessage))
            {
                doorTextLabel.text = doorMessage;
            }

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeAlphaRoutine(0f, 1f, fadeInDuration));
        }

        private void HideDoorText()
        {
            if (doorTextCanvasGroup == null) return;
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeAlphaRoutine(doorTextCanvasGroup.alpha, 0f, fadeOutDuration));
        }

        private IEnumerator FadeAlphaRoutine(float fromAlpha, float toAlpha, float duration)
        {
            doorTextCanvasGroup.gameObject.SetActive(true);
            float safeDuration = Mathf.Max(0.05f, duration);
            float elapsed = 0f;
            doorTextCanvasGroup.alpha = fromAlpha;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                doorTextCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / safeDuration));
                yield return null;
            }

            doorTextCanvasGroup.alpha = toAlpha;
            _fadeRoutine = null;
        }

        private void ResolveVisualNovelUI()
        {
            if (visualNovelUI == null)
            {
                visualNovelUI = FindAnyObjectByType<VisualNovelDialogueUI>(FindObjectsInactive.Include);
            }
        }
    }
}
