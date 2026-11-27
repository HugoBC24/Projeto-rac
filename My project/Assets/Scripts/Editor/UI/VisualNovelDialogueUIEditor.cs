using UnityEditor;
using UnityEngine;
using ProjectRac.UI;

namespace ProjectRac.Editor.UI
{
    [CustomEditor(typeof(VisualNovelDialogueUI))]
    public class VisualNovelDialogueUIEditor : UnityEditor.Editor
    {
        private SerializedProperty _dialogueLines;
        private SerializedProperty _playOnStart;
        private SerializedProperty _allowSkipWithEscape;
        private SerializedProperty _hideGameplayHud;
        private SerializedProperty _hideWeaponViewmodel;
        private SerializedProperty _charactersPerSecond;
        private SerializedProperty _dimInactiveSpeakers;
        private SerializedProperty _inactiveSpeakerColor;
        private SerializedProperty _onDialogueFinished;

        private SerializedProperty _dialogueRoot;
        private SerializedProperty _speakerNameBox;
        private SerializedProperty _speakerNameText;
        private SerializedProperty _dialogueText;
        private SerializedProperty _dialogueTextRect;
        private SerializedProperty _continueIndicator;
        private SerializedProperty _leftPortrait;
        private SerializedProperty _centerPortrait;
        private SerializedProperty _rightPortrait;

        private bool _showAdvanced;
        private GUIStyle _textAreaStyle;

        private void OnEnable()
        {
            _dialogueLines = serializedObject.FindProperty("dialogueLines");
            _playOnStart = serializedObject.FindProperty("playOnStart");
            _allowSkipWithEscape = serializedObject.FindProperty("allowSkipWithEscape");
            _hideGameplayHud = serializedObject.FindProperty("hideGameplayHud");
            _hideWeaponViewmodel = serializedObject.FindProperty("hideWeaponViewmodel");
            _charactersPerSecond = serializedObject.FindProperty("charactersPerSecond");
            _dimInactiveSpeakers = serializedObject.FindProperty("dimInactiveSpeakers");
            _inactiveSpeakerColor = serializedObject.FindProperty("inactiveSpeakerColor");
            _onDialogueFinished = serializedObject.FindProperty("onDialogueFinished");

            _dialogueRoot = serializedObject.FindProperty("dialogueRoot");
            _speakerNameBox = serializedObject.FindProperty("speakerNameBox");
            _speakerNameText = serializedObject.FindProperty("speakerNameText");
            _dialogueText = serializedObject.FindProperty("dialogueText");
            _dialogueTextRect = serializedObject.FindProperty("dialogueTextRect");
            _continueIndicator = serializedObject.FindProperty("continueIndicator");
            _leftPortrait = serializedObject.FindProperty("leftPortrait");
            _centerPortrait = serializedObject.FindProperty("centerPortrait");
            _rightPortrait = serializedObject.FindProperty("rightPortrait");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (_textAreaStyle == null)
            {
                _textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            }

            EditorGUILayout.LabelField("Dialogo Inicial (Visual Novel)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Defina abaixo quantos dialogos vao aparecer e escreva o texto de cada fala. Avanca com Clique Esquerdo ou Barra de Espaco.",
                MessageType.Info);

            DrawDialogueCountHeader();
            EditorGUILayout.Space(4f);
            DrawDialogueCards();
            EditorGUILayout.Space(8f);
            DrawAdvancedSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDialogueCountHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            int currentCount = _dialogueLines.arraySize;
            int newCount = Mathf.Clamp(EditorGUILayout.IntField("Quantidade de Dialogos", currentCount), 0, 100);
            if (newCount != currentCount)
            {
                ResizeDialogueList(newCount);
            }

            if (GUILayout.Button("+ Adicionar", GUILayout.Width(86f)))
            {
                ResizeDialogueList(_dialogueLines.arraySize + 1);
            }

            GUI.enabled = _dialogueLines.arraySize > 0;
            if (GUILayout.Button("- Remover", GUILayout.Width(80f)))
            {
                ResizeDialogueList(_dialogueLines.arraySize - 1);
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDialogueCards()
        {
            for (int i = 0; i < _dialogueLines.arraySize; i++)
            {
                SerializedProperty item = _dialogueLines.GetArrayElementAtIndex(i);
                SerializedProperty speakerName = item.FindPropertyRelative("speakerName");
                SerializedProperty speakerColor = item.FindPropertyRelative("speakerNameColor");
                SerializedProperty text = item.FindPropertyRelative("text");
                SerializedProperty sprite = item.FindPropertyRelative("characterSprite");
                SerializedProperty slot = item.FindPropertyRelative("slot");
                SerializedProperty keepOthers = item.FindPropertyRelative("keepOtherCharacters");
                SerializedProperty flipX = item.FindPropertyRelative("flipHorizontal");
                SerializedProperty voice = item.FindPropertyRelative("voiceClip");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Dialogo {i + 1}", EditorStyles.boldLabel);

                GUI.enabled = i > 0;
                if (GUILayout.Button("Subir", GUILayout.Width(48f))) _dialogueLines.MoveArrayElement(i, i - 1);
                GUI.enabled = i < _dialogueLines.arraySize - 1;
                if (GUILayout.Button("Descer", GUILayout.Width(54f))) _dialogueLines.MoveArrayElement(i, i + 1);
                GUI.enabled = true;

                if (GUILayout.Button("X", GUILayout.Width(24f)))
                {
                    _dialogueLines.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Texto do Dialogo");
                text.stringValue = EditorGUILayout.TextArea(text.stringValue, _textAreaStyle, GUILayout.MinHeight(54f));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(speakerName, new GUIContent("Nome (opcional)"));
                EditorGUILayout.PropertyField(speakerColor, GUIContent.none, GUILayout.Width(54f));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(sprite, new GUIContent("Sprite do Personagem"));
                EditorGUILayout.PropertyField(slot, new GUIContent("Posicao na Tela"));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(keepOthers, new GUIContent("Manter Outros Sprites"));
                EditorGUILayout.PropertyField(flipX, new GUIContent("Espelhar X"));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(voice, new GUIContent("Som da Fala (opcional)"));

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3f);
            }
        }

        private void DrawAdvancedSection()
        {
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Configuracoes e Referencias UI", true, EditorStyles.foldoutHeader);
            if (!_showAdvanced) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_playOnStart, new GUIContent("Iniciar ao Comecar"));
            EditorGUILayout.PropertyField(_allowSkipWithEscape, new GUIContent("Permitir Pular com ESC"));
            EditorGUILayout.PropertyField(_hideGameplayHud, new GUIContent("Ocultar HUD de Gameplay"));
            EditorGUILayout.PropertyField(_hideWeaponViewmodel, new GUIContent("Ocultar Arma na Mao"));
            EditorGUILayout.PropertyField(_charactersPerSecond, new GUIContent("Velocidade Typewriter (0 = instantaneo)"));
            EditorGUILayout.PropertyField(_dimInactiveSpeakers, new GUIContent("Escurecer Quem Nao Fala"));
            EditorGUILayout.PropertyField(_inactiveSpeakerColor, new GUIContent("Cor Personagem Inativo"));
            EditorGUILayout.PropertyField(_onDialogueFinished);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Referencias de UI", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_dialogueRoot);
            EditorGUILayout.PropertyField(_speakerNameBox);
            EditorGUILayout.PropertyField(_speakerNameText);
            EditorGUILayout.PropertyField(_dialogueText);
            EditorGUILayout.PropertyField(_dialogueTextRect);
            EditorGUILayout.PropertyField(_continueIndicator);
            EditorGUILayout.PropertyField(_leftPortrait);
            EditorGUILayout.PropertyField(_centerPortrait);
            EditorGUILayout.PropertyField(_rightPortrait);
            EditorGUI.indentLevel--;
        }

        private void ResizeDialogueList(int targetSize)
        {
            int oldSize = _dialogueLines.arraySize;
            _dialogueLines.arraySize = targetSize;
            for (int i = oldSize; i < targetSize; i++)
            {
                SerializedProperty elem = _dialogueLines.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("speakerName").stringValue = "";
                elem.FindPropertyRelative("speakerNameColor").colorValue = Color.white;
                elem.FindPropertyRelative("text").stringValue = "";
                elem.FindPropertyRelative("characterSprite").objectReferenceValue = null;
                elem.FindPropertyRelative("slot").enumValueIndex = (int)CharacterSlot.Right;
                elem.FindPropertyRelative("keepOtherCharacters").boolValue = false;
                elem.FindPropertyRelative("flipHorizontal").boolValue = false;
                elem.FindPropertyRelative("voiceClip").objectReferenceValue = null;
            }
        }
    }
}
