using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectRac.Enemy;
using ProjectRac.Interaction;

namespace ProjectRac.Editor.Map
{
    public static class MansionEnemyRespawnSetup
    {
        private const string BonecaPrefabPath = "Assets/Prefabs/Enemies/Boneca_MeleeEnemy.prefab";
        private const string MordomoPrefabPath = "Assets/Prefabs/Enemies/Mordomo_RangedEnemy.prefab";

        [MenuItem("ProjectRac/Map/Setup Enemy Doors, Patrol & Respawn")]
        public static object Apply()
        {
            PS2VisualSetupBuilder.ApplyPS2Style();

            EnsureDoorOpenerOnPrefab(BonecaPrefabPath);
            EnsureDoorOpenerOnPrefab(MordomoPrefabPath);

            // Ensure all existing scene enemies have EnemyDoorOpener
            int sceneEnemiesUpdated = 0;
            var healths = Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < healths.Length; i++)
            {
                if (healths[i].GetComponent<EnemyDoorOpener>() == null)
                {
                    Undo.AddComponent<EnemyDoorOpener>(healths[i].gameObject);
                    sceneEnemiesUpdated++;
                }
            }

            // Configure interior doors vs exterior main entrance door
            int interiorDoors = 0;
            var doors = Object.FindObjectsByType<DoorController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < doors.Length; i++)
            {
                bool isMainEntrance = doors[i].transform.parent != null
                    && doors[i].transform.parent.parent != null
                    && doors[i].transform.parent.parent.name == "Door_MainEntrance";

                Undo.RecordObject(doors[i], "Configure Door Enemy Access");
                doors[i].SetAllowEnemyOpen(!isMainEntrance);
                EditorUtility.SetDirty(doors[i]);
                if (!isMainEntrance) interiorDoors++;
            }

            // Setup MansionEnemySystem with MansionPatrolNetwork + EnemyRespawnManager
            GameObject systemRoot = GameObject.Find("MansionEnemySystem");
            if (systemRoot == null)
            {
                systemRoot = new GameObject("MansionEnemySystem");
                Undo.RegisterCreatedObjectUndo(systemRoot, "Create MansionEnemySystem");
            }

            var patrolNetwork = systemRoot.GetComponent<MansionPatrolNetwork>();
            if (patrolNetwork == null) patrolNetwork = Undo.AddComponent<MansionPatrolNetwork>(systemRoot);

            var respawnManager = systemRoot.GetComponent<EnemyRespawnManager>();
            if (respawnManager == null) respawnManager = Undo.AddComponent<EnemyRespawnManager>(systemRoot);

            var bonecaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BonecaPrefabPath);
            var mordomoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MordomoPrefabPath);
            var pool = Object.FindAnyObjectByType<EnemyProjectilePool>(FindObjectsInactive.Include);

            respawnManager.Configure(new[] { bonecaPrefab, mordomoPrefab }, patrolNetwork, pool);
            EditorUtility.SetDirty(patrolNetwork);
            EditorUtility.SetDirty(respawnManager);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            return new
            {
                ok = true,
                fogEnabled = RenderSettings.fog,
                sceneEnemiesUpdated,
                interiorDoors,
                hasBonecaPrefab = bonecaPrefab != null,
                hasMordomoPrefab = mordomoPrefab != null
            };
        }

        private static void EnsureDoorOpenerOnPrefab(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;
            if (prefab.GetComponent<EnemyDoorOpener>() != null) return;

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (root.GetComponent<EnemyDoorOpener>() == null)
                {
                    root.AddComponent<EnemyDoorOpener>();
                }
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
