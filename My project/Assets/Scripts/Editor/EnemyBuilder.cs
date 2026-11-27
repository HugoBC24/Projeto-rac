using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;
using ProjectRac.Enemy;

namespace ProjectRac.Editor
{
    public static class EnemyBuilder
    {
        private const string MaterialsFolder = "Assets/Materials/Enemies";

        public static void SetupNavMesh(Transform arenaRoot)
        {
            var surface = arenaRoot.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = arenaRoot.gameObject.AddComponent<NavMeshSurface>();
            }

            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
        }

        public static EnemyProjectilePool EnsureEnemyProjectilePool(Transform arenaRoot)
        {
            Transform poolsRoot = arenaRoot.Find("ShootPools");
            if (poolsRoot == null)
            {
                GameObject poolsObj = new GameObject("ShootPools");
                poolsObj.transform.SetParent(arenaRoot, false);
                poolsRoot = poolsObj.transform;
            }

            Transform existing = poolsRoot.Find("EnemyProjectilePool");
            if (existing != null)
            {
                var existingPool = existing.GetComponent<EnemyProjectilePool>();
                if (existingPool != null) return existingPool;
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            GameObject poolObj = new GameObject("EnemyProjectilePool");
            poolObj.transform.SetParent(poolsRoot, false);
            Undo.RegisterCreatedObjectUndo(poolObj, "Create Enemy Projectile Pool");

            var pool = poolObj.AddComponent<EnemyProjectilePool>();
            pool.Configure(null, 20);
            return pool;
        }

        public static GameObject SpawnMeleeEnemy(Transform parent, Vector3 spawnPosition)
        {
            GameObject bonecaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Enemy.BonecaEnemyAssetSetup.PrefabPath);
            if (bonecaPrefab != null)
            {
                GameObject existing = GameObject.Find("MeleeEnemy");
                if (existing != null) Undo.DestroyObjectImmediate(existing);

                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(bonecaPrefab, parent);
                inst.name = "MeleeEnemy";
                inst.transform.position = spawnPosition;
                Undo.RegisterCreatedObjectUndo(inst, "Create MeleeEnemy");
                return inst;
            }

            GameObject enemyObj = CreateBaseEnemy(parent, "MeleeEnemy", spawnPosition, 3.2f, 1.5f);
            enemyObj.AddComponent<EnemyHealth>();
            enemyObj.AddComponent<EnemyMeleeAI>();
            enemyObj.AddComponent<EnemyMeleeAnimator>();
            return enemyObj;
        }

        public static GameObject SpawnRangedEnemy(Transform parent, Vector3 spawnPosition, EnemyProjectilePool pool = null)
        {
            GameObject enemyObj = CreateBaseEnemy(parent, "RangedEnemy", spawnPosition, 2.8f, 1.0f);

            var health = enemyObj.AddComponent<EnemyHealth>();
            health.SetMaxHealth(60f);

            var rangedAI = enemyObj.AddComponent<EnemyRangedAI>();
            if (pool != null) rangedAI.BindProjectilePool(pool);

            // Build procedural visuals in Edit Mode and assign persistent URP materials so colors survive scene reloads.
            EnemyRangedVisuals.EnsureVisuals(enemyObj.transform);
            AssignPersistentRangedMaterials(enemyObj.transform.Find("RangedEnemyMesh"));

            return enemyObj;
        }

        private static GameObject CreateBaseEnemy(
            Transform parent,
            string enemyName,
            Vector3 spawnPosition,
            float baseSpeed,
            float stoppingDistance)
        {
            GameObject existing = GameObject.Find(enemyName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            GameObject enemyObj = new GameObject(enemyName);
            enemyObj.tag = "Untagged";
            enemyObj.transform.SetParent(parent, false);
            enemyObj.transform.position = spawnPosition;
            Undo.RegisterCreatedObjectUndo(enemyObj, "Create " + enemyName);

            var collider = enemyObj.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1.0f, 0f);
            collider.radius = 0.45f;
            collider.height = 2.0f;

            var agent = enemyObj.AddComponent<NavMeshAgent>();
            agent.radius = 0.45f;
            agent.height = 2.0f;
            agent.baseOffset = 0f;
            agent.speed = baseSpeed;
            agent.acceleration = 14f;
            agent.angularSpeed = 280f;
            agent.stoppingDistance = stoppingDistance;

            return enemyObj;
        }

        private static void AssignPersistentRangedMaterials(Transform meshRoot)
        {
            if (meshRoot == null) return;

            ApplyPersistentMaterial(meshRoot.Find("Body"), "Mat_Ranged_Body", new Color(0.14f, 0.28f, 0.36f, 1f));
            ApplyPersistentMaterial(meshRoot.Find("Visor"), "Mat_Ranged_Visor", new Color(1f, 0.65f, 0.1f, 1f));
            ApplyPersistentMaterial(meshRoot.Find("ShoulderPack"), "Mat_Ranged_Trim", new Color(0.22f, 0.24f, 0.27f, 1f));
            ApplyPersistentMaterial(meshRoot.Find("BlasterBody"), "Mat_Ranged_Trim", new Color(0.22f, 0.24f, 0.27f, 1f));
            ApplyPersistentMaterial(meshRoot.Find("MuzzleGlow"), "Mat_Ranged_Muzzle", EnemyRangedVisuals.IdleMuzzleColor);
        }

        private static void ApplyPersistentMaterial(Transform part, string matName, Color color)
        {
            if (part == null) return;
            var renderer = part.GetComponent<Renderer>();
            if (renderer == null) return;

            string assetPath = $"{MaterialsFolder}/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                EnsureFolder(MaterialsFolder);
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else mat.color = color;
                AssetDatabase.CreateAsset(mat, assetPath);
            }

            renderer.sharedMaterial = mat;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            AssetDatabase.CreateFolder("Assets/Materials", "Enemies");
        }
    }
}
