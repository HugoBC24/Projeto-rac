using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectRac.Interaction;
using ProjectRac.UI;

namespace ProjectRac.Editor.Map
{
    public static class IntroDoorSequenceBuilder
    {
        public const string DoorRevealObjectName = "Intro_DoorRevealText";

        // Interior face of Door_MainEntrance (F1_Ext_South) under MansionMap scale (1.5, 1.0, 1.5).
        // World Door_MainEntrance center is (-1.20, 1.25, -15.90); offset slightly North (+Z) to sit flush on interior face.
        private static readonly Vector3 DoorTextWorldPos = new Vector3(-1.20f, 1.60f, -15.55f);

        [MenuItem("Tools/Setup Intro Door Text & Director")]
        public static IntroDialogueDoorDirector SetupInScene()
        {
            GameObject existing = GameObject.Find(DoorRevealObjectName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            GameObject root = new GameObject(DoorRevealObjectName);
            root.transform.position = DoorTextWorldPos;
            // Facing +Z (North, into the Hall) so the player sees it right-side-up when looking South at the door.
            root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Undo.RegisterCreatedObjectUndo(root, "Create Intro Door Reveal Text");

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(460f, 160f);
            rt.localScale = new Vector3(0.0024f, 0.0024f, 0.0024f);

            var cg = root.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            GameObject textObj = new GameObject("DoorMessageText");
            textObj.transform.SetParent(root.transform, false);
            var textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var txt = textObj.AddComponent<UnityEngine.UI.Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = "NAO HA SAIDA";
            txt.fontSize = 54;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.95f, 0.92f, 0.86f, 1f);
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;

            var shadow = textObj.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
            shadow.effectDistance = new Vector2(3f, -3f);

            var vnUI = Object.FindAnyObjectByType<VisualNovelDialogueUI>(FindObjectsInactive.Include);
            var director = root.AddComponent<IntroDialogueDoorDirector>();
            director.Configure(vnUI, root.transform, cg, txt, "NAO HA SAIDA");

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[IntroDoorSequenceBuilder] Intro_DoorRevealText e IntroDialogueDoorDirector configurados na Door_MainEntrance.");
            return director;
        }
    }
}
