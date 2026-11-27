using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectRac.Enemy
{
    public class MansionPatrolNetwork : MonoBehaviour
    {
        public static MansionPatrolNetwork Instance { get; private set; }

        [Header("Spawn & Patrol Point Containers (Empty GameObjects)")]
        [SerializeField] private Transform floor1PointsRoot;
        [SerializeField] private Transform floor2PointsRoot;

        [Header("Explicit Point Overrides (Optional)")]
        [SerializeField] private Transform[] floor1Points;
        [SerializeField] private Transform[] floor2Points;

        [Header("Patrol Settings")]
        [SerializeField] private float minPatrolStepDistance = 5.0f;
        [SerializeField] private float navMeshSampleRadius = 3.5f;

        private readonly List<Transform> _floor1 = new List<Transform>(16);
        private readonly List<Transform> _floor2 = new List<Transform>(16);
        private readonly List<Transform> _allPoints = new List<Transform>(32);
        private NavMeshPath _pathCheck;

        public int Floor1Count => _floor1.Count;
        public int Floor2Count => _floor2.Count;

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

        public void ConfigurePoints(Transform f1Root, Transform[] f1Points, Transform f2Root, Transform[] f2Points)
        {
            floor1PointsRoot = f1Root;
            floor1Points = f1Points;
            floor2PointsRoot = f2Root;
            floor2Points = f2Points;
            RebuildCache();
        }

        public void RebuildCache()
        {
            _floor1.Clear();
            _floor2.Clear();
            _allPoints.Clear();

            CollectPoints(floor1PointsRoot, floor1Points, _floor1);
            CollectPoints(floor2PointsRoot, floor2Points, _floor2);

            _allPoints.AddRange(_floor1);
            _allPoints.AddRange(_floor2);
        }

        private static void CollectPoints(Transform root, Transform[] explicitList, List<Transform> target)
        {
            if (root != null)
            {
                for (int i = 0; i < root.childCount; i++)
                {
                    Transform child = root.GetChild(i);
                    if (child != null && child.gameObject.activeInHierarchy)
                    {
                        target.Add(child);
                    }
                }
            }

            if (target.Count == 0 && explicitList != null)
            {
                for (int i = 0; i < explicitList.Length; i++)
                {
                    if (explicitList[i] != null) target.Add(explicitList[i]);
                }
            }
        }

        public bool TryGetRandomWaypoint(Vector3 currentPos, out Vector3 destination)
        {
            if (_allPoints.Count == 0) RebuildCache();
            if (_allPoints.Count == 0)
            {
                destination = currentPos;
                return false;
            }

            if (_pathCheck == null) _pathCheck = new NavMeshPath();
            float minDistSq = minPatrolStepDistance * minPatrolStepDistance;
            Vector3 fallbackPos = currentPos;
            bool hasFallback = false;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                Transform t = _allPoints[Random.Range(0, _allPoints.Count)];
                if (t == null) continue;

                Vector3 candidate = t.position;
                if ((candidate - currentPos).sqrMagnitude < minDistSq && attempt < 12) continue;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                {
                    if (!hasFallback)
                    {
                        fallbackPos = hit.position;
                        hasFallback = true;
                    }

                    if (NavMesh.CalculatePath(currentPos, hit.position, NavMesh.AllAreas, _pathCheck)
                        && _pathCheck.status == NavMeshPathStatus.PathComplete)
                    {
                        destination = hit.position;
                        return true;
                    }
                }
            }

            if (hasFallback)
            {
                destination = fallbackPos;
                return true;
            }

            destination = currentPos;
            return false;
        }

        public bool TryGetSpawnPointForFloor(bool secondFloor, out Vector3 spawnPos)
        {
            if (_allPoints.Count == 0) RebuildCache();
            List<Transform> pool = secondFloor ? _floor2 : _floor1;
            if (pool.Count == 0)
            {
                spawnPos = Vector3.zero;
                return false;
            }

            int startIdx = Random.Range(0, pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                Transform t = pool[(startIdx + i) % pool.Count];
                if (t == null) continue;

                if (NavMesh.SamplePosition(t.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                {
                    spawnPos = hit.position;
                    return true;
                }
            }

            spawnPos = pool[0] != null ? pool[0].position : Vector3.zero;
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            DrawGroupGizmos(floor1PointsRoot, floor1Points, Color.cyan);
            DrawGroupGizmos(floor2PointsRoot, floor2Points, new Color(1f, 0.72f, 0.15f, 1f));
        }

        private static void DrawGroupGizmos(Transform root, Transform[] explicitList, Color color)
        {
            Gizmos.color = color;
            if (root != null && root.childCount > 0)
            {
                for (int i = 0; i < root.childCount; i++)
                {
                    Transform c = root.GetChild(i);
                    if (c != null) Gizmos.DrawSphere(c.position + Vector3.up * 0.25f, 0.3f);
                }
                return;
            }

            if (explicitList == null) return;
            for (int i = 0; i < explicitList.Length; i++)
            {
                if (explicitList[i] != null) Gizmos.DrawSphere(explicitList[i].position + Vector3.up * 0.25f, 0.3f);
            }
        }
    }
}
