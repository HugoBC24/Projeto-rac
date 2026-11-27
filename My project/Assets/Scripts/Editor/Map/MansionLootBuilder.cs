using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using ProjectRac.Interaction;
using ProjectRac.Weapons;

namespace ProjectRac.Editor.Map
{
    public static class MansionLootBuilder
    {
        public const string LootRootName = "MansionLoot";

        private static readonly Color WoodDark = new Color(0.24f, 0.14f, 0.08f, 1f);
        private static readonly Color WoodMedium = new Color(0.36f, 0.22f, 0.13f, 1f);
        private static readonly Color BrassGold = new Color(0.82f, 0.68f, 0.28f, 1f);
        private static readonly Color MarbleSteel = new Color(0.72f, 0.78f, 0.86f, 1f);
        private static readonly Color SlingshotWood = new Color(0.48f, 0.28f, 0.15f, 1f);
        private static readonly Color SlingshotRubber = new Color(0.84f, 0.68f, 0.26f, 1f);

        [MenuItem("Tools/Build Mansion Weapons, Ammo & Chest")]
        public static GameObject BuildAll()
        {
            PlaceLootInHallDresser();

            GameObject existing = GameObject.Find(LootRootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            GameObject root = new GameObject(LootRootName);
            root.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(root, "Create Mansion Loot, Ammo & Chest");

            // Dressers with interactive sliding drawers containing slingshot ammo (+6 each)
            BuildAmmoDresser(root.transform, "Movel_Cozinha", "Ammo_Cozinha", new Vector3(-18.5f, 0f, 11.2f), 0f, 6);
            BuildAmmoDresser(root.transform, "Movel_SalaDeEstar", "Ammo_SalaDeEstar", new Vector3(13.5f, 0f, -13.5f), 0f, 6);
            BuildAmmoDresser(root.transform, "Movel_QuartoFilha", "Ammo_QuartoFilha", new Vector3(-20.2f, 3.85f, -5.0f), 90f, 6);
            BuildAmmoDresser(root.transform, "Movel_QuartoMaid", "Ammo_QuartoMaid", new Vector3(-16.5f, 3.85f, 13.8f), 0f, 6);
            BuildAmmoDresser(root.transform, "Movel_QuartoPais", "Ammo_QuartoPais", new Vector3(11.0f, 3.85f, -10.5f), 90f, 6);

            // Chest in Quarto dos Pais with Shotgun (all 30 ammo included directly with weapon)
            BuildShotgunChestInParentsRoom(root.transform, new Vector3(20.4f, 3.85f, -3.5f), -90f);

            ConfigurePlayerUnarmed();
            RebakeMansionNavMesh();

            var scene = EditorSceneManager.GetActiveScene();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[MansionLootBuilder] Estilingue e bolinhas em gavetas interativas e Bau com Escopeta configurados.");
            return root;
        }

        private static void PlaceLootInHallDresser()
        {
            var drawers = Object.FindObjectsByType<DrawerController>(FindObjectsInactive.Include);
            DrawerController topDrawer = null;
            DrawerController bottomDrawer = null;

            foreach (var d in drawers)
            {
                if (d.name == "Drawer_Top" && d.transform.IsChildOf(d.transform.root)) topDrawer = d;
                else if (d.name == "Drawer_Bottom" && d.transform.IsChildOf(d.transform.root)) bottomDrawer = d;
            }

            if (topDrawer != null)
            {
                Transform existing = topDrawer.transform.Find("Slingshot_Pickup");
                if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

                GameObject pickup = new GameObject("Slingshot_Pickup");
                pickup.transform.SetParent(topDrawer.transform, false);
                pickup.transform.localPosition = new Vector3(0f, 0.04f, 0.02f);
                pickup.transform.localRotation = Quaternion.Euler(90f, 22f, 0f);

                var col = pickup.AddComponent<BoxCollider>();
                col.size = new Vector3(0.32f, 0.36f, 0.14f);

                var comp = pickup.AddComponent<SlingshotPickup>();
                comp.Configure(6);
                topDrawer.SetContainedInteractable(comp);
                EditorUtility.SetDirty(topDrawer);

                MansionDoorFactory.CreateLocalBox(pickup.transform, "Handle", new Vector3(0f, -0.07f, 0f), new Vector3(0.035f, 0.14f, 0.035f), SlingshotWood, removeCollider: true);
                GameObject fL = MansionDoorFactory.CreateLocalBox(pickup.transform, "Fork_L", new Vector3(-0.045f, 0.05f, 0f), new Vector3(0.028f, 0.11f, 0.028f), SlingshotWood, removeCollider: true);
                fL.transform.localRotation = Quaternion.Euler(0f, 0f, 26f);
                GameObject fR = MansionDoorFactory.CreateLocalBox(pickup.transform, "Fork_R", new Vector3(0.045f, 0.05f, 0f), new Vector3(0.028f, 0.11f, 0.028f), SlingshotWood, removeCollider: true);
                fR.transform.localRotation = Quaternion.Euler(0f, 0f, -26f);
                MansionDoorFactory.CreateLocalBox(pickup.transform, "Band", new Vector3(0f, 0.09f, -0.02f), new Vector3(0.11f, 0.014f, 0.014f), SlingshotRubber, removeCollider: true);
            }

            if (bottomDrawer != null)
            {
                Transform existing = bottomDrawer.transform.Find("Ammo_Hall");
                if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

                GameObject ammoObj = CreateAmmoCluster(bottomDrawer.transform, "Ammo_Hall", new Vector3(0f, -0.06f, 0.02f), 6);
                var pickup = ammoObj.GetComponent<SlingshotAmmoPickup>();
                bottomDrawer.SetContainedInteractable(pickup);
                EditorUtility.SetDirty(bottomDrawer);
            }
        }

        private static void BuildAmmoDresser(Transform parent, string dresserName, string ammoName, Vector3 worldPos, float yawDeg, int ammoAmount)
        {
            GameObject dresser = new GameObject(dresserName);
            dresser.transform.SetParent(parent, false);
            dresser.transform.position = worldPos;
            dresser.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);

            // Carcass (width 1.0m, height 0.86m, depth 0.54m) with front drawer slot
            MansionDoorFactory.CreateLocalBox(dresser.transform, "TopBoard", new Vector3(0f, 0.83f, 0f), new Vector3(1.04f, 0.06f, 0.58f), WoodDark);
            MansionDoorFactory.CreateLocalBox(dresser.transform, "BottomBase", new Vector3(0f, 0.20f, 0f), new Vector3(0.98f, 0.40f, 0.52f), WoodDark);
            MansionDoorFactory.CreateLocalBox(dresser.transform, "BackPanel", new Vector3(0f, 0.60f, -0.24f), new Vector3(0.98f, 0.40f, 0.04f), WoodMedium);
            MansionDoorFactory.CreateLocalBox(dresser.transform, "Side_L", new Vector3(-0.47f, 0.60f, 0f), new Vector3(0.04f, 0.40f, 0.52f), WoodDark);
            MansionDoorFactory.CreateLocalBox(dresser.transform, "Side_R", new Vector3(0.47f, 0.60f, 0f), new Vector3(0.04f, 0.40f, 0.52f), WoodDark);

            // Sliding interactive drawer
            GameObject drawer = new GameObject("Drawer");
            drawer.transform.SetParent(dresser.transform, false);
            drawer.transform.localPosition = new Vector3(0f, 0.60f, 0.02f);

            var drawerCol = drawer.AddComponent<BoxCollider>();
            drawerCol.center = new Vector3(0f, 0f, 0.06f);
            drawerCol.size = new Vector3(0.92f, 0.28f, 0.50f);

            var drawerCtrl = drawer.AddComponent<DrawerController>();
            drawerCtrl.Configure(drawer.transform, Vector3.forward, 0.44f);

            MansionDoorFactory.CreateLocalBox(drawer.transform, "Front", new Vector3(0f, 0f, 0.24f), new Vector3(0.92f, 0.28f, 0.04f), WoodMedium, removeCollider: true);
            MansionDoorFactory.CreateLocalBox(drawer.transform, "Bottom", new Vector3(0f, -0.12f, 0f), new Vector3(0.86f, 0.03f, 0.44f), WoodDark, removeCollider: true);
            MansionDoorFactory.CreateLocalBox(drawer.transform, "Left", new Vector3(-0.42f, 0f, 0f), new Vector3(0.03f, 0.22f, 0.44f), WoodDark, removeCollider: true);
            MansionDoorFactory.CreateLocalBox(drawer.transform, "Right", new Vector3(0.42f, 0f, 0f), new Vector3(0.03f, 0.22f, 0.44f), WoodDark, removeCollider: true);
            MansionDoorFactory.CreateLocalBox(drawer.transform, "Back", new Vector3(0f, 0f, -0.21f), new Vector3(0.86f, 0.22f, 0.03f), WoodDark, removeCollider: true);
            MansionDoorFactory.CreateLocalBox(drawer.transform, "Handle", new Vector3(0f, 0f, 0.28f), new Vector3(0.20f, 0.04f, 0.04f), BrassGold, removeCollider: true);

            // Bolinhas Pickup inside drawer tray
            GameObject ammoObj = CreateAmmoCluster(drawer.transform, ammoName, new Vector3(0f, -0.06f, 0.02f), ammoAmount);
            var pickup = ammoObj.GetComponent<SlingshotAmmoPickup>();
            drawerCtrl.SetContainedInteractable(pickup);
        }

        private static GameObject CreateAmmoCluster(Transform parent, string ammoName, Vector3 localPos, int ammoAmount)
        {
            GameObject ammoObj = new GameObject(ammoName);
            ammoObj.transform.SetParent(parent, false);
            ammoObj.transform.localPosition = localPos;

            var col = ammoObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.05f, 0f);
            col.size = new Vector3(0.36f, 0.16f, 0.28f);

            var pickup = ammoObj.AddComponent<SlingshotAmmoPickup>();
            pickup.Configure(ammoAmount);

            // Visual cluster of 5 steel marbles on a wooden tray
            MansionDoorFactory.CreateLocalBox(ammoObj.transform, "Tray", new Vector3(0f, 0.01f, 0f), new Vector3(0.22f, 0.02f, 0.16f), WoodDark, removeCollider: true);
            Vector3[] offsets =
            {
                new Vector3(-0.045f, 0.04f, -0.025f),
                new Vector3(0.045f, 0.04f, -0.025f),
                new Vector3(0f, 0.04f, 0.01f),
                new Vector3(-0.035f, 0.04f, 0.035f),
                new Vector3(0.035f, 0.04f, 0.035f)
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = $"Bolinha_{i + 1:02}";
                ball.transform.SetParent(ammoObj.transform, false);
                ball.transform.localPosition = offsets[i];
                ball.transform.localScale = Vector3.one * 0.045f;
                Object.DestroyImmediate(ball.GetComponent<Collider>());
                MansionDoorFactory.ApplyPersistentColor(ball, MarbleSteel);
            }

            return ammoObj;
        }

        private static void BuildShotgunChestInParentsRoom(Transform parent, Vector3 worldPos, float yawDeg)
        {
            GameObject chest = new GameObject("Bau_QuartoPais");
            chest.transform.SetParent(parent, false);
            chest.transform.position = worldPos;
            chest.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);

            MansionDoorFactory.CreateLocalBox(chest.transform, "Base_Bottom", new Vector3(0f, 0.06f, 0f), new Vector3(1.50f, 0.12f, 0.78f), WoodDark);
            MansionDoorFactory.CreateLocalBox(chest.transform, "Wall_Front", new Vector3(0f, 0.30f, 0.35f), new Vector3(1.50f, 0.42f, 0.08f), WoodMedium);
            MansionDoorFactory.CreateLocalBox(chest.transform, "Wall_Back", new Vector3(0f, 0.30f, -0.35f), new Vector3(1.50f, 0.42f, 0.08f), WoodMedium);
            MansionDoorFactory.CreateLocalBox(chest.transform, "Wall_Left", new Vector3(-0.71f, 0.30f, 0f), new Vector3(0.08f, 0.42f, 0.64f), WoodMedium);
            MansionDoorFactory.CreateLocalBox(chest.transform, "Wall_Right", new Vector3(0.71f, 0.30f, 0f), new Vector3(0.08f, 0.42f, 0.64f), WoodMedium);
            MansionDoorFactory.CreateLocalBox(chest.transform, "Lock_Clasp", new Vector3(0f, 0.46f, 0.40f), new Vector3(0.14f, 0.16f, 0.04f), BrassGold, removeCollider: true);

            GameObject lidPivot = new GameObject("Lid_Pivot");
            lidPivot.transform.SetParent(chest.transform, false);
            lidPivot.transform.localPosition = new Vector3(0f, 0.51f, -0.35f);
            GameObject lidPanel = MansionDoorFactory.CreateLocalBox(lidPivot.transform, "Lid_Panel", new Vector3(0f, 0.06f, 0.35f), new Vector3(1.54f, 0.12f, 0.80f), WoodDark);
            MansionDoorFactory.CreateLocalBox(lidPivot.transform, "Lid_Trim", new Vector3(0f, 0.125f, 0.35f), new Vector3(1.56f, 0.02f, 0.12f), BrassGold, removeCollider: true);

            GameObject sgObj = new GameObject("Shotgun_Pickup");
            sgObj.transform.SetParent(chest.transform, false);
            sgObj.transform.localPosition = new Vector3(0f, 0.24f, 0f);
            sgObj.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            var sgCol = sgObj.AddComponent<BoxCollider>();
            sgCol.size = new Vector3(0.45f, 0.30f, 1.15f);
            var sgPickup = sgObj.AddComponent<ShotgunPickup>();
            sgPickup.ConfigureAmmo(30);
            sgPickup.ResolveWorldModel();

            var chestCtrl = lidPanel.AddComponent<ChestController>();
            chestCtrl.Configure(lidPivot.transform, sgCol);
        }

        private static void ConfigurePlayerUnarmed()
        {
            var inv = Object.FindAnyObjectByType<PlayerWeaponInventory>();
            if (inv != null)
            {
                inv.SetStartingLoadout(startSlingshot: false, startShotgun: false);
                EditorUtility.SetDirty(inv);
            }

            var slingshot = Object.FindAnyObjectByType<SlingshotWeapon>(FindObjectsInactive.Include);
            if (slingshot != null)
            {
                slingshot.SetAmmo(0);
                EditorUtility.SetDirty(slingshot);
            }
        }

        private static void RebakeMansionNavMesh()
        {
            GameObject mansion = GameObject.Find(MansionMapBuilder.MansionRootName);
            if (mansion == null) return;
            Physics.SyncTransforms();

            var surface = mansion.GetComponent<NavMeshSurface>();
            if (surface != null)
            {
                surface.collectObjects = CollectObjects.Volume;
                surface.center = new Vector3(0f, 3.8f, 0f);
                surface.size = new Vector3(48f, 10f, 36f);
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.BuildNavMesh();
            }
        }
    }
}
