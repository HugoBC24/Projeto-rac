using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectRac.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyRangedAI : MonoBehaviour
    {
        public enum RangedAIState { Patrol, Chase, Attack, Retreat }

        [Header("Perception & Ranges")]
        [SerializeField] private RangedAIState currentState = RangedAIState.Patrol;
        [SerializeField] private float detectionRadius = 20f;
        [SerializeField] private float loseTargetRadius = 28f;
        [SerializeField] private float attackRange = 15f;
        [SerializeField] private float attackExitRange = 18f;
        [SerializeField] private float retreatRange = 5.5f;
        [SerializeField] private float retreatSafeRange = 8.0f;
        [SerializeField] private float patrolRadius = 10f;
        [SerializeField] private float patrolWaitTime = 2.2f;
        [SerializeField] private float perceptionInterval = 0.15f;
        [SerializeField] private LayerMask losObstructionLayers = ~0;

        [Header("Speeds & Repath")]
        [SerializeField] private float patrolSpeed = 2.8f;
        [SerializeField] private float chaseSpeed = 4.6f;
        [SerializeField] private float retreatSpeed = 4.2f;
        [SerializeField] private float repathInterval = 0.25f;

        [Header("Ranged Attack")]
        [SerializeField] private float projectileDamage = 15f;
        [SerializeField] private float projectileSpeed = 24f;
        [SerializeField] private float attackWindup = 0.35f;
        [SerializeField] private float fireCooldown = 1.6f;

        [Header("References")]
        [SerializeField] private Transform targetPlayer;
        [SerializeField] private EnemyProjectilePool projectilePool;
        [SerializeField] private GameObject customEnemyPrefab;

        public RangedAIState CurrentState => currentState;
        public bool HasLineOfSight => _hasLineOfSight;
        public Transform FirePoint => _firePoint;

        private NavMeshAgent _agent;
        private EnemyHealth _health;
        private Transform _firePoint;
        private Renderer _muzzleRenderer;
        private MaterialPropertyBlock _muzzleBlock;
        private Vector3 _spawnOrigin;
        private Vector3 _lastKnownPlayerPos;
        private bool _hasLineOfSight;
        private bool _hasSeenPlayer;
        private bool _isFiring;
        private float _stateTimer;
        private float _nextPerceptionTime;
        private float _nextRepathTime;
        private float _lastFireTime = -999f;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<EnemyHealth>();
            _muzzleBlock = new MaterialPropertyBlock();
            _spawnOrigin = transform.position;
            _agent.speed = patrolSpeed;
            _agent.autoBraking = true;
            _agent.acceleration = 12f;
            _agent.angularSpeed = 300f;
        }

        private void Start()
        {
            ResolveReferences();
            var handles = EnemyRangedVisuals.EnsureVisuals(transform, customEnemyPrefab);
            _firePoint = handles.FirePoint;
            _muzzleRenderer = handles.MuzzleRenderer;
            _health.RefreshRenderers();
            _health.OnDamageTaken += HandleDamageTaken;
            _health.OnDeath += HandleDeath;
            TransitionToState(RangedAIState.Patrol);
        }

        private void OnDestroy()
        {
            if (_health == null) return;
            _health.OnDamageTaken -= HandleDamageTaken;
            _health.OnDeath -= HandleDeath;
        }

        public void BindProjectilePool(EnemyProjectilePool pool) => projectilePool = pool;

        private void Update()
        {
            if (_health != null && _health.IsDead) return;
            if (targetPlayer == null) { ResolveReferences(); if (targetPlayer == null) return; }

            float distToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
            if (Time.time >= _nextPerceptionTime)
            {
                _nextPerceptionTime = Time.time + perceptionInterval;
                _hasLineOfSight = distToPlayer <= loseTargetRadius
                    && EnemyPerception.HasLineOfSight(transform, targetPlayer, losObstructionLayers);
                if (_hasLineOfSight) { _hasSeenPlayer = true; _lastKnownPlayerPos = targetPlayer.position; }
            }

            switch (currentState)
            {
                case RangedAIState.Patrol: UpdatePatrol(distToPlayer); break;
                case RangedAIState.Chase: UpdateChase(distToPlayer); break;
                case RangedAIState.Attack: UpdateAttack(distToPlayer); break;
                case RangedAIState.Retreat: UpdateRetreat(distToPlayer); break;
            }
        }

        private void UpdatePatrol(float distToPlayer)
        {
            if (distToPlayer <= detectionRadius && _hasLineOfSight) { TransitionToState(RangedAIState.Chase); return; }

            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.35f)
            {
                _stateTimer += Time.deltaTime;
                if (_stateTimer >= patrolWaitTime)
                {
                    _stateTimer = 0f;
                    if (EnemyPerception.TrySamplePatrolPoint(_spawnOrigin, patrolRadius, out Vector3 pt))
                        _agent.SetDestination(pt);
                }
            }
        }

        private void UpdateChase(float distToPlayer)
        {
            if (distToPlayer > loseTargetRadius && !_hasLineOfSight)
            {
                _hasSeenPlayer = false;
                TransitionToState(RangedAIState.Patrol);
                return;
            }

            if (_hasLineOfSight && distToPlayer <= attackRange)
            {
                TransitionToState(distToPlayer < retreatRange ? RangedAIState.Retreat : RangedAIState.Attack);
                return;
            }

            if (Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + repathInterval;
                _agent.isStopped = false;
                _agent.SetDestination(_hasSeenPlayer ? _lastKnownPlayerPos : targetPlayer.position);
            }

            if (!_hasLineOfSight && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.6f)
            {
                _stateTimer += Time.deltaTime;
                if (_stateTimer >= 2.0f) { _hasSeenPlayer = false; TransitionToState(RangedAIState.Patrol); }
            }
        }

        private void UpdateAttack(float distToPlayer)
        {
            if (!_isFiring)
            {
                if (!_hasLineOfSight || distToPlayer > attackExitRange) { TransitionToState(RangedAIState.Chase); return; }
                if (distToPlayer < retreatRange) { TransitionToState(RangedAIState.Retreat); return; }
            }

            _agent.isStopped = true;
            RotateTowardPlayer();

            if (!_isFiring && _hasLineOfSight && Time.time >= _lastFireTime + fireCooldown)
                StartCoroutine(PerformRangedAttackRoutine());
        }

        private void UpdateRetreat(float distToPlayer)
        {
            if (!_hasLineOfSight) { TransitionToState(RangedAIState.Chase); return; }
            if (distToPlayer >= retreatSafeRange) { TransitionToState(RangedAIState.Attack); return; }

            if (Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + repathInterval;
                if (EnemyPerception.TrySampleRetreatPoint(transform, targetPlayer, 6f, out Vector3 retreatPos))
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(retreatPos);
                }
                else
                {
                    // Cornered with no walkable fallback: stand ground and fight back instead of stalling.
                    TransitionToState(RangedAIState.Attack);
                }
            }
        }

        private IEnumerator PerformRangedAttackRoutine()
        {
            _isFiring = true;
            EnemyRangedVisuals.SetMuzzleColor(_muzzleRenderer, _muzzleBlock, EnemyRangedVisuals.ChargingMuzzleColor);
            yield return new WaitForSeconds(attackWindup);

            if (_health != null && !_health.IsDead && targetPlayer != null
                && EnemyPerception.HasLineOfSight(transform, targetPlayer, losObstructionLayers))
            {
                FireProjectile();
                _lastFireTime = Time.time;
            }

            EnemyRangedVisuals.SetMuzzleColor(_muzzleRenderer, _muzzleBlock, EnemyRangedVisuals.IdleMuzzleColor);
            _isFiring = false;
        }

        private void FireProjectile()
        {
            if (projectilePool == null) projectilePool = FindAnyObjectByType<EnemyProjectilePool>();
            if (projectilePool == null) return;

            Vector3 spawnOrigin = _firePoint != null
                ? _firePoint.position
                : transform.position + transform.forward * 0.85f + Vector3.up * 1.25f;

            Vector3 direction = ((targetPlayer.position + Vector3.up * 1.15f) - spawnOrigin).normalized;
            if (direction == Vector3.zero) direction = transform.forward;
            projectilePool.SpawnProjectile(spawnOrigin, direction, projectileDamage, projectileSpeed, transform);
        }

        private void RotateTowardPlayer()
        {
            Vector3 lookDir = targetPlayer.position - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
        }

        private void TransitionToState(RangedAIState newState)
        {
            currentState = newState;
            _stateTimer = 0f;
            _nextRepathTime = 0f;

            if (currentState == RangedAIState.Patrol)
            {
                _agent.speed = patrolSpeed; _agent.stoppingDistance = 0.5f; _agent.isStopped = false;
                if (EnemyPerception.TrySamplePatrolPoint(_spawnOrigin, patrolRadius, out Vector3 pt)) _agent.SetDestination(pt);
            }
            else if (currentState == RangedAIState.Chase)
            {
                _agent.speed = chaseSpeed; _agent.stoppingDistance = 1.0f; _agent.isStopped = false;
            }
            else if (currentState == RangedAIState.Attack)
            {
                _agent.isStopped = true;
            }
            else if (currentState == RangedAIState.Retreat)
            {
                _agent.speed = retreatSpeed; _agent.stoppingDistance = 0.4f; _agent.isStopped = false;
                if (EnemyPerception.TrySampleRetreatPoint(transform, targetPlayer, 6f, out Vector3 rp)) _agent.SetDestination(rp);
            }
        }

        private void HandleDamageTaken(Vector3 hitPoint)
        {
            if (targetPlayer != null) { _hasSeenPlayer = true; _lastKnownPlayerPos = targetPlayer.position; }
            if (currentState == RangedAIState.Patrol) TransitionToState(RangedAIState.Chase);
        }

        private void HandleDeath()
        {
            StopAllCoroutines();
            _isFiring = false;
            if (_agent != null && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.enabled = false; }
            enabled = false;
        }

        private void ResolveReferences()
        {
            if (targetPlayer == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) targetPlayer = playerObj.transform;
            }

            if (projectilePool == null) projectilePool = FindAnyObjectByType<EnemyProjectilePool>();
        }
    }
}
