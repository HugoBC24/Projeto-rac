using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;
using ProjectRac.Enemy;

namespace ProjectRac.Editor
{
    public static class EnemyBuilder
    {
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

        public static GameObject SpawnMeleeEnemy(Transform parent, Vector3 spawnPosition)
        {
            GameObject enemyObj = GameObject.Find("MeleeEnemy");
            if (enemyObj != null)
            {
                Undo.DestroyObjectImmediate(enemyObj);
            }

            enemyObj = new GameObject("MeleeEnemy");
            enemyObj.tag = "Untagged";
            enemyObj.transform.parent = parent;
            enemyObj.transform.position = spawnPosition;
            Undo.RegisterCreatedObjectUndo(enemyObj, "Create Melee Enemy");

            var collider = enemyObj.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1.0f, 0f);
            collider.radius = 0.45f;
            collider.height = 2.0f;

            var agent = enemyObj.AddComponent<NavMeshAgent>();
            agent.radius = 0.45f;
            agent.height = 2.0f;
            agent.baseOffset = 0f;
            agent.speed = 3.2f;
            agent.acceleration = 14f;
            agent.angularSpeed = 280f;
            agent.stoppingDistance = 1.5f;

            enemyObj.AddComponent<EnemyHealth>();
            enemyObj.AddComponent<EnemyMeleeAI>();

            return enemyObj;
        }
    }
}
