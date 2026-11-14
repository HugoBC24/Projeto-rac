using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using ProjectRac.Player;
using ProjectRac.Interaction;
using ProjectRac.Weapons;
using ProjectRac.Effects;

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

            // 2. Main Floor & Walls
            BuildFloorAndWalls(arenaRoot.transform);

            // 3. Platforms & Ramp for jump/slope testing
            BuildPlatformsAndRamp(arenaRoot.transform);

            // 4. Interactive Test Objects
            CreateInteractable(arenaRoot.transform, "Interactive_Cube_Alpha", new Vector3(-6f, 1f, 8f));
            CreateInteractable(arenaRoot.transform, "Interactive_Cube_Beta", new Vector3(-10f, 1f, 8f));
            CreateInteractable(arenaRoot.transform, "Interactive_Cube_Gamma", new Vector3(10f, 4f, 18f));

            // 5. Physics Target Stack (Boxes to shoot down with Slingshot)
            BuildPhysicsTargetStack(arenaRoot.transform, new Vector3(0f, 0f, 16f));

            // 6. Setup ShootPool
            var projectilePool = SetupShootPool(arenaRoot.transform);

            // 7. Setup Player with Slingshot
            GameObject player = SetupPlayer(projectilePool);

            // 8. Setup Responsive uGUI Canvas & Stamina HUD
            HUDCanvasBuilder.SetupHUDCanvas(player);

            // Mark Scene Dirty and Save
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("[TestArenaBuilder] Arena de Teste FPAA configurada com Estilingue e ShootPool! Pressione PLAY para testar.");
        }

        private static void BuildFloorAndWalls(Transform root)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.parent = root;
            floor.transform.position = new Vector3(0, -0.5f, 0);
            floor.transform.localScale = new Vector3(60f, 1f, 60f);

            CreateWall(root, "Wall_North", new Vector3(0, 2.5f, 30f), new Vector3(60f, 5f, 1f));
            CreateWall(root, "Wall_South", new Vector3(0, 2.5f, -30f), new Vector3(60f, 5f, 1f));
            CreateWall(root, "Wall_East", new Vector3(30f, 2.5f, 0), new Vector3(1f, 5f, 60f));
            CreateWall(root, "Wall_West", new Vector3(-30f, 2.5f, 0), new Vector3(1f, 5f, 60f));
        }

        private static void BuildPlatformsAndRamp(Transform root)
        {
            GameObject platform1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform1.name = "Platform_Low";
            platform1.transform.parent = root;
            platform1.transform.position = new Vector3(10f, 0.75f, 10f);
            platform1.transform.localScale = new Vector3(6f, 1.5f, 6f);

            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Ramp";
            ramp.transform.parent = root;
            ramp.transform.position = new Vector3(10f, 0.75f, 4f);
            ramp.transform.rotation = Quaternion.Euler(20f, 0, 0);
            ramp.transform.localScale = new Vector3(4f, 0.5f, 5f);

            GameObject platformHigh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platformHigh.name = "Platform_High";
            platformHigh.transform.parent = root;
            platformHigh.transform.position = new Vector3(10f, 1.75f, 18f);
            platformHigh.transform.localScale = new Vector3(6f, 3.5f, 6f);
        }

        private static void BuildPhysicsTargetStack(Transform root, Vector3 centerPos)
        {
            GameObject stackRoot = new GameObject("PhysicsTarget_Boxes");
            stackRoot.transform.parent = root;
            stackRoot.transform.position = centerPos;

            // Base row (3 boxes)
            CreatePhysicsBox(stackRoot.transform, new Vector3(-1.1f, 0.5f, 0f));
            CreatePhysicsBox(stackRoot.transform, new Vector3(0f, 0.5f, 0f));
            CreatePhysicsBox(stackRoot.transform, new Vector3(1.1f, 0.5f, 0f));

            // Middle row (2 boxes)
            CreatePhysicsBox(stackRoot.transform, new Vector3(-0.55f, 1.5f, 0f));
            CreatePhysicsBox(stackRoot.transform, new Vector3(0.55f, 1.5f, 0f));

            // Top box (1 box)
            CreatePhysicsBox(stackRoot.transform, new Vector3(0f, 2.5f, 0f));
        }

        private static void CreatePhysicsBox(Transform parent, Vector3 localPos)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "TargetBox";
            box.transform.parent = parent;
            box.transform.localPosition = localPos;
            box.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);

            var rb = box.AddComponent<Rigidbody>();
            rb.mass = 1.2f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var renderer = box.GetComponent<Renderer>();
            if (renderer != null)
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", new Color(0.75f, 0.55f, 0.32f)); // Wood color
                renderer.SetPropertyBlock(block);
            }
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

        private static SlingshotProjectilePool SetupShootPool(Transform root)
        {
            GameObject poolObj = new GameObject("SlingshotPool");
            poolObj.transform.parent = root;
            var pool = poolObj.AddComponent<SlingshotProjectilePool>();
            pool.Configure(null, 25);
            return pool;
        }

        private static GameObject SetupPlayer(SlingshotProjectilePool pool)
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
            player.AddComponent<StaminaExhaustionPostProcess>();

            // Setup Camera & Slingshot Weapon
            GameObject cameraObj = new GameObject("FirstPersonCamera");
            cameraObj.transform.parent = player.transform;
            cameraObj.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            cameraObj.transform.localRotation = Quaternion.identity;

            var camComponent = cameraObj.AddComponent<Camera>();
            camComponent.tag = "MainCamera";
            camComponent.nearClipPlane = 0.1f;
            cameraObj.AddComponent<AudioListener>();
            cameraObj.AddComponent<FirstPersonCameraLook>();

            var camData = cameraObj.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null)
            {
                camData = cameraObj.AddComponent<UniversalAdditionalCameraData>();
            }
            camData.renderPostProcessing = true;

            var slingshot = cameraObj.AddComponent<SlingshotWeapon>();
            slingshot.BindPool(pool);

            return player;
        }
    }
}
