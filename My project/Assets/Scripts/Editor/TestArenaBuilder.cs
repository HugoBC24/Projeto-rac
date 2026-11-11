using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectRac.Player;
using ProjectRac.Interaction;

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

            // 6. Setup Player with New Input System
            SetupPlayer();

            // Mark Scene Dirty and Save
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("[TestArenaBuilder] Arena de Teste FPAA configurada com Unity New Input System! Pressione PLAY para testar.");
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

        private static void SetupPlayer()
        {
            // Remove existing standalone cameras
            var existingCams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            foreach (var cam in existingCams)
            {
                if (cam.transform.parent == null || cam.transform.parent.name != "Player")
                {
                    Undo.DestroyObjectImmediate(cam.gameObject);
                }
            }

            // Clean existing Player
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                Undo.DestroyObjectImmediate(player);
            }

            player = new GameObject("Player");
            Undo.RegisterCreatedObjectUndo(player, "Create Player");
            player.transform.position = new Vector3(0f, 0.1f, 0f);

            // Add CharacterController
            var charController = player.AddComponent<CharacterController>();
            charController.height = 2.0f;
            charController.radius = 0.45f;
            charController.center = new Vector3(0f, 1.0f, 0f);
            charController.stepOffset = 0.4f;
            charController.slopeLimit = 45f;

            // Add Input and Controllers
            var inputHandler = player.AddComponent<PlayerInputHandler>();

            var actionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
            if (actionAsset != null)
            {
                var playerInput = player.AddComponent<PlayerInput>();
                playerInput.actions = actionAsset;
                playerInput.defaultActionMap = "Player";
                inputHandler.SetActionAsset(actionAsset);
            }

            player.AddComponent<FirstPersonController>();
            player.AddComponent<PlayerInteraction>();

            // Setup Camera
            GameObject cameraObj = new GameObject("FirstPersonCamera");
            cameraObj.transform.parent = player.transform;
            cameraObj.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            cameraObj.transform.localRotation = Quaternion.identity;

            var camComponent = cameraObj.AddComponent<Camera>();
            camComponent.tag = "MainCamera";
            camComponent.nearClipPlane = 0.1f;
            cameraObj.AddComponent<AudioListener>();

            cameraObj.AddComponent<FirstPersonCameraLook>();
        }
    }
}
