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
            if (arenaRoot != null) Undo.DestroyObjectImmediate(arenaRoot);
            arenaRoot = new GameObject("TestArena");
            Undo.RegisterCreatedObjectUndo(arenaRoot, "Create Arena Root");

            // 2. Main Floor & Walls
            BuildFloorAndWalls(arenaRoot.transform);

            // 3. Platforms & Ramp (STRICTLY PRESERVED: do NOT change transforms)
            BuildPlatformsAndRamp(arenaRoot.transform);

            // 4. Interactive Test Objects
            CreateInteractable(arenaRoot.transform, "Interactive_Cube_Alpha", new Vector3(-6f, 1f, 8f));
            CreateInteractable(arenaRoot.transform, "Interactive_Cube_Beta", new Vector3(-10f, 1f, 8f));
            CreateInteractable(arenaRoot.transform, "Interactive_Cube_Gamma", new Vector3(10f, 4f, 18f));

            // 5. Physics Target Stack (Boxes), Wall With Door & Shotgun Pickup Pedestal
            BuildPhysicsTargetStack(arenaRoot.transform, new Vector3(0f, 0f, 16f));
            DoorArenaBuilder.BuildWallWithDoor(arenaRoot.transform);
            CreateShotgunPickup(arenaRoot.transform, new Vector3(10f, 2.72f, 17f));

            // 6. Setup Shoot Pools (Slingshot + Shotgun)
            var (slingshotPool, pelletPool) = SetupShootPools(arenaRoot.transform);

            // 7. Setup Player with dedicated weapons & health
            GameObject player = SetupPlayer(slingshotPool, pelletPool);

            // 8. Setup Responsive uGUI Canvas (Health, Stamina, Crosshair, Ammo)
            HUDCanvasBuilder.SetupHUDCanvas(player);

            // 9. Setup NavMesh & Spawn Enemies (Melee + Ranged)
            EnemyBuilder.SetupNavMesh(arenaRoot.transform);
            EnemyBuilder.SpawnMeleeEnemy(arenaRoot.transform, new Vector3(-12f, 0f, 12f));
            var enemyPool = EnemyBuilder.EnsureEnemyProjectilePool(arenaRoot.transform);
            EnemyBuilder.SpawnRangedEnemy(arenaRoot.transform, new Vector3(4f, 0f, 21f), enemyPool);

            // Mark Scene Dirty and Save
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("[TestArenaBuilder] Arena de Teste FPAA configurada com sucesso na SampleScene!");
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
            // PLATFORMS AND RAMP: Preserved exactly as requested
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
            platformHigh.transform.position = new Vector3(10f, 1.10f, 17f);
            platformHigh.transform.localScale = new Vector3(6f, 2.20f, 6f);
        }

        private static void BuildPhysicsTargetStack(Transform root, Vector3 centerPos)
        {
            GameObject stackRoot = new GameObject("PhysicsTarget_Boxes");
            stackRoot.transform.parent = root;
            stackRoot.transform.position = centerPos;

            Vector3[] offsets = {
                new Vector3(-1.1f, 0.5f, 0f), new Vector3(0f, 0.5f, 0f), new Vector3(1.1f, 0.5f, 0f),
                new Vector3(-0.55f, 1.5f, 0f), new Vector3(0.55f, 1.5f, 0f), new Vector3(0f, 2.5f, 0f)
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                CreatePhysicsBox(stackRoot.transform, offsets[i]);
            }
        }

        private static void CreatePhysicsBox(Transform parent, Vector3 localPos)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "TargetBox";
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPos;
            box.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);

            var rb = box.AddComponent<Rigidbody>();
            rb.mass = 1.2f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
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

        private static (SlingshotProjectilePool, ShotgunPelletPool) SetupShootPools(Transform root)
        {
            GameObject poolsRoot = new GameObject("ShootPools");
            poolsRoot.transform.parent = root;

            GameObject slingObj = new GameObject("SlingshotPool");
            slingObj.transform.parent = poolsRoot.transform;
            var slingPool = slingObj.AddComponent<SlingshotProjectilePool>();
            slingPool.Configure(null, 25);

            GameObject shotObj = new GameObject("ShotgunPool");
            shotObj.transform.parent = poolsRoot.transform;
            var shotPool = shotObj.AddComponent<ShotgunPelletPool>();
            shotPool.Configure(null, 48);

            return (slingPool, shotPool);
        }

        private static GameObject SetupPlayer(SlingshotProjectilePool slingPool, ShotgunPelletPool shotPool)
        {
            var existingCams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            foreach (var cam in existingCams)
            {
                if (cam.transform.parent == null || cam.transform.parent.name != "Player")
                    Undo.DestroyObjectImmediate(cam.gameObject);
            }

            GameObject player = GameObject.Find("Player");
            if (player != null) Undo.DestroyObjectImmediate(player);

            player = new GameObject("Player");
            player.tag = "Player";
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

            player.AddComponent<PlayerHealth>();
            player.AddComponent<PlayerStamina>();
            player.AddComponent<FirstPersonController>();
            player.AddComponent<PlayerInteraction>();
            player.AddComponent<StaminaExhaustionPostProcess>();

            // Setup Camera
            GameObject cameraObj = new GameObject("FirstPersonCamera");
            cameraObj.transform.parent = player.transform;
            cameraObj.transform.localPosition = new Vector3(0f, 1.7f, 0f);

            var camComponent = cameraObj.AddComponent<Camera>();
            camComponent.tag = "MainCamera";
            camComponent.nearClipPlane = 0.1f;
            cameraObj.AddComponent<AudioListener>();
            cameraObj.AddComponent<FirstPersonCameraLook>();

            var camData = cameraObj.GetComponent<UniversalAdditionalCameraData>() ?? cameraObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            // Setup Dedicated Weapons Container
            GameObject weaponsRoot = new GameObject("Weapons");
            weaponsRoot.transform.parent = cameraObj.transform;
            weaponsRoot.transform.localPosition = Vector3.zero;

            var slingshotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlingshotPrefabGenerator.PrefabPath)
                                  ?? SlingshotPrefabGenerator.GeneratePrefab();

            // Slingshot Weapon (SetParent(..., false) keeps localPosition at zero on camera)
            GameObject slingObj = new GameObject("Slingshot");
            slingObj.transform.SetParent(weaponsRoot.transform, false);
            var slingshot = slingObj.AddComponent<SlingshotWeapon>();
            slingshot.SetCustomViewmodelPrefab(slingshotPrefab);
            slingshot.BindPool(slingPool);

            // Shotgun Weapon with Dedicated ShootPoint aligned with barrel muzzle
            GameObject shotObj = new GameObject("Shotgun");
            shotObj.transform.SetParent(weaponsRoot.transform, false);
            var shotgun = shotObj.AddComponent<ShotgunWeapon>();

            GameObject shootPointObj = new GameObject("ShootPoint");
            shootPointObj.transform.SetParent(shotObj.transform, false);
            shootPointObj.transform.localPosition = new Vector3(0.24f, -0.22f, 1.05f);

            shotgun.SetShootPoint(shootPointObj.transform);
            shotgun.BindReferences(camComponent, inputHandler, shotPool);

            var inventory = player.AddComponent<PlayerWeaponInventory>();
            inventory.BindWeapons(slingshot, shotgun);

            return player;
        }

        private static void CreateShotgunPickup(Transform parent, Vector3 pickupPos)
        {
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Shotgun_Pedestal";
            pedestal.transform.parent = parent;
            pedestal.transform.position = new Vector3(pickupPos.x, 2.45f, pickupPos.z);
            pedestal.transform.localScale = new Vector3(1.2f, 0.25f, 1.2f);

            // Replace CapsuleCollider (which inflates above short cylinders and blocks raycasts)
            // with a flush BoxCollider matching the cylinder's visual top (y = 2.70).
            var capsule = pedestal.GetComponent<Collider>();
            if (capsule != null) Object.DestroyImmediate(capsule);
            var pedBox = pedestal.AddComponent<BoxCollider>();
            pedBox.size = new Vector3(1f, 2f, 1f);

            GameObject lightObj = new GameObject("Pedestal_Light");
            lightObj.transform.SetParent(pedestal.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.95f, 0.8f);
            light.intensity = 1.2f;
            light.range = 5f;

            GameObject pickupObj = new GameObject("ShotgunPickup");
            pickupObj.transform.parent = parent;
            pickupObj.transform.position = pickupPos;

            var boxCol = pickupObj.AddComponent<BoxCollider>();
            boxCol.isTrigger = false;
            boxCol.size = new Vector3(0.8f, 0.5f, 1.5f);
            boxCol.center = new Vector3(0f, 0.25f, 0f);

            var pickup = pickupObj.AddComponent<ShotgunPickup>();
            pickup.ResolveWorldModel();
        }
    }
}
