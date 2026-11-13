using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectRac.Player;
using ProjectRac.Interaction;
using ProjectRac.UI;

namespace ProjectRac.Editor
{
    public static class TestArenaBuilder
    {
        [MenuItem("Tools/Build FPAA Test Arena")]
        public static void BuildArena()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Setup Arena Root
            GameObject arenaRoot = GameObject.Find("TestArena");
            if (arenaRoot != null)
            {
                Undo.DestroyObjectImmediate(arenaRoot);
            }
            arenaRoot = new GameObject("TestArena");
            Undo.RegisterCreatedObjectUndo(arenaRoot, "Create Arena Root");

            // 2. Main Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.parent = arenaRoot.transform;
            floor.transform.position = new Vector3(0, -0.5f, 0);
            floor.transform.localScale = new Vector3(60f, 1f, 60f);

            // 3. Surrounding Walls
            CreateWall(arenaRoot.transform, "Wall_North", new Vector3(0, 2.5f, 30f), new Vector3(60f, 5f, 1f));
            CreateWall(arenaRoot.transform, "Wall_South", new Vector3(0, 2.5f, -30f), new Vector3(60f, 5f, 1f));
            CreateWall(arenaRoot.transform, "Wall_East", new Vector3(30f, 2.5f, 0), new Vector3(1f, 5f, 60f));
            CreateWall(arenaRoot.transform, "Wall_West", new Vector3(-30f, 2.5f, 0), new Vector3(1f, 5f, 60f));

            // 4. Platforms & Ramp for jump/slope testing
            GameObject platform1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform1.name = "Platform_Low";
            platform1.transform.parent = arenaRoot.transform;
            platform1.transform.position = new Vector3(10f, 0.75f, 10f);
            platform1.transform.localScale = new Vector3(6f, 1.5f, 6f);

            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Ramp";
            ramp.transform.parent = arenaRoot.transform;
            ramp.transform.position = new Vector3(10f, 0.75f, 4f);
            ramp.transform.rotation = Quaternion.Euler(20f, 0, 0);
            ramp.transform.localScale = new Vector3(4f, 0.5f, 5f);

            GameObject platformHigh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platformHigh.name = "Platform_High";
            platformHigh.transform.parent = arenaRoot.transform;
            platformHigh.transform.position = new Vector3(10f, 1.75f, 18f);
            platformHigh.transform.localScale = new Vector3(6f, 3.5f, 6f);

            // 5. Interactive Test Objects
            CreateInteractable(arenaRoot.transform, "Interactive_Cube_Alpha", new Vector3(-6f, 1f, 8f));
            CreateInteractable(arenaRoot.transform, "Interactive_Cube_Beta", new Vector3(-10f, 1f, 8f));
            CreateInteractable(arenaRoot.transform, "Interactive_Cube_Gamma", new Vector3(10f, 4f, 18f));

            // 6. Setup Player
            GameObject player = SetupPlayer();

            // 7. Setup Responsive uGUI Canvas & Stamina HUD
            SetupHUDCanvas(player);

            // Mark Scene Dirty and Save
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("[TestArenaBuilder] Arena de Teste FPAA configurada com Canvas responsivo! Pressione PLAY para testar.");
        }

        private static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.parent = parent;
            wall.transform.position = pos;
            wall.transform.localScale = scale;
        }

        private static void CreateInteractable(Transform parent, string name, Vector3 pos)
        {
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = name + "_Pedestal";
            pedestal.transform.parent = parent;
            pedestal.transform.position = new Vector3(pos.x, 0.4f, pos.z);
            pedestal.transform.localScale = new Vector3(1.2f, 0.4f, 1.2f);

            GameObject interactable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            interactable.name = name;
            interactable.transform.parent = parent;
            interactable.transform.position = new Vector3(pos.x, 1.4f, pos.z);
            interactable.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            interactable.AddComponent<InteractableTestObject>();
        }

        private static GameObject SetupPlayer()
        {
            var existingCams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            foreach (var cam in existingCams)
            {
                if (cam.transform.parent == null || cam.transform.parent.name != "Player")
                {
                    Undo.DestroyObjectImmediate(cam.gameObject);
                }
            }

            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                Undo.DestroyObjectImmediate(player);
            }

            player = new GameObject("Player");
            Undo.RegisterCreatedObjectUndo(player, "Create Player");
            player.transform.position = new Vector3(0f, 0.1f, 0f);

            var charController = player.AddComponent<CharacterController>();
            charController.height = 2.0f;
            charController.radius = 0.45f;
            charController.center = new Vector3(0f, 1.0f, 0f);
            charController.stepOffset = 0.4f;
            charController.slopeLimit = 45f;

            var inputHandler = player.AddComponent<PlayerInputHandler>();

            var actionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
            if (actionAsset != null)
            {
                var playerInput = player.AddComponent<PlayerInput>();
                playerInput.actions = actionAsset;
                playerInput.defaultActionMap = "Player";
                inputHandler.SetActionAsset(actionAsset);
            }

            player.AddComponent<PlayerStamina>();
            player.AddComponent<FirstPersonController>();
            player.AddComponent<PlayerInteraction>();

            GameObject cameraObj = new GameObject("FirstPersonCamera");
            cameraObj.transform.parent = player.transform;
            cameraObj.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            cameraObj.transform.localRotation = Quaternion.identity;

            var camComponent = cameraObj.AddComponent<Camera>();
            camComponent.tag = "MainCamera";
            camComponent.nearClipPlane = 0.1f;
            cameraObj.AddComponent<AudioListener>();
            cameraObj.AddComponent<FirstPersonCameraLook>();

            return player;
        }

        private static void SetupHUDCanvas(GameObject player)
        {
            GameObject canvasObj = GameObject.Find("HUD_Canvas");
            if (canvasObj != null)
            {
                Undo.DestroyObjectImmediate(canvasObj);
            }

            canvasObj = new GameObject("HUD_Canvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create HUD Canvas");

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject eventSys = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(eventSys, "Create EventSystem");
                eventSys.AddComponent<EventSystem>();
                eventSys.AddComponent<InputSystemUIInputModule>();
            }

            // Stamina HUD Root Container
            GameObject staminaRoot = new GameObject("StaminaHUD");
            staminaRoot.transform.SetParent(canvasObj.transform, false);

            RectTransform rootRt = staminaRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 0f);
            rootRt.anchorMax = new Vector2(0f, 0f);
            rootRt.pivot = new Vector2(0f, 0f);
            rootRt.anchoredPosition = new Vector2(40f, 40f);
            rootRt.sizeDelta = new Vector2(260f, 24f);

            // Background Panel
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(staminaRoot.transform, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            Outline bgOutline = bgObj.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0.7f, 0.7f, 0.7f, 0.75f);
            bgOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Slider & Fill Area
            Slider slider = staminaRoot.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(staminaRoot.transform, false);
            RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.sizeDelta = new Vector2(-4f, -4f);
            fillAreaRt.anchoredPosition = Vector2.zero;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fillObj.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;

            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.85f, 0.4f, 0.95f);

            slider.fillRect = fillRt;
            slider.targetGraphic = fillImg;

            // Label Text
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(staminaRoot.transform, false);
            RectTransform labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot = new Vector2(0f, 0f);
            labelRt.anchoredPosition = new Vector2(2f, 6f);
            labelRt.sizeDelta = new Vector2(0f, 22f);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = "STAMINA: 100 / 100";
            labelText.fontSize = 13;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            Shadow textShadow = labelObj.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            textShadow.effectDistance = new Vector2(1f, -1f);

            // Bind UI Controller
            StaminaBarUI staminaUI = staminaRoot.AddComponent<StaminaBarUI>();
            var playerStamina = player.GetComponent<PlayerStamina>();
            staminaUI.BindPlayerStamina(playerStamina);
            staminaUI.SetUGUIReferences(slider, fillImg, labelText);
        }
    }
}
