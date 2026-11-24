using UnityEngine;
using UnityEngine.AI;

namespace ProjectRac.Enemy
{
    public static class EnemyPerception
    {
        private static readonly float[] RetreatAngles = { 0f, 45f, -45f, 90f, -90f };

        public static bool HasLineOfSight(Transform observer, Transform target, LayerMask obstructionLayers)
        {
            if (observer == null || target == null) return false;

            // Cast to both chest and head so low cover does not cause false-negative sight checks.
            Vector3 eyeOrigin = observer.position + Vector3.up * 1.45f;
            return CanSeePoint(observer, target, eyeOrigin, target.position + Vector3.up * 1.15f, obstructionLayers)
                || CanSeePoint(observer, target, eyeOrigin, target.position + Vector3.up * 1.65f, obstructionLayers);
        }

        private static bool CanSeePoint(
            Transform observer,
            Transform target,
            Vector3 origin,
            Vector3 targetPoint,
            LayerMask obstructionLayers)
        {
            Vector3 toTarget = targetPoint - origin;
            float dist = toTarget.magnitude;
            if (dist <= 0.05f) return true;

            RaycastHit[] hits = Physics.RaycastAll(origin, toTarget / dist, dist, obstructionLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Transform hitT = hits[i].transform;
                if (hitT == observer || hitT.IsChildOf(observer)) continue;
                return hitT == target || hitT.IsChildOf(target) || hits[i].collider.CompareTag("Player");
            }

            return true;
        }

        public static bool TrySamplePatrolPoint(Vector3 origin, float radius, out Vector3 result)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 candidate = origin + new Vector3(randomCircle.x, 0f, randomCircle.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }

            result = origin;
            return false;
        }

        public static bool TrySampleRetreatPoint(Transform observer, Transform threat, float stepDistance, out Vector3 result)
        {
            Vector3 away = observer.position - threat.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -observer.forward;
            away.Normalize();

            float currentDistSq = (observer.position - threat.position).sqrMagnitude;
            for (int i = 0; i < RetreatAngles.Length; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, RetreatAngles[i], 0f) * away;
                Vector3 candidate = observer.position + dir * stepDistance;
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                {
                    if ((hit.position - threat.position).sqrMagnitude > currentDistSq + 1.5f)
                    {
                        result = hit.position;
                        return true;
                    }
                }
            }

            result = observer.position;
            return false;
        }
    }
}
