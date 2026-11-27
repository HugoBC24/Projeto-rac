using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using ProjectRac.Player;

namespace ProjectRac.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyMeleeAI : MonoBehaviour
    {
        public enum AIState { Idle, Patrol, Chase, Attack }

        [Header("State Settings")]
        [SerializeField] private AIState currentState = AIState.Patrol;
        [SerializeField] private bool startInIdle = false;
        [SerializeField] private bool relentlessChase = false;
        [SerializeField] private float detectionRadius = 14.0f;
        [SerializeField] private float loseTargetRadius = 22.0f;
        [SerializeField] private float patrolRadius = 12.0f;
        [SerializeField] private float patrolWaitTime = 2.0f;

        [Header("Speeds & Melee Attack")]
        [SerializeField] private float patrolSpeed = 2.6f;
        [SerializeField] private float chaseSpeed = 5.4f;
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackDamage = 20.0f;
        [SerializeField] private float attackCooldown = 1.2f;

        [Header("Target & Model")]
        [SerializeField] private Transform targetPlayer;
        [SerializeField] private GameObject customEnemyPrefab;

        public AIState CurrentState => currentState;
        public bool IsAmbushHeld => _ambushHeld;

        private NavMeshAgent _agent;
        private EnemyHealth _health;
        private EnemyMeleeAnimator _meleeAnimator;
        private PlayerHealth _playerHealth;
        private Vector3 _spawnOrigin;
        private float _stateTimer, _lastAttackTime = -999f;
        private bool _isAttacking, _ambushHeld, _initialized;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<EnemyHealth>();
            _meleeAnimator = GetComponent<EnemyMeleeAnimator>();
            _spawnOrigin = transform.position;

            _agent.speed = patrolSpeed;
            _agent.stoppingDistance = attackRange * 0.85f;
            _agent.autoBraking = true;
            _agent.acceleration = 14f;
            _agent.angularSpeed = 300f;
        }

        private void Start()
        {
            ResolveTarget();
            EnsureVisuals();
            _health.OnDamageTaken += HandleDamageTaken;
            _health.OnDeath += HandleDeath;

            if (!_initialized)
            {
                _initialized = true;
                TransitionToState(startInIdle || _ambushHeld ? AIState.Idle : AIState.Patrol);
            }
        }

        private void OnDestroy()
        {
            if (_health == null) return;
            _health.OnDamageTaken -= HandleDamageTaken;
            _health.OnDeath -= HandleDeath;
        }

        public void SetCustomEnemyPrefab(GameObject prefab) => customEnemyPrefab = prefab;

        public void EnterAmbushIdle(Transform lookTarget)
        {
            _initialized = true;
            _ambushHeld = true;
            _health?.SetInvulnerable(true);
            BindTarget(lookTarget);
            TransitionToState(AIState.Idle);
            FaceTarget(targetPlayer, immediate: true);
            _meleeAnimator?.ResolveAnimator();
        }

        public void StartRelentlessChase(Transform target = null)
        {
            _initialized = true;
            _ambushHeld = false;
            relentlessChase = true;
            _health?.SetInvulnerable(false);
            if (target != null) BindTarget(target);
            else ResolveTarget();
            TransitionToState(AIState.Chase);
        }

        public void ConfigureAsRoamingRespawn()
        {
            _initialized = true;
            _ambushHeld = false;
            startInIdle = false;
            relentlessChase = false;
            _health?.SetInvulnerable(false);
            ResolveTarget();
            TransitionToState(AIState.Patrol);
        }

        private void Update()
        {
            if (_health != null && _health.IsDead) return;
            if (targetPlayer == null) { ResolveTarget(); if (targetPlayer == null) return; }

            float dist = Vector3.Distance(transform.position, targetPlayer.position);
            switch (currentState)
            {
                case AIState.Idle:
                    if (_agent.isOnNavMesh) _agent.isStopped = true;
                    if (_ambushHeld) FaceTarget(targetPlayer, false, 8f);
                    break;
                case AIState.Patrol: UpdatePatrol(dist); break;
                case AIState.Chase: UpdateChase(dist); break;
                case AIState.Attack: UpdateAttack(dist); break;
            }
        }

        private void UpdatePatrol(float distToPlayer)
        {
            if (distToPlayer <= detectionRadius) { TransitionToState(AIState.Chase); return; }
            if (!_agent.isOnNavMesh || _agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance + 0.3f) return;

            _stateTimer += Time.deltaTime;
            if (_stateTimer >= patrolWaitTime) { _stateTimer = 0f; SetRandomPatrolDestination(); }
        }

        private void UpdateChase(float distToPlayer)
        {
            if (!relentlessChase && distToPlayer > loseTargetRadius) { TransitionToState(AIState.Patrol); return; }
            if (distToPlayer <= attackRange) { TransitionToState(AIState.Attack); return; }
            if (_agent.isOnNavMesh) { _agent.isStopped = false; _agent.SetDestination(targetPlayer.position); }
        }

        private void UpdateAttack(float distToPlayer)
        {
            if (distToPlayer > attackRange * 1.25f && !_isAttacking) { TransitionToState(AIState.Chase); return; }
            if (_agent.isOnNavMesh) _agent.isStopped = true;
            FaceTarget(targetPlayer, false, 10f);
            if (Time.time >= _lastAttackTime + attackCooldown) StartCoroutine(PerformAttackRoutine());
        }

        private IEnumerator PerformAttackRoutine()
        {
            _isAttacking = true;
            _lastAttackTime = Time.time;
            yield return new WaitForSeconds(0.25f);

            if (_health != null && !_health.IsDead && targetPlayer != null)
            {
                if (Vector3.Distance(transform.position, targetPlayer.position) <= attackRange * 1.3f && _playerHealth != null)
                {
                    Vector3 strikeDir = (targetPlayer.position - transform.position).normalized;
                    _playerHealth.TakeDamage(attackDamage, targetPlayer.position, strikeDir);
                }
            }

            yield return new WaitForSeconds(0.2f);
            _isAttacking = false;
        }

        public void TransitionToState(AIState newState)
        {
            currentState = newState;
            _stateTimer = 0f;
            if (_agent == null || !_agent.isOnNavMesh) return;

            if (currentState == AIState.Idle || currentState == AIState.Attack)
            {
                _agent.isStopped = true;
            }
            else if (currentState == AIState.Patrol)
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
        }

        private void SetRandomPatrolDestination()
        {
            if (MansionPatrolNetwork.Instance != null
                && MansionPatrolNetwork.Instance.TryGetRandomWaypoint(transform.position, out Vector3 mansionPoint))
            {
                _agent.SetDestination(mansionPoint);
                return;
            }

            Vector2 c = Random.insideUnitCircle * patrolRadius;
            Vector3 targetPos = _spawnOrigin + new Vector3(c.x, 0f, c.y);
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
            }
        }

        private void HandleDamageTaken(Vector3 hitPoint)
        {
            if (!_ambushHeld && (currentState == AIState.Patrol || currentState == AIState.Idle))
            {
                TransitionToState(AIState.Chase);
            }
        }

        private void HandleDeath()
        {
            StopAllCoroutines();
            if (_agent != null && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.enabled = false; }
            enabled = false;
        }

        private void FaceTarget(Transform target, bool immediate, float turnSpeed = 8f)
        {
            if (target == null) return;
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude <= 0.001f) return;
            Quaternion rot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = immediate ? rot : Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * turnSpeed);
        }

        private void BindTarget(Transform target)
        {
            if (target == null) return;
            targetPlayer = target;
            _playerHealth = target.GetComponent<PlayerHealth>();
        }

        private void ResolveTarget()
        {
            if (targetPlayer != null) return;
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) BindTarget(playerObj.transform);
        }

        private void EnsureVisuals()
        {
            if (GetComponentInChildren<Renderer>(true) != null) return;
            if (customEnemyPrefab != null)
            {
                Instantiate(customEnemyPrefab, transform);
                _health?.RefreshRenderers();
                _meleeAnimator?.ResolveAnimator();
                return;
            }

            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = "EnemyMesh";
            fallback.transform.SetParent(transform, false);
            fallback.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            Destroy(fallback.GetComponent<Collider>());
        }
    }
}
