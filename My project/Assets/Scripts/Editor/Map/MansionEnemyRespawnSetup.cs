using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using ProjectRac.Enemy;
using ProjectRac.Interaction;

namespace ProjectRac.Editor.Map
{
    public static class MansionEnemyRespawnSetup
    {
        private const string BonecaPrefabPath = "Assets/Prefabs/Enemies/Boneca_MeleeEnemy.prefab";
        private const string MordomoPrefabPath = "Assets/Prefabs/Enemies/Mordomo_RangedEnemy.prefab";
        private const float FloorSplitY = 2.5f;

        [MenuItem("ProjectRac/Map/Setup Enemy Doors, Patrol & Respawn")]
        public static object Apply()
        {
            EnsureDoorOpenerOnPrefab(BonecaPrefabPath);
            EnsureDoorOpenerOnPrefab(MordomoPrefabPath);

            int sceneEnemiesUpdated = 0;
            var healths = Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include);
            for (int i = 0; i < healths.Length; i++)
            {
                if (healths[i].GetComponent<EnemyDoorOpener>() == null)
                {
                    Undo.AddComponent<EnemyDoorOpener>(healths[i].gameObject);
                    sceneEnemiesUpdated++;
                }
            }

            int interiorDoors = 0;
            var doors = Object.FindObjectsByType<DoorController>(FindObjectsInactive.Include);
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

            Transform f1Root = GetOrCreateChild(systemRoot.transform, "Floor1_SpawnPoints");
            Transform f2Root = GetOrCreateChild(systemRoot.transform, "Floor2_SpawnPoints");

            Transform[] f1Points = PopulateFloorSpawnPoints(f1Root, secondFloor: false);
            Transform[] f2Points = PopulateFloorSpawnPoints(f2Root, secondFloor: true);

            patrolNetwork.ConfigurePoints(f1Root, f1Points, f2Root, f2Points);

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
                sceneEnemiesUpdated,
                interiorDoors,
                floor1SpawnPoints = f1Points.Length,
                floor2SpawnPoints = f2Points.Length,
                hasBonecaPrefab = bonecaPrefab != null,
                hasMordomoPrefab = mordomoPrefab != null
            };
        }

        private static Transform GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;

            GameObject obj = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            obj.transform.SetParent(parent, false);
            return obj.transform;
        }

        private static Transform[] PopulateFloorSpawnPoints(Transform container, bool secondFloor)
        {
            // Preserve user-placed points if already present
            if (container.childCount > 0)
            {
                var existing = new Transform[container.childCount];
                for (int i = 0; i < container.childCount; i++) existing[i] = container.GetChild(i);
                return existing;
            }

            var created = new List<Transform>();
            GameObject mansion = GameObject.Find("MansionMap");
            Transform lightsRoot = mansion != null ? mansion.transform.Find("Lighting") : null;
            if (lightsRoot != null)
            {
                var lights = lightsRoot.GetComponentsInChildren<Light>(true);
                int idx = 1;
                for (int i = 0; i < lights.Length; i++)
                {
                    Vector3 lightPos = lights[i].transform.position;
                    bool lightIsF2 = lightPos.y >= FloorSplitY + 2.0f;
                    if (lightIsF2 != secondFloor) continue;

                    Vector3 probe = new Vector3(lightPos.x, secondFloor ? 4.0f : 0.2f, lightPos.z);
                    Vector3 finalPos = probe;
                    if (NavMesh.SamplePosition(probe, out NavMeshHit hit, 4.0f, NavMesh.AllAreas))
                    {
                        finalPos = hit.position;
                    }

                    string ptName = string.Format("Spawn_{0}_{1:D2}", secondFloor ? "F2" : "F1", idx++);
                    GameObject pt = new GameObject(ptName);
                    Undo.RegisterCreatedObjectUndo(pt, "Create " + ptName);
                    pt.transform.SetParent(container, false);
                    pt.transform.position = finalPos;
                    created.Add(pt.transform);
                }
            }

            return created.ToArray();
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
