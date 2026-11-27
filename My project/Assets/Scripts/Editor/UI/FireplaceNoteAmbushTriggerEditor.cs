using UnityEditor;
using UnityEngine;
using ProjectRac.Interaction;
using ProjectRac.UI;

namespace ProjectRac.Editor.UI
{
    [CustomEditor(typeof(FireplaceNoteAmbushTrigger))]
    public class FireplaceNoteAmbushTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty _targetNote;
        private SerializedProperty _meleeEnemyPrefab;
        private SerializedProperty _visualNovelUI;
        private SerializedProperty _spawnDistanceBehind;
        private SerializedProperty _fallbackSpawnPosition;
        private SerializedProperty _turnPlayerToFaceEnemy;
        private SerializedProperty _cameraFacePitch;
        private SerializedProperty _ambushDialogues;

        private bool _showConfig = true;
        private GUIStyle _textAreaStyle;

        private void OnEnable()
        {
            _targetNote = serializedObject.FindProperty("targetNote");
            _meleeEnemyPrefab = serializedObject.FindProperty("meleeEnemyPrefab");
            _visualNovelUI = serializedObject.FindProperty("visualNovelUI");
            _spawnDistanceBehind = serializedObject.FindProperty("spawnDistanceBehind");
            _fallbackSpawnPosition = serializedObject.FindProperty("fallbackSpawnPosition");
            _turnPlayerToFaceEnemy = serializedObject.FindProperty("turnPlayerToFaceEnemy");
            _cameraFacePitch = serializedObject.FindProperty("cameraFacePitch");
            _ambushDialogues = serializedObject.FindProperty("ambushDialogues");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (_textAreaStyle == null) _textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };

            EditorGUILayout.LabelField("Emboscada da Lareira — Dialogo Visual Novel", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Ao fechar esta nota, o inimigo melee surge atras do jogador em Idle, vira a camera, exibe os dialogos abaixo e inicia a perseguicao.",
                MessageType.Info);

            DrawDialogueHeader();
            EditorGUILayout.Space(4f);
            DrawDialogueCards();
            EditorGUILayout.Space(6f);

            _showConfig = EditorGUILayout.Foldout(_showConfig, "Configuracoes do Evento e Spawn", true, EditorStyles.foldoutHeader);
            if (_showConfig)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_targetNote);
                EditorGUILayout.PropertyField(_meleeEnemyPrefab);
                EditorGUILayout.PropertyField(_visualNovelUI);
                EditorGUILayout.PropertyField(_spawnDistanceBehind, new GUIContent("Distancia Atras do Player"));
                EditorGUILayout.PropertyField(_fallbackSpawnPosition, new GUIContent("Posicao Reserva (NavMesh)"));
                EditorGUILayout.PropertyField(_turnPlayerToFaceEnemy, new GUIContent("Virar Camera para a Boneca"));
                EditorGUILayout.PropertyField(_cameraFacePitch, new GUIContent("Inclinacao da Camera"));
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDialogueHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            int cur = _ambushDialogues.arraySize;
            int next = Mathf.Clamp(EditorGUILayout.IntField("Quantidade de Dialogos", cur), 0, 50);
            if (next != cur) Resize(next);

            if (GUILayout.Button("+ Adicionar", GUILayout.Width(86f))) Resize(_ambushDialogues.arraySize + 1);
            GUI.enabled = _ambushDialogues.arraySize > 0;
            if (GUILayout.Button("- Remover", GUILayout.Width(80f))) Resize(_ambushDialogues.arraySize - 1);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDialogueCards()
        {
            for (int i = 0; i < _ambushDialogues.arraySize; i++)
            {
                SerializedProperty item = _ambushDialogues.GetArrayElementAtIndex(i);
                SerializedProperty speaker = item.FindPropertyRelative("speakerName");
                SerializedProperty color = item.FindPropertyRelative("speakerNameColor");
                SerializedProperty text = item.FindPropertyRelative("text");
                SerializedProperty sprite = item.FindPropertyRelative("characterSprite");
                SerializedProperty slot = item.FindPropertyRelative("slot");
                SerializedProperty keep = item.FindPropertyRelative("keepOtherCharacters");
                SerializedProperty flip = item.FindPropertyRelative("flipHorizontal");
                SerializedProperty voice = item.FindPropertyRelative("voiceClip");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Dialogo {i + 1}", EditorStyles.boldLabel);
                GUI.enabled = i > 0;
                if (GUILayout.Button("Subir", GUILayout.Width(48f))) _ambushDialogues.MoveArrayElement(i, i - 1);
                GUI.enabled = i < _ambushDialogues.arraySize - 1;
                if (GUILayout.Button("Descer", GUILayout.Width(54f))) _ambushDialogues.MoveArrayElement(i, i + 1);
                GUI.enabled = true;
                if (GUILayout.Button("X", GUILayout.Width(24f)))
                {
                    _ambushDialogues.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Texto do Dialogo");
                text.stringValue = EditorGUILayout.TextArea(text.stringValue, _textAreaStyle, GUILayout.MinHeight(50f));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(speaker, new GUIContent("Nome (opcional)"));
                EditorGUILayout.PropertyField(color, GUIContent.none, GUILayout.Width(54f));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(sprite, new GUIContent("Sprite do Personagem"));
                EditorGUILayout.PropertyField(slot, new GUIContent("Posicao na Tela"));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(keep, new GUIContent("Manter Outros Sprites"));
                EditorGUILayout.PropertyField(flip, new GUIContent("Espelhar X"));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(voice, new GUIContent("Som da Fala (opcional)"));
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2f);
            }
        }

        private void Resize(int targetSize)
        {
            int oldSize = _ambushDialogues.arraySize;
            _ambushDialogues.arraySize = targetSize;
            for (int i = oldSize; i < targetSize; i++)
            {
                SerializedProperty e = _ambushDialogues.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("speakerName").stringValue = "";
                e.FindPropertyRelative("speakerNameColor").colorValue = Color.white;
                e.FindPropertyRelative("text").stringValue = "";
                e.FindPropertyRelative("characterSprite").objectReferenceValue = null;
                e.FindPropertyRelative("slot").enumValueIndex = (int)CharacterSlot.Right;
                e.FindPropertyRelative("keepOtherCharacters").boolValue = false;
                e.FindPropertyRelative("flipHorizontal").boolValue = false;
                e.FindPropertyRelative("voiceClip").objectReferenceValue = null;
            }
        }
    }
}
