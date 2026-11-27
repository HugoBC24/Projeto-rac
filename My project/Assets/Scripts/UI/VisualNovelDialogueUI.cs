using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace ProjectRac.UI
{
    public class VisualNovelDialogueUI : MonoBehaviour
    {
        [Header("Dialogue Entries")]
        [SerializeField] private List<VisualNovelDialogueLine> dialogueLines = new List<VisualNovelDialogueLine>();

        [Header("Behavior")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool allowSkipWithEscape = true;
        [SerializeField] private bool hideGameplayHud = true;
        [SerializeField] private bool hideWeaponViewmodel = true;
        [SerializeField] private float charactersPerSecond = 42f;
        [SerializeField] private bool dimInactiveSpeakers = true;
        [SerializeField] private Color inactiveSpeakerColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private UnityEvent onDialogueFinished;

        [Header("UI Component References")]
        [SerializeField] private GameObject dialogueRoot;
        [SerializeField] private GameObject speakerNameBox;
        [SerializeField] private UnityEngine.UI.Text speakerNameText;
        [SerializeField] private UnityEngine.UI.Text dialogueText;
        [SerializeField] private RectTransform dialogueTextRect;
        [SerializeField] private RectTransform continueIndicator;
        [SerializeField] private UnityEngine.UI.Image leftPortrait;
        [SerializeField] private UnityEngine.UI.Image centerPortrait;
        [SerializeField] private UnityEngine.UI.Image rightPortrait;

        public static bool IsActive { get; private set; }
        public event Action<int, bool> OnLineStarted;
        public int CurrentLineIndex => _currentIndex;
        public int LineCount => dialogueLines != null ? dialogueLines.Count : 0;
        public bool IsTyping => _isTyping;

        private const float ReleaseTimeoutSeconds = 0.35f;

        private readonly VisualNovelGameplayLock _gameplayLock = new VisualNovelGameplayLock();
        private AudioSource _audioSource;
        private Coroutine _restoreRoutine;
        private List<VisualNovelDialogueLine> _activeLines;
        private Action _onSequenceComplete;
        private int _currentIndex = -1, _openedFrame = -1;
        private bool _isDialogueVisible, _isTyping, _hasContinueBasePos, _isIntroSequence;
        private string _currentFullText = "";
        private float _revealedChars;
        private Vector2 _continueBasePos;

        private void Awake()
        {
            IsActive = false;
            _isDialogueVisible = false;
            if (dialogueRoot == null) dialogueRoot = gameObject;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 0f;
                _audioSource.playOnAwake = false;
            }

            if (continueIndicator != null)
            {
                _continueBasePos = continueIndicator.anchoredPosition;
                _hasContinueBasePos = true;
            }

            SetVisualsVisible(false);
        }

        private void Start()
        {
            if (playOnStart && dialogueLines != null && dialogueLines.Count > 0)
            {
                StartDialogue();
                StartCoroutine(EnforceInitialPauseAfterStart());
            }
        }

        private void OnDisable()
        {
            if (IsActive)
            {
                _gameplayLock.Restore();
                IsActive = false;
                _isDialogueVisible = false;
            }
        }

        private void Update()
        {
            if (!_isDialogueVisible) return;

            UpdateTypewriter();
            AnimateContinueIndicator();
            if (Time.frameCount <= _openedFrame) return;

            var kb = Keyboard.current;
            var mouse = Mouse.current;

            if (allowSkipWithEscape && kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                SkipDialogue();
                return;
            }

            bool leftClick = mouse != null && mouse.leftButton.wasPressedThisFrame;
            bool spaceKey = kb != null && kb.spaceKey.wasPressedThisFrame;
            if (leftClick || spaceKey) AdvanceDialogue();
        }

        public void BindComponents(
            GameObject root, GameObject nameBox, UnityEngine.UI.Text nameText, UnityEngine.UI.Text bodyText,
            RectTransform indicator, UnityEngine.UI.Image left, UnityEngine.UI.Image center, UnityEngine.UI.Image right)
        {
            dialogueRoot = root;
            speakerNameBox = nameBox;
            speakerNameText = nameText;
            dialogueText = bodyText;
            dialogueTextRect = bodyText != null ? bodyText.rectTransform : null;
            continueIndicator = indicator;
            leftPortrait = left;
            centerPortrait = center;
            rightPortrait = right;
            if (indicator != null)
            {
                _continueBasePos = indicator.anchoredPosition;
                _hasContinueBasePos = true;
            }
        }

        public void SetDialogueLines(List<VisualNovelDialogueLine> lines) => dialogueLines = lines ?? new List<VisualNovelDialogueLine>();

        public void SkipDialogue()
        {
            if (_isDialogueVisible) CloseDialogue();
        }

        public void StartDialogue() => StartDialogue(dialogueLines, null);

        public void StartDialogue(List<VisualNovelDialogueLine> customLines, Action onComplete = null)
        {
            if (customLines == null || customLines.Count == 0) { onComplete?.Invoke(); return; }
            if (_restoreRoutine != null) { StopCoroutine(_restoreRoutine); _restoreRoutine = null; }

            _activeLines = customLines;
            _isIntroSequence = ReferenceEquals(customLines, dialogueLines);
            _onSequenceComplete = onComplete;
            _gameplayLock.Pause(transform, hideGameplayHud, hideWeaponViewmodel);
            IsActive = true;
            _isDialogueVisible = true;
            _openedFrame = Time.frameCount;
            _currentIndex = 0;

            VisualNovelPortraitStage.ClearAll(leftPortrait, centerPortrait, rightPortrait);
            SetVisualsVisible(true);
            DisplayLine(_currentIndex);
        }

        public void AdvanceDialogue()
        {
            if (!_isDialogueVisible) return;
            if (_isTyping) { CompleteCurrentLine(); return; }

            _currentIndex++;
            if (_activeLines == null || _currentIndex >= _activeLines.Count) CloseDialogue();
            else DisplayLine(_currentIndex);
        }

        private void DisplayLine(int index)
        {
            VisualNovelDialogueLine line = _activeLines[index] ?? new VisualNovelDialogueLine();
            bool hasSpeaker = !string.IsNullOrWhiteSpace(line.speakerName);
            if (speakerNameBox != null) speakerNameBox.SetActive(hasSpeaker);
            if (speakerNameText != null && hasSpeaker)
            {
                speakerNameText.text = line.speakerName;
                speakerNameText.color = line.speakerNameColor.a <= 0.01f ? Color.white : line.speakerNameColor;
            }

            VisualNovelPortraitStage.ApplyLine(line, leftPortrait, centerPortrait, rightPortrait, dimInactiveSpeakers, inactiveSpeakerColor);
            if (dialogueTextRect == null && dialogueText != null) dialogueTextRect = dialogueText.rectTransform;
            VisualNovelPortraitStage.ApplySafeTextMargins(dialogueTextRect, leftPortrait, rightPortrait);

            _currentFullText = line.text ?? "";
            if (dialogueText != null) dialogueText.supportRichText = true;

            if (charactersPerSecond <= 0f || _currentFullText.Length == 0)
            {
                CompleteCurrentLine();
            }
            else
            {
                _revealedChars = 0f;
                _isTyping = true;
                ApplyRevealedText(0);
                if (continueIndicator != null) continueIndicator.gameObject.SetActive(false);
            }

            if (line.voiceClip != null && _audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.PlayOneShot(line.voiceClip, 0.9f);
            }

            OnLineStarted?.Invoke(index, _isIntroSequence);
        }

        private void UpdateTypewriter()
        {
            if (!_isTyping) return;
            _revealedChars += charactersPerSecond * Time.unscaledDeltaTime;
            int count = Mathf.FloorToInt(_revealedChars);
            if (count >= _currentFullText.Length) CompleteCurrentLine();
            else ApplyRevealedText(count);
        }

        private void CompleteCurrentLine()
        {
            _isTyping = false;
            _revealedChars = _currentFullText.Length;
            if (dialogueText != null) dialogueText.text = _currentFullText;
            if (continueIndicator != null) continueIndicator.gameObject.SetActive(true);
        }

        private void ApplyRevealedText(int visibleCount)
        {
            if (dialogueText == null) return;
            int clamped = Mathf.Clamp(visibleCount, 0, _currentFullText.Length);
            dialogueText.text = clamped >= _currentFullText.Length
                ? _currentFullText
                : _currentFullText.Substring(0, clamped) + "<color=#00000000>" + _currentFullText.Substring(clamped) + "</color>";
        }

        private void AnimateContinueIndicator()
        {
            if (continueIndicator == null || !continueIndicator.gameObject.activeSelf || !_hasContinueBasePos) return;
            continueIndicator.anchoredPosition = _continueBasePos + new Vector2(0f, Mathf.Sin(Time.unscaledTime * 5.5f) * 3.5f);
        }

        private void CloseDialogue()
        {
            _isDialogueVisible = false;
            _isTyping = false;
            SetVisualsVisible(false);
            if (_restoreRoutine != null) StopCoroutine(_restoreRoutine);
            _restoreRoutine = StartCoroutine(RestoreControlAfterInputRelease());
        }

        private IEnumerator EnforceInitialPauseAfterStart()
        {
            yield return null;
            if (IsActive) _gameplayLock.EnforcePostStartPause(hideWeaponViewmodel);
        }

        private IEnumerator RestoreControlAfterInputRelease()
        {
            // Defer gameplay restore until Space / Left Click are released so closing never triggers jump or attack.
            yield return null;
            float elapsed = 0f;
            while (elapsed < ReleaseTimeoutSeconds && IsAdvanceInputHeld())
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _gameplayLock.Restore();
            IsActive = false;
            _restoreRoutine = null;
            Action callback = _onSequenceComplete;
            _onSequenceComplete = null;
            callback?.Invoke();
            onDialogueFinished?.Invoke();
        }

        private static bool IsAdvanceInputHeld()
        {
            return (Mouse.current != null && Mouse.current.leftButton.isPressed)
                || (Keyboard.current != null && Keyboard.current.spaceKey.isPressed);
        }

        private void SetVisualsVisible(bool visible)
        {
            if (dialogueRoot != null && dialogueRoot != gameObject)
            {
                dialogueRoot.SetActive(visible);
                return;
            }

            for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(visible);
        }
    }
}
