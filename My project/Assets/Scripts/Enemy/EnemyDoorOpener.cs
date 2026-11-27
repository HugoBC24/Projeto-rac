using UnityEngine;
using UnityEngine.AI;
using ProjectRac.Interaction;

namespace ProjectRac.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyDoorOpener : MonoBehaviour
    {
        [Header("Door Detection")]
        [SerializeField] private float checkInterval = 0.15f;
        [SerializeField] private float detectRadius = 1.25f;
        [SerializeField] private float forwardOffset = 0.85f;
        [SerializeField] private float chestHeight = 1.0f;

        private NavMeshAgent _agent;
        private EnemyHealth _health;
        private float _nextCheckTime;
        private readonly Collider[] _overlapBuffer = new Collider[12];

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<EnemyHealth>();
        }

        private void Update()
        {
            if (_health != null && _health.IsDead) return;
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh || _agent.isStopped) return;
            if (!_agent.hasPath && _agent.velocity.sqrMagnitude < 0.04f) return;

            if (Time.time < _nextCheckTime) return;
            _nextCheckTime = Time.time + checkInterval;

            TryOpenNearbyDoors();
        }

        private void TryOpenNearbyDoors()
        {
            Vector3 probeCenter = transform.position + Vector3.up * chestHeight + transform.forward * forwardOffset;
            int count = Physics.OverlapSphereNonAlloc(probeCenter, detectRadius, _overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider col = _overlapBuffer[i];
                if (col == null) continue;

                DoorController door = col.GetComponent<DoorController>();
                if (door == null) door = col.GetComponentInParent<DoorController>();
                if (door != null && door.AllowEnemyOpen && !door.IsOpen)
                {
                    door.Open(transform);
                }
            }
        }
    }
}
