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
            PlaceSlingshotInHallDrawer();

            GameObject existing = GameObject.Find(LootRootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            GameObject root = new GameObject(LootRootName);
            root.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(root, "Create Mansion Loot, Ammo & Chest");

            // 5 furniture pieces with Slingshot marble ammo packs (+6 each)
            BuildAmmoTable(root.transform, "Movel_Cozinha", "Ammo_Cozinha", new Vector3(-18.5f, 0f, 11.2f), 0f, 6);
            BuildAmmoTable(root.transform, "Movel_SalaDeEstar", "Ammo_SalaDeEstar", new Vector3(13.5f, 0f, -13.5f), 0f, 6);
            BuildAmmoTable(root.transform, "Movel_QuartoFilha", "Ammo_QuartoFilha", new Vector3(-20.2f, 3.85f, -5.0f), 90f, 6);
            BuildAmmoTable(root.transform, "Movel_QuartoMaid", "Ammo_QuartoMaid", new Vector3(-16.5f, 3.85f, 13.8f), 0f, 6);
            BuildAmmoTable(root.transform, "Movel_QuartoPais", "Ammo_QuartoPais", new Vector3(11.0f, 3.85f, -10.5f), 90f, 6);

            // Interactive Chest in Quarto dos Pais (2nd floor, right side) containing Shotgun with 30 ammo
            BuildShotgunChestInParentsRoom(root.transform, new Vector3(20.4f, 3.85f, -3.5f), -90f);

            ConfigurePlayerUnarmed();
            RebakeMansionNavMesh();

            var scene = EditorSceneManager.GetActiveScene();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[MansionLootBuilder] Estilingue na gaveta do Hall, 5 pacotes de bolinhas e Bau com Escopeta (30 mun.) no Quarto dos Pais configurados.");
            return root;
        }

        private static void PlaceSlingshotInHallDrawer()
        {
            var drawerCtrl = Object.FindAnyObjectByType<DrawerController>(FindObjectsInactive.Include);
            Transform drawerPivot = drawerCtrl != null ? drawerCtrl.transform : null;
            if (drawerPivot == null)
            {
                Debug.LogWarning("[MansionLootBuilder] Gaveta do Hall (DrawerController) nao encontrada.");
                return;
            }

            Transform existing = drawerPivot.Find("Slingshot_Pickup");
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

            GameObject pickup = new GameObject("Slingshot_Pickup");
            pickup.transform.SetParent(drawerPivot, false);
            // Rests inside the drawer tray so opening the drawer slides the slingshot out into view.
            pickup.transform.localPosition = new Vector3(0f, 0.04f, 0.02f);
            pickup.transform.localRotation = Quaternion.Euler(90f, 22f, 0f);

            var col = pickup.AddComponent<BoxCollider>();
            col.size = new Vector3(0.32f, 0.36f, 0.14f);

            var comp = pickup.AddComponent<SlingshotPickup>();
            comp.Configure(6);
            drawerCtrl.SetContainedInteractable(comp);
            EditorUtility.SetDirty(drawerCtrl);

            // Clean miniature slingshot visual (handle + forks + bands)
            MansionDoorFactory.CreateLocalBox(pickup.transform, "Handle", new Vector3(0f, -0.07f, 0f), new Vector3(0.035f, 0.14f, 0.035f), SlingshotWood, removeCollider: true);
            GameObject fL = MansionDoorFactory.CreateLocalBox(pickup.transform, "Fork_L", new Vector3(-0.045f, 0.05f, 0f), new Vector3(0.028f, 0.11f, 0.028f), SlingshotWood, removeCollider: true);
            fL.transform.localRotation = Quaternion.Euler(0f, 0f, 26f);
            GameObject fR = MansionDoorFactory.CreateLocalBox(pickup.transform, "Fork_R", new Vector3(0.045f, 0.05f, 0f), new Vector3(0.028f, 0.11f, 0.028f), SlingshotWood, removeCollider: true);
            fR.transform.localRotation = Quaternion.Euler(0f, 0f, -26f);
            MansionDoorFactory.CreateLocalBox(pickup.transform, "Band", new Vector3(0f, 0.09f, -0.02f), new Vector3(0.11f, 0.014f, 0.014f), SlingshotRubber, removeCollider: true);
        }

        private static void BuildAmmoTable(Transform parent, string tableName, string ammoName, Vector3 worldPos, float yawDeg, int ammoAmount)
        {
            GameObject table = new GameObject(tableName);
            table.transform.SetParent(parent, false);
            table.transform.position = worldPos;
            table.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);

            MansionDoorFactory.CreateLocalBox(table.transform, "Top", new Vector3(0f, 0.78f, 0f), new Vector3(1.10f, 0.08f, 0.65f), WoodDark);
            MansionDoorFactory.CreateLocalBox(table.transform, "Body", new Vector3(0f, 0.37f, 0f), new Vector3(0.98f, 0.74f, 0.56f), WoodMedium);

            GameObject ammoObj = new GameObject(ammoName);
            ammoObj.transform.SetParent(table.transform, false);
            ammoObj.transform.localPosition = new Vector3(0f, 0.85f, 0f);

            var col = ammoObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.42f, 0.24f, 0.34f);

            var pickup = ammoObj.AddComponent<SlingshotAmmoPickup>();
            pickup.Configure(ammoAmount);

            // Visual cluster of 5 steel marbles resting on a small tray
            MansionDoorFactory.CreateLocalBox(ammoObj.transform, "Tray", new Vector3(0f, -0.02f, 0f), new Vector3(0.24f, 0.02f, 0.18f), WoodDark, removeCollider: true);
            Vector3[] offsets =
            {
                new Vector3(-0.05f, 0.015f, -0.03f),
                new Vector3(0.05f, 0.015f, -0.03f),
                new Vector3(0f, 0.015f, 0.01f),
                new Vector3(-0.04f, 0.015f, 0.04f),
                new Vector3(0.04f, 0.015f, 0.04f)
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = $"Bolinha_{i + 1:00}";
                ball.transform.SetParent(ammoObj.transform, false);
                ball.transform.localPosition = offsets[i];
                ball.transform.localScale = Vector3.one * 0.048f;
                Object.DestroyImmediate(ball.GetComponent<Collider>());
                MansionDoorFactory.ApplyPersistentColor(ball, MarbleSteel);
            }
        }

        private static void BuildShotgunChestInParentsRoom(Transform parent, Vector3 worldPos, float yawDeg)
        {
            GameObject chest = new GameObject("Bau_QuartoPais");
            chest.transform.SetParent(parent, false);
            chest.transform.position = worldPos;
            chest.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);

            // Chest base walls (hollow interior so the Shotgun rests visibly inside)
            MansionDoorFactory.CreateLocalBox(chest.transform, "Base_Bottom", new Vector3(0f, 0.06f, 0f), new Vector3(1.50f, 0.12f, 0.78f), WoodDark);
            MansionDoorFactory.CreateLocalBox(chest.transform, "Wall_Front", new Vector3(0f, 0.30f, 0.35f), new Vector3(1.50f, 0.42f, 0.08f), WoodMedium);
            MansionDoorFactory.CreateLocalBox(chest.transform, "Wall_Back", new Vector3(0f, 0.30f, -0.35f), new Vector3(1.50f, 0.42f, 0.08f), WoodMedium);
            MansionDoorFactory.CreateLocalBox(chest.transform, "Wall_Left", new Vector3(-0.71f, 0.30f, 0f), new Vector3(0.08f, 0.42f, 0.64f), WoodMedium);
            MansionDoorFactory.CreateLocalBox(chest.transform, "Wall_Right", new Vector3(0.71f, 0.30f, 0f), new Vector3(0.08f, 0.42f, 0.64f), WoodMedium);
            MansionDoorFactory.CreateLocalBox(chest.transform, "Lock_Clasp", new Vector3(0f, 0.46f, 0.40f), new Vector3(0.14f, 0.16f, 0.04f), BrassGold, removeCollider: true);

            // Hinged lid at back edge
            GameObject lidPivot = new GameObject("Lid_Pivot");
            lidPivot.transform.SetParent(chest.transform, false);
            lidPivot.transform.localPosition = new Vector3(0f, 0.51f, -0.35f);
            GameObject lidPanel = MansionDoorFactory.CreateLocalBox(lidPivot.transform, "Lid_Panel", new Vector3(0f, 0.06f, 0.35f), new Vector3(1.54f, 0.12f, 0.80f), WoodDark);
            MansionDoorFactory.CreateLocalBox(lidPivot.transform, "Lid_Trim", new Vector3(0f, 0.125f, 0.35f), new Vector3(1.56f, 0.02f, 0.12f), BrassGold, removeCollider: true);

            // Shotgun pickup resting inside the chest with 30 total ammo
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
