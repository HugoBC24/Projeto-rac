using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectRac.UI;

namespace ProjectRac.Editor.UI
{
    public static class VisualNovelDialogueBuilder
    {
        private static readonly Color BoxFillColor = new Color(0.01f, 0.01f, 0.01f, 0.94f);
        private static readonly Color BorderColor = new Color(0.96f, 0.96f, 0.96f, 1f);

        [MenuItem("Tools/Build Visual Novel Intro Dialogue UI")]
        public static VisualNovelDialogueUI EnsureVisualNovelDialogueUI()
        {
            GameObject canvas = GameObject.Find("HUD_Canvas");
            if (canvas == null)
            {
                Debug.LogWarning("[VisualNovelDialogueBuilder] HUD_Canvas nao encontrado na cena.");
                return null;
            }

            var ui = BuildVisualNovelDialogueUI(canvas.transform);
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[VisualNovelDialogueBuilder] VisualNovelDialoguePanel criado em HUD_Canvas.");
            return ui;
        }

        public static VisualNovelDialogueUI BuildVisualNovelDialogueUI(Transform canvasRoot)
        {
            Sprite maidSprite = VisualNovelPlaceholderArt.EnsureMaidPlaceholder();
            Sprite guestSprite = VisualNovelPlaceholderArt.EnsureGuestPlaceholder();
            Sprite arrowSprite = VisualNovelPlaceholderArt.EnsureContinueArrow();

            Transform existing = canvasRoot.Find("VisualNovelDialoguePanel");
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject root = new GameObject("VisualNovelDialoguePanel");
            root.transform.SetParent(canvasRoot, false);
            StretchFull(root.AddComponent<RectTransform>());
            root.transform.SetAsLastSibling();
            Undo.RegisterCreatedObjectUndo(root, "Create Visual Novel Dialogue UI");

            GameObject contentRoot = new GameObject("ContentRoot");
            contentRoot.transform.SetParent(root.transform, false);
            StretchFull(contentRoot.AddComponent<RectTransform>());

            // Layer 1 (Back): Classic RPG Maker / VN black dialogue box with crisp white border.
            GameObject boxBackdrop = CreateBoxRect(contentRoot.transform, "DialogueBoxBackdrop");
            var bgImg = boxBackdrop.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = BoxFillColor;
            bgImg.raycastTarget = false;
            AddCrispWhiteBorder(boxBackdrop.transform, 3f);

            // Layer 2 (Middle): Tall character portraits rising from screen bottom over the box top border.
            GameObject stage = new GameObject("CharacterStage");
            stage.transform.SetParent(contentRoot.transform, false);
            StretchFull(stage.AddComponent<RectTransform>());

            UnityEngine.UI.Image leftPortrait = CreatePortraitSlot(stage.transform, "LeftPortrait", new Vector2(0f, 0f), new Vector2(36f, 0f));
            UnityEngine.UI.Image centerPortrait = CreatePortraitSlot(stage.transform, "CenterPortrait", new Vector2(0.5f, 0f), new Vector2(0f, 0f));
            UnityEngine.UI.Image rightPortrait = CreatePortraitSlot(stage.transform, "RightPortrait", new Vector2(1f, 0f), new Vector2(-36f, 0f));

            // Layer 3 (Front): Optional speaker nameplate, readable text with dynamic margins, and bottom white arrow.
            GameObject textLayer = CreateBoxRect(contentRoot.transform, "DialogueTextLayer");

            GameObject nameBox = new GameObject("SpeakerNameBox");
            nameBox.transform.SetParent(textLayer.transform, false);
            var nameRt = nameBox.AddComponent<RectTransform>();
            nameRt.anchorMin = nameRt.anchorMax = new Vector2(0f, 1f);
            nameRt.pivot = new Vector2(0f, 0f);
            nameRt.anchoredPosition = new Vector2(24f, 6f);
            nameRt.sizeDelta = new Vector2(320f, 48f);
            var nameBg = nameBox.AddComponent<UnityEngine.UI.Image>();
            nameBg.color = BoxFillColor;
            nameBg.raycastTarget = false;
            AddCrispWhiteBorder(nameBox.transform, 2.5f);

            GameObject nameTextObj = new GameObject("SpeakerNameText");
            nameTextObj.transform.SetParent(nameBox.transform, false);
            StretchFull(nameTextObj.AddComponent<RectTransform>());
            var nameText = nameTextObj.AddComponent<UnityEngine.UI.Text>();
            nameText.font = font;
            nameText.fontSize = 24;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = Color.white;
            nameText.raycastTarget = false;

            GameObject bodyObj = new GameObject("DialogueText");
            bodyObj.transform.SetParent(textLayer.transform, false);
            var bodyRt = bodyObj.AddComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(44f, 28f);
            bodyRt.offsetMax = new Vector2(-460f, -26f);
            var bodyText = bodyObj.AddComponent<UnityEngine.UI.Text>();
            bodyText.font = font;
            bodyText.fontSize = 30;
            bodyText.fontStyle = FontStyle.Normal;
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.color = Color.white;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.lineSpacing = 1.25f;
            bodyText.supportRichText = true;
            bodyText.raycastTarget = false;
            var shadow = bodyObj.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject arrowObj = new GameObject("ContinueIndicator");
            arrowObj.transform.SetParent(textLayer.transform, false);
            var arrowRt = arrowObj.AddComponent<RectTransform>();
            arrowRt.anchorMin = arrowRt.anchorMax = new Vector2(0.5f, 0f);
            arrowRt.pivot = new Vector2(0.5f, 0.5f);
            arrowRt.anchoredPosition = new Vector2(0f, 2f);
            arrowRt.sizeDelta = new Vector2(26f, 16f);
            var arrowImg = arrowObj.AddComponent<UnityEngine.UI.Image>();
            arrowImg.sprite = arrowSprite;
            arrowImg.color = Color.white;
            arrowImg.preserveAspect = true;
            arrowImg.raycastTarget = false;

            var ui = root.AddComponent<VisualNovelDialogueUI>();
            ui.BindComponents(contentRoot, nameBox, nameText, bodyText, arrowRt, leftPortrait, centerPortrait, rightPortrait);
            ui.SetDialogueLines(BuildSampleLines(maidSprite, guestSprite));
            return ui;
        }

        private static List<VisualNovelDialogueLine> BuildSampleLines(Sprite maidSprite, Sprite guestSprite)
        {
            return new List<VisualNovelDialogueLine>
            {
                new VisualNovelDialogueLine
                {
                    speakerName = "",
                    speakerNameColor = Color.white,
                    text = "Finalmente voce acordou... A tempestade derrubou as linhas la fora e ninguem consegue sair desta mansao.",
                    characterSprite = maidSprite,
                    slot = CharacterSlot.Right,
                    keepOtherCharacters = false
                },
                new VisualNovelDialogueLine
                {
                    speakerName = "Visitante",
                    speakerNameColor = new Color(0.95f, 0.88f, 0.72f, 1f),
                    text = "O que aconteceu aqui? Eu juro que ouvi passos vindos do segundo andar agora mesmo.",
                    characterSprite = guestSprite,
                    slot = CharacterSlot.Left,
                    keepOtherCharacters = true
                },
                new VisualNovelDialogueLine
                {
                    speakerName = "Empregada",
                    speakerNameColor = new Color(0.86f, 0.78f, 1f, 1f),
                    text = "Fique atento. Explore o salao, procure pistas nas gavetas e nos armarios... e cuidado com o que se esconde no escuro.",
                    characterSprite = maidSprite,
                    slot = CharacterSlot.Right,
                    keepOtherCharacters = true
                }
            };
        }

        private static GameObject CreateBoxRect(Transform parent, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.015f, 0.015f);
            rt.anchorMax = new Vector2(0.985f, 0.285f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return obj;
        }

        private static UnityEngine.UI.Image CreatePortraitSlot(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(540f, 945f);

            var img = obj.AddComponent<UnityEngine.UI.Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            obj.SetActive(false);
            return img;
        }

        private static void AddCrispWhiteBorder(Transform parent, float thickness)
        {
            CreateEdge(parent, "Border_Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -thickness), Vector2.zero);
            CreateEdge(parent, "Border_Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, thickness));
            CreateEdge(parent, "Border_Left", new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(thickness, 0f));
            CreateEdge(parent, "Border_Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-thickness, 0f), Vector2.zero);
        }

        private static void CreateEdge(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject edge = new GameObject(name);
            edge.transform.SetParent(parent, false);
            var rt = edge.AddComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var img = edge.AddComponent<UnityEngine.UI.Image>();
            img.color = BorderColor;
            img.raycastTarget = false;
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
