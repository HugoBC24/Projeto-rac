using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectRac.Enemy
{
    public class MansionPatrolNetwork : MonoBehaviour
    {
        public static MansionPatrolNetwork Instance { get; private set; }

        [Header("Floor 1 Waypoints")]
        [SerializeField] private Vector3[] floor1Waypoints =
        {
            new Vector3(-0.8f, 0.1f, -4.5f),   // Hall Center
            new Vector3(1.2f, 0.1f, 3.4f),     // Corredor Norte F1
            new Vector3(9.0f, 0.1f, -4.2f),    // Sala de Estar
            new Vector3(-9.8f, 0.1f, -4.2f),   // Sala de Jantar
            new Vector3(-9.8f, 0.1f, 6.4f),    // Cozinha
            new Vector3(-0.5f, 0.1f, 7.6f),    // Lavanderia
            new Vector3(9.3f, 0.1f, 6.4f)      // Biblioteca
        };

        [Header("Floor 2 Waypoints")]
        [SerializeField] private Vector3[] floor2Waypoints =
        {
            new Vector3(-0.5f, 3.95f, 3.4f),   // Mezanino / Corredor Norte F2
            new Vector3(-0.7f, 3.95f, -9.6f),  // Corredor Sul F2
            new Vector3(-10.8f, 3.95f, -3.2f), // Quarto da Filha
            new Vector3(10.5f, 3.95f, -2.5f),  // Quarto dos Pais
            new Vector3(3.9f, 3.95f, 7.6f),    // Suite
            new Vector3(-7.7f, 3.95f, 7.6f)    // Quarto da Maid
        };

        [SerializeField] private float minPatrolStepDistance = 5.0f;

        private readonly List<Vector3> _allWaypoints = new List<Vector3>(16);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            RebuildCache();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void RebuildCache()
        {
            _allWaypoints.Clear();
            if (floor1Waypoints != null) _allWaypoints.AddRange(floor1Waypoints);
            if (floor2Waypoints != null) _allWaypoints.AddRange(floor2Waypoints);
        }

        public bool TryGetRandomWaypoint(Vector3 currentPos, out Vector3 destination)
        {
            if (_allWaypoints.Count == 0) RebuildCache();
            if (_allWaypoints.Count == 0)
            {
                destination = currentPos;
                return false;
            }

            float minDistSq = minPatrolStepDistance * minPatrolStepDistance;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector3 candidate = _allWaypoints[Random.Range(0, _allWaypoints.Count)];
                if ((candidate - currentPos).sqrMagnitude < minDistSq && attempt < 8) continue;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
                {
                    destination = hit.position;
                    return true;
                }
            }

            destination = currentPos;
            return false;
        }

        public bool TryGetSpawnPointForFloor(bool secondFloor, out Vector3 spawnPos)
        {
            Vector3[] pool = secondFloor ? floor2Waypoints : floor1Waypoints;
            if (pool == null || pool.Length == 0)
            {
                spawnPos = Vector3.zero;
                return false;
            }

            int startIdx = Random.Range(0, pool.Length);
            for (int i = 0; i < pool.Length; i++)
            {
                Vector3 candidate = pool[(startIdx + i) % pool.Length];
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
                {
                    spawnPos = hit.position;
                    return true;
                }
            }

            spawnPos = pool[0];
            return false;
        }
    }
}
