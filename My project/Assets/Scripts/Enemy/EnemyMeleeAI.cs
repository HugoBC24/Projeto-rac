using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using ProjectRac.Combat;
using ProjectRac.Player;

namespace ProjectRac.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyMeleeAI : MonoBehaviour
    {
        public enum AIState
        {
            Patrol,
            Chase,
            Attack
        }

        [Header("State Settings")]
        [SerializeField] private AIState currentState = AIState.Patrol;
        [SerializeField] private float detectionRadius = 14.0f;
        [SerializeField] private float loseTargetRadius = 22.0f;
        [SerializeField] private float patrolRadius = 12.0f;
        [SerializeField] private float patrolWaitTime = 2.0f;

        [Header("Speeds")]
        [SerializeField] private float patrolSpeed = 3.2f;
        [SerializeField] private float chaseSpeed = 5.8f;

        [Header("Melee Attack")]
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackDamage = 20.0f;
        [SerializeField] private float attackCooldown = 1.2f;

        [Header("Target & Model")]
        [SerializeField] private Transform targetPlayer;
        [SerializeField] private GameObject customEnemyPrefab;

        public AIState CurrentState => currentState;

        private NavMeshAgent _agent;
        private EnemyHealth _health;
        private PlayerHealth _playerHealth;
        private Vector3 _spawnOrigin;
        private float _stateTimer;
        private float _lastAttackTime = -999f;
        private bool _isAttacking;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<EnemyHealth>();
            _spawnOrigin = transform.position;

            _agent.speed = patrolSpeed;
            _agent.stoppingDistance = attackRange * 0.85f;
            _agent.autoBraking = true;
            _agent.acceleration = 14f;
            _agent.angularSpeed = 280f;
        }

        private void Start()
        {
            ResolveTarget();
            BuildProceduralVisualsIfNeeded();

            _health.OnDamageTaken += HandleDamageTaken;
            _health.OnDeath += HandleDeath;

            TransitionToState(AIState.Patrol);
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDamageTaken -= HandleDamageTaken;
                _health.OnDeath -= HandleDeath;
            }
        }

        private void Update()
        {
            if (_health != null && _health.IsDead) return;

            if (targetPlayer == null)
            {
                ResolveTarget();
                if (targetPlayer == null) return;
            }

            float distToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

            switch (currentState)
            {
                case AIState.Patrol:
                    UpdatePatrol(distToPlayer);
                    break;
                case AIState.Chase:
                    UpdateChase(distToPlayer);
                    break;
                case AIState.Attack:
                    UpdateAttack(distToPlayer);
                    break;
            }
        }

        private void UpdatePatrol(float distToPlayer)
        {
            if (distToPlayer <= detectionRadius)
            {
                TransitionToState(AIState.Chase);
                return;
            }

            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.3f)
            {
                _stateTimer += Time.deltaTime;
                if (_stateTimer >= patrolWaitTime)
                {
                    _stateTimer = 0f;
                    SetRandomPatrolDestination();
                }
            }
        }

        private void UpdateChase(float distToPlayer)
        {
            if (distToPlayer > loseTargetRadius)
            {
                TransitionToState(AIState.Patrol);
                return;
            }

            if (distToPlayer <= attackRange)
            {
                TransitionToState(AIState.Attack);
                return;
            }

            _agent.isStopped = false;
            _agent.SetDestination(targetPlayer.position);
        }

        private void UpdateAttack(float distToPlayer)
        {
            if (distToPlayer > attackRange * 1.25f && !_isAttacking)
            {
                TransitionToState(AIState.Chase);
                return;
            }

            _agent.isStopped = true;

            // Rotate smoothly towards player while in attack stance
            Vector3 lookDir = (targetPlayer.position - transform.position);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
            }

            if (Time.time >= _lastAttackTime + attackCooldown)
            {
                StartCoroutine(PerformAttackRoutine());
            }
        }

        private IEnumerator PerformAttackRoutine()
        {
            _isAttacking = true;
            _lastAttackTime = Time.time;

            // Short windup before strike
            yield return new WaitForSeconds(0.25f);

            if (_health != null && !_health.IsDead && targetPlayer != null)
            {
                float currentDist = Vector3.Distance(transform.position, targetPlayer.position);
                if (currentDist <= attackRange * 1.3f)
                {
                    if (_playerHealth != null)
                    {
                        Vector3 strikeDir = (targetPlayer.position - transform.position).normalized;
                        _playerHealth.TakeDamage(attackDamage, targetPlayer.position, strikeDir);
                    }
                }
            }

            yield return new WaitForSeconds(0.2f);
            _isAttacking = false;
        }

        private void TransitionToState(AIState newState)
        {
            currentState = newState;
            _stateTimer = 0f;

            if (currentState == AIState.Patrol)
            {
                _agent.speed = patrolSpeed;
                _agent.isStopped = false;
                SetRandomPatrolDestination();
            }
            else if (currentState == AIState.Chase)
            {
                _agent.speed = chaseSpeed;
                _agent.isStopped = false;
                if (targetPlayer != null) _agent.SetDestination(targetPlayer.position);
            }
            else if (currentState == AIState.Attack)
            {
                _agent.isStopped = true;
            }
        }

        private void SetRandomPatrolDestination()
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            Vector3 targetPos = _spawnOrigin + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
            }
        }

        private void HandleDamageTaken(Vector3 hitPoint)
        {
            if (currentState == AIState.Patrol)
            {
                TransitionToState(AIState.Chase);
            }
        }

        private void HandleDeath()
        {
            StopAllCoroutines();
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.enabled = false;
            }
            enabled = false;
        }

        private void ResolveTarget()
        {
            if (targetPlayer == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    targetPlayer = playerObj.transform;
                    _playerHealth = playerObj.GetComponent<PlayerHealth>();
                }
            }
        }

        private void BuildProceduralVisualsIfNeeded()
        {
            if (customEnemyPrefab != null)
            {
                Instantiate(customEnemyPrefab, transform);
                return;
            }

            if (transform.Find("EnemyMesh") != null) return;

            // Generate procedural cyber-melee model
            GameObject meshRoot = new GameObject("EnemyMesh");
            meshRoot.transform.SetParent(transform, false);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(meshRoot.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            body.transform.localScale = new Vector3(0.75f, 1.0f, 0.75f);
            Destroy(body.GetComponent<Collider>());

            var rend = body.GetComponent<Renderer>();
            if (rend != null)
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", new Color(0.18f, 0.18f, 0.22f));
                rend.SetPropertyBlock(block);
            }

            // Visor
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Visor";
            visor.transform.SetParent(meshRoot.transform, false);
            visor.transform.localPosition = new Vector3(0f, 1.45f, 0.32f);
            visor.transform.localScale = new Vector3(0.45f, 0.15f, 0.2f);
            Destroy(visor.GetComponent<Collider>());

            var visorRend = visor.GetComponent<Renderer>();
            if (visorRend != null)
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", new Color(0.95f, 0.1f, 0.1f));
                visorRend.SetPropertyBlock(block);
            }
        }
    }
}
