using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using ProjectRac.Editor.Enemy;
using ProjectRac.Editor.UI;
using ProjectRac.Enemy;
using ProjectRac.Interaction;
using ProjectRac.UI;

namespace ProjectRac.Editor.Map
{
    public static class MansionDiningRoomBuilder
    {
        public const string DiningRoomGroupName = "SalaDeJantar_MordomoEvent";

        // Sala de Jantar (1st floor, left side): world X in [-22.50, -6.90], Z in [-15.90, +3.30].
        private static readonly Vector3 TableWorldPos = new Vector3(-14.70f, 0f, -6.30f);
        private static readonly Vector3 MordomoWorldPos = new Vector3(-17.80f, 0f, -6.30f);

        private static readonly Color TableWoodDark = new Color(0.24f, 0.14f, 0.08f, 1f);
        private static readonly Color ChairWood = new Color(0.32f, 0.19f, 0.11f, 1f);
        private static readonly Color ClothRunner = new Color(0.46f, 0.10f, 0.12f, 1f);
        private static readonly Color PlateWhite = new Color(0.88f, 0.86f, 0.82f, 1f);
        private static readonly Color BrassCandle = new Color(0.80f, 0.68f, 0.30f, 1f);

        [MenuItem("Tools/Build Dining Room Table & Mordomo Encounter")]
        public static GameObject BuildDiningRoomTableAndMordomo()
        {
            GameObject mordomoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MordomoEnemyAssetSetup.PrefabPath)
                                    ?? MordomoEnemyAssetSetup.BuildAll();

            VisualNovelDialogueUI vnUI = Object.FindAnyObjectByType<VisualNovelDialogueUI>(FindObjectsInactive.Include);
            EnemyProjectilePool pool = Object.FindAnyObjectByType<EnemyProjectilePool>(FindObjectsInactive.Include);

            GameObject existing = GameObject.Find(DiningRoomGroupName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            GameObject root = new GameObject(DiningRoomGroupName);
            root.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(root, "Create Dining Room Table & Mordomo Encounter");

            BuildDiningTable(root.transform);
            EnemyRangedAI mordomoAI = PlaceMordomoBesideTable(root.transform, mordomoPrefab, pool);
            BuildEntryTrigger(root.transform, mordomoAI, vnUI);

            UpgradeTestArenaRangedEnemy(mordomoPrefab, pool);
            RebakeMansionNavMesh();

            var scene = EditorSceneManager.GetActiveScene();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[MansionDiningRoomBuilder] Mesa de jantar, Mordomo e gatilho de entrada montados na Sala de Jantar.");
            return root;
        }

        private static void BuildDiningTable(Transform parent)
        {
            GameObject table = new GameObject("Mesa_SalaDeJantar");
            table.transform.SetParent(parent, false);
            table.transform.position = TableWorldPos;

            // Long dark-wood tabletop (1.9m wide x 4.8m long along Z)
            MansionDoorFactory.CreateLocalBox(table.transform, "TableTop", new Vector3(0f, 0.80f, 0f), new Vector3(1.90f, 0.10f, 4.80f), TableWoodDark);
            MansionDoorFactory.CreateLocalBox(table.transform, "TableRunner", new Vector3(0f, 0.855f, 0f), new Vector3(0.55f, 0.01f, 4.40f), ClothRunner, removeCollider: true);

            // 4 sturdy legs
            float[] lx = { -0.78f, 0.78f };
            float[] lz = { -2.15f, 2.15f };
            int legIdx = 1;
            for (int ix = 0; ix < lx.Length; ix++)
            {
                for (int iz = 0; iz < lz.Length; iz++)
                {
                    MansionDoorFactory.CreateLocalBox(table.transform, $"Leg_{legIdx++:00}", new Vector3(lx[ix], 0.38f, lz[iz]), new Vector3(0.14f, 0.76f, 0.14f), TableWoodDark);
                }
            }

            // Center candelabra & plates
            MansionDoorFactory.CreateLocalBox(table.transform, "Candelabra_Base", new Vector3(0f, 0.96f, 0f), new Vector3(0.14f, 0.22f, 0.14f), BrassCandle, removeCollider: true);
            float[] plateZ = { -1.55f, 0f, 1.55f };
            for (int i = 0; i < plateZ.Length; i++)
            {
                MansionDoorFactory.CreateLocalBox(table.transform, $"Plate_L_{i + 1}", new Vector3(-0.56f, 0.86f, plateZ[i]), new Vector3(0.26f, 0.02f, 0.26f), PlateWhite, removeCollider: true);
                MansionDoorFactory.CreateLocalBox(table.transform, $"Plate_R_{i + 1}", new Vector3(0.56f, 0.86f, plateZ[i]), new Vector3(0.26f, 0.02f, 0.26f), PlateWhite, removeCollider: true);
                BuildChair(table.transform, $"Chair_L_{i + 1}", new Vector3(-1.22f, 0f, plateZ[i]), 90f);
                BuildChair(table.transform, $"Chair_R_{i + 1}", new Vector3(1.22f, 0f, plateZ[i]), -90f);
            }
        }

        private static void BuildChair(Transform parent, string name, Vector3 localPos, float yawDeg)
        {
            GameObject chair = new GameObject(name);
            chair.transform.SetParent(parent, false);
            chair.transform.localPosition = localPos;
            chair.transform.localRotation = Quaternion.Euler(0f, yawDeg, 0f);

            MansionDoorFactory.CreateLocalBox(chair.transform, "Seat", new Vector3(0f, 0.46f, 0f), new Vector3(0.48f, 0.06f, 0.48f), ChairWood);
            MansionDoorFactory.CreateLocalBox(chair.transform, "Back", new Vector3(0f, 0.78f, -0.21f), new Vector3(0.48f, 0.58f, 0.06f), ChairWood, removeCollider: true);
            MansionDoorFactory.CreateLocalBox(chair.transform, "Legs", new Vector3(0f, 0.22f, 0f), new Vector3(0.44f, 0.44f, 0.44f), ChairWood, removeCollider: true);
        }

        private static EnemyRangedAI PlaceMordomoBesideTable(Transform parent, GameObject mordomoPrefab, EnemyProjectilePool pool)
        {
            if (mordomoPrefab == null) return null;

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(mordomoPrefab, parent);
            inst.name = "Mordomo_SalaDeJantar";
            inst.transform.position = MordomoWorldPos;
            inst.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // Facing East toward table & Hall door

            var ai = inst.GetComponent<EnemyRangedAI>();
            if (ai != null)
            {
                ai.SetStartInIdle(true);
                if (pool != null) ai.BindProjectilePool(pool);
                EditorUtility.SetDirty(ai);
            }

            return ai;
        }

        private static void BuildEntryTrigger(Transform parent, EnemyRangedAI mordomoAI, VisualNovelDialogueUI vnUI)
        {
            GameObject trigObj = new GameObject("DiningRoom_EntryTrigger");
            trigObj.transform.SetParent(parent, false);
            // Covers interior of Sala de Jantar inset slightly from walls so entering via either door triggers immediately.
            trigObj.transform.position = new Vector3(-14.70f, 1.50f, -6.30f);

            var box = trigObj.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(14.2f, 3.0f, 17.8f);

            Sprite guestSprite = VisualNovelPlaceholderArt.EnsureGuestPlaceholder();
            var dialogues = new List<VisualNovelDialogueLine>
            {
                new VisualNovelDialogueLine
                {
                    speakerName = "Mordomo",
                    speakerNameColor = new Color(0.80f, 0.85f, 0.95f, 1f),
                    text = "Convidados nao deveriam vagar pela sala de jantar a esta hora... O jantar ja foi servido.",
                    characterSprite = guestSprite,
                    slot = CharacterSlot.Right,
                    keepOtherCharacters = false
                },
                new VisualNovelDialogueLine
                {
                    speakerName = "",
                    speakerNameColor = Color.white,
                    text = "O mordomo ergue a mao e avanca na sua direcao!",
                    characterSprite = guestSprite,
                    slot = CharacterSlot.Right,
                    keepOtherCharacters = false
                }
            };

            var trigger = trigObj.AddComponent<DiningRoomEncounterTrigger>();
            trigger.Configure(mordomoAI, vnUI, dialogues);
        }

        private static void UpgradeTestArenaRangedEnemy(GameObject mordomoPrefab, EnemyProjectilePool pool)
        {
            if (mordomoPrefab == null) return;
            GameObject existing = GameObject.Find("RangedEnemy");
            if (existing == null) return;

            Transform parent = existing.transform.parent;
            Vector3 pos = existing.transform.position;
            Quaternion rot = existing.transform.rotation;
            Undo.DestroyObjectImmediate(existing);

            GameObject upgraded = (GameObject)PrefabUtility.InstantiatePrefab(mordomoPrefab, parent);
            upgraded.name = "RangedEnemy";
            upgraded.transform.SetPositionAndRotation(pos, rot);
            var ai = upgraded.GetComponent<EnemyRangedAI>();
            if (ai != null && pool != null) ai.BindProjectilePool(pool);
            Undo.RegisterCreatedObjectUndo(upgraded, "Upgrade RangedEnemy to Mordomo");
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
