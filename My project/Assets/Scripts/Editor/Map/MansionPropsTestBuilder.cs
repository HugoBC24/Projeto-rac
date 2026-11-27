using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectRac.Interaction;
using ProjectRac.UI;

namespace ProjectRac.Editor.Map
{
    public static class MansionPropsTestBuilder
    {
        public const string PropsRootName = "MansionProps_Test";

        private static readonly Color WoodDark = new Color(0.32f, 0.21f, 0.13f, 1f);
        private static readonly Color WoodMedium = new Color(0.45f, 0.30f, 0.18f, 1f);
        private static readonly Color WoodInner = new Color(0.24f, 0.16f, 0.10f, 1f);
        private static readonly Color BrassHandle = new Color(0.84f, 0.72f, 0.35f, 1f);
        private static readonly Color PaperColor = new Color(0.93f, 0.89f, 0.78f, 1f);
        private static readonly Color InkColor = new Color(0.18f, 0.15f, 0.12f, 1f);

        [MenuItem("Tools/Build Mansion Test Props (Drawer, Cabinet, Note)")]
        public static GameObject BuildTestPropsAndUI()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            // Compute East HALL wall inner world X dynamically so furniture sits flush regardless of MansionMap scale.
            float hallEastInnerX = ResolveHallEastWallInnerX();
            float floorY = 0f;

            GameObject existing = GameObject.Find(PropsRootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            // Keep world scale (1,1,1) outside MansionMap so furniture keeps realistic real-world dimensions.
            GameObject root = new GameObject(PropsRootName);
            root.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(root, "Create Mansion Test Props");

            NoteInspectionUI noteUI = EnsureNoteInspectionUI();

            // 1. Dresser with 1 interactive sliding drawer + 1 inspectable note on top
            Vector3 dresserPos = new Vector3(hallEastInnerX - 0.34f, floorY, -5.0f);
            BuildTestDresserWithNote(root.transform, dresserPos, noteUI);

            // 2. Tall double-door cabinet right next to the dresser
            Vector3 cabinetPos = new Vector3(hallEastInnerX - 0.36f, floorY, -2.4f);
            BuildTestCabinet(root.transform, cabinetPos);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("[MansionPropsTestBuilder] Gaveteiro, armario, nota e NoteInspectionUI criados no HALL.");
            return root;
        }

        private static float ResolveHallEastWallInnerX()
        {
            GameObject mansion = GameObject.Find(MansionMapBuilder.MansionRootName);
            if (mansion != null)
            {
                float scaleX = mansion.transform.lossyScale.x;
                return mansion.transform.position.x + (MansionLayoutData.XHallEast - MansionLayoutData.WallThickness * 0.5f) * scaleX;
            }
            return 4.25f;
        }

        private static void BuildTestDresserWithNote(Transform parent, Vector3 worldPos, NoteInspectionUI noteUI)
        {
            GameObject dresser = new GameObject("Test_Dresser");
            dresser.transform.SetParent(parent, false);
            dresser.transform.position = worldPos;
            // Face West (-X) toward the center of the HALL.
            dresser.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            // Outer carcass (width 1.1m, height 0.92m, depth 0.58m) with openings for top and bottom drawers
            MansionDoorFactory.CreateLocalBox(dresser.transform, "TopBoard", new Vector3(0f, 0.89f, 0f), new Vector3(1.12f, 0.06f, 0.60f), WoodDark);
            MansionDoorFactory.CreateLocalBox(dresser.transform, "MidShelf", new Vector3(0f, 0.46f, 0f), new Vector3(1.06f, 0.04f, 0.56f), WoodDark);
            MansionDoorFactory.CreateLocalBox(dresser.transform, "BottomPlinth", new Vector3(0f, 0.04f, 0f), new Vector3(1.06f, 0.08f, 0.56f), WoodDark);
            MansionDoorFactory.CreateLocalBox(dresser.transform, "BackPanel", new Vector3(0f, 0.47f, -0.26f), new Vector3(1.06f, 0.78f, 0.04f), WoodInner);
            MansionDoorFactory.CreateLocalBox(dresser.transform, "Side_Left", new Vector3(-0.51f, 0.47f, 0f), new Vector3(0.05f, 0.78f, 0.56f), WoodDark);
            MansionDoorFactory.CreateLocalBox(dresser.transform, "Side_Right", new Vector3(0.51f, 0.47f, 0f), new Vector3(0.05f, 0.78f, 0.56f), WoodDark);

            // Two sliding interactive drawers
            BuildSingleDrawer(dresser.transform, "Drawer_Top", new Vector3(0f, 0.68f, 0.02f));
            BuildSingleDrawer(dresser.transform, "Drawer_Bottom", new Vector3(0f, 0.25f, 0.02f));

            // Inspectable Note resting on top of the dresser
            GameObject noteObj = new GameObject("Test_Note");
            noteObj.transform.SetParent(dresser.transform, false);
            noteObj.transform.localPosition = new Vector3(-0.08f, 0.928f, 0.04f);
            noteObj.transform.localRotation = Quaternion.Euler(0f, 14f, 0f);

            var noteCol = noteObj.AddComponent<BoxCollider>();
            noteCol.size = new Vector3(0.32f, 0.08f, 0.40f);
            noteCol.center = new Vector3(0f, 0.03f, 0f);

            var note = noteObj.AddComponent<InspectableNote>();
            note.Configure(
                "Registro do Zelador",
                "14 de Outubro\n\n" +
                "Tranquei as portas dos quartos do segundo andar antes de sair.\n" +
                "Ouvi passos vindos da biblioteca durante a madrugada, mas o hall estava vazio.\n\n" +
                "Se alguem encontrar este bilhete, verifique as gavetas e os armarios — deixei os suprimentos guardados por aqui.",
                noteUI);

            MansionDoorFactory.CreateLocalBox(noteObj.transform, "PaperSheet", Vector3.zero, new Vector3(0.26f, 0.012f, 0.34f), PaperColor, removeCollider: true);
            MansionDoorFactory.CreateLocalBox(noteObj.transform, "InkLine_1", new Vector3(0f, 0.008f, 0.09f), new Vector3(0.18f, 0.003f, 0.014f), InkColor, removeCollider: true);
            MansionDoorFactory.CreateLocalBox(noteObj.transform, "InkLine_2", new Vector3(-0.01f, 0.008f, 0.02f), new Vector3(0.19f, 0.003f, 0.012f), InkColor, removeCollider: true);
            MansionDoorFactory.CreateLocalBox(noteObj.transform, "InkLine_3", new Vector3(-0.02f, 0.008f, -0.05f), new Vector3(0.15f, 0.003f, 0.012f), InkColor, removeCollider: true);
        }

        public static DrawerController BuildSingleDrawer(Transform parent, string name, Vector3 localPos)
        {
            GameObject drawer = new GameObject(name);
            drawer.transform.SetParent(parent, false);
            drawer.transform.localPosition = localPos;

            var drawerCol = drawer.AddComponent<BoxCollider>();
            drawerCol.center = new Vector3(0f, 0f, 0.08f);
            drawerCol.size = new Vector3(0.98f, 0.26f, 0.54f);

            var drawerCtrl = drawer.AddComponent<DrawerController>();
            drawerCtrl.Configure(drawer.transform, Vector3.forward, 0.44f);

            MansionDoorFactory.CreateLocalBox(drawer.transform, "Drawer_Front", new Vector3(0f, 0f, 0.26f), new Vector3(0.98f, 0.25f, 0.04f), WoodMedium, removeCollider: true);
            MansionDoorFactory.CreateLocalBox(drawer.transform, "Drawer_Bottom", new Vector3(0f, -0.10f, 0f), new Vector3(0.92f, 0.03f, 0.48f), WoodInner, removeCollider: true);
            MansionDoorFactory.CreateLocalBox(drawer.transform, "Drawer_Left", new Vector3(-0.45f, 0f, 0f), new Vector3(0.03f, 0.20f, 0.48f), WoodInner, removeCollider: true);
            MansionDoorFactory.CreateLocalBox(drawer.transform, "Drawer_Right", new Vector3(0.45f, 0f, 0f), new Vector3(0.03f, 0.20f, 0.48f), WoodInner, removeCollider: true);
            MansionDoorFactory.CreateLocalBox(drawer.transform, "Drawer_Back", new Vector3(0f, 0f, -0.23f), new Vector3(0.92f, 0.20f, 0.03f), WoodInner, removeCollider: true);
            MansionDoorFactory.CreateLocalBox(drawer.transform, "Drawer_Handle", new Vector3(0f, 0f, 0.30f), new Vector3(0.22f, 0.04f, 0.04f), BrassHandle, removeCollider: true);

            return drawerCtrl;
        }

        private static void BuildTestCabinet(Transform parent, Vector3 worldPos)
        {
            GameObject cabinet = new GameObject("Test_Cabinet");
            cabinet.transform.SetParent(parent, false);
            cabinet.transform.position = worldPos;
            cabinet.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            // Tall wardrobe shell (width 1.32m, height 2.10m, depth 0.62m) with interior shelves
            MansionDoorFactory.CreateLocalBox(cabinet.transform, "TopPanel", new Vector3(0f, 2.07f, 0f), new Vector3(1.34f, 0.06f, 0.64f), WoodDark);
            MansionDoorFactory.CreateLocalBox(cabinet.transform, "BottomPanel", new Vector3(0f, 0.04f, 0f), new Vector3(1.32f, 0.08f, 0.62f), WoodDark);
            MansionDoorFactory.CreateLocalBox(cabinet.transform, "BackPanel", new Vector3(0f, 1.05f, -0.29f), new Vector3(1.32f, 2.02f, 0.04f), WoodInner);
            MansionDoorFactory.CreateLocalBox(cabinet.transform, "Side_Left", new Vector3(-0.63f, 1.05f, 0f), new Vector3(0.06f, 2.02f, 0.62f), WoodDark);
            MansionDoorFactory.CreateLocalBox(cabinet.transform, "Side_Right", new Vector3(0.63f, 1.05f, 0f), new Vector3(0.06f, 2.02f, 0.62f), WoodDark);
            MansionDoorFactory.CreateLocalBox(cabinet.transform, "Shelf_Mid", new Vector3(0f, 1.05f, -0.02f), new Vector3(1.20f, 0.04f, 0.52f), WoodInner);
            MansionDoorFactory.CreateLocalBox(cabinet.transform, "Shelf_High", new Vector3(0f, 1.56f, -0.02f), new Vector3(1.20f, 0.04f, 0.52f), WoodInner);

            // Hinged double doors swinging outward (+Z)
            Transform leftPivot = CreateCabinetDoor(cabinet.transform, "Door_Left_Pivot", new Vector3(-0.60f, 0.08f, 0.29f), isLeft: true);
            Transform rightPivot = CreateCabinetDoor(cabinet.transform, "Door_Right_Pivot", new Vector3(0.60f, 0.08f, 0.29f), isLeft: false);

            var controller = cabinet.AddComponent<CabinetController>();
            // Left pivot at -X swings outward (+Z) with +105 deg? Check Unity left-handed Y rotation:
            // (+1,0,0) rotated -105 around Y goes toward (+Z)! (-1,0,0) rotated +105 around Y goes toward (+Z)!
            controller.Configure(leftPivot, rightPivot, leftAngle: -105f, rightAngle: 105f);
        }

        private static Transform CreateCabinetDoor(Transform parent, string pivotName, Vector3 pivotLocalPos, bool isLeft)
        {
            GameObject pivot = new GameObject(pivotName);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = pivotLocalPos;

            float leafW = 0.59f;
            float leafH = 1.94f;
            float dir = isLeft ? 1f : -1f;

            MansionDoorFactory.CreateLocalBox(
                pivot.transform,
                isLeft ? "CabinetDoor_Left" : "CabinetDoor_Right",
                new Vector3(dir * leafW * 0.5f, leafH * 0.5f, 0.02f),
                new Vector3(leafW, leafH, 0.04f),
                WoodMedium);

            MansionDoorFactory.CreateLocalBox(
                pivot.transform,
                "Handle",
                new Vector3(dir * (leafW - 0.07f), 0.95f, 0.06f),
                new Vector3(0.03f, 0.18f, 0.04f),
                BrassHandle,
                removeCollider: true);

            return pivot.transform;
        }

        public static NoteInspectionUI EnsureNoteInspectionUI()
        {
            GameObject canvas = GameObject.Find("HUD_Canvas");
            if (canvas == null) return null;
            return BuildNoteInspectionUI(canvas.transform);
        }

        public static NoteInspectionUI BuildNoteInspectionUI(Transform canvasRoot)
        {
            Transform existing = canvasRoot.Find("NoteInspectionPanel");
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject root = new GameObject("NoteInspectionPanel");
            root.transform.SetParent(canvasRoot, false);
            StretchFull(root.AddComponent<RectTransform>());
            Undo.RegisterCreatedObjectUndo(root, "Create Note Inspection UI");

            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(root.transform, false);
            StretchFull(backdrop.AddComponent<RectTransform>());
            var bgImg = backdrop.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.04f, 0.05f, 0.72f);

            GameObject card = new GameObject("PaperCard");
            card.transform.SetParent(backdrop.transform, false);
            var cardRt = card.AddComponent<RectTransform>();
            cardRt.anchorMin = cardRt.anchorMax = cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(680f, 520f);
            var cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.93f, 0.89f, 0.79f, 1f);
            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.24f, 0.18f, 0.12f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);

            Text title = CreateLabel(card.transform, "TitleText", font, 28, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(0.18f, 0.13f, 0.09f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(36f, -92f), new Vector2(-36f, -24f));
            Text body = CreateLabel(card.transform, "BodyText", font, 20, FontStyle.Normal, TextAnchor.UpperLeft,
                new Color(0.16f, 0.12f, 0.09f, 1f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(44f, 78f), new Vector2(-44f, -108f));
            Text hint = CreateLabel(card.transform, "HintText", font, 16, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(0.38f, 0.28f, 0.20f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(36f, 20f), new Vector2(-36f, 56f));

            var ui = root.AddComponent<NoteInspectionUI>();
            ui.BindComponents(backdrop, title, body, hint);
            return ui;
        }

        private static Text CreateLabel(
            Transform parent, string name, Font font, int size, FontStyle style, TextAnchor align,
            Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var txt = obj.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.alignment = align;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.lineSpacing = 1.25f;
            return txt;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
