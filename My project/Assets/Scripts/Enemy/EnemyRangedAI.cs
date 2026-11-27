using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectRac.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyRangedAI : MonoBehaviour
    {
        public enum RangedAIState { Idle, Patrol, Chase, Attack, Retreat }

        [Header("Perception & Ranges")]
        [SerializeField] private RangedAIState currentState = RangedAIState.Patrol;
        [SerializeField] private bool startInIdle = false;
        [SerializeField] private bool relentlessChase = false;
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
        [SerializeField] private float patrolSpeed = 2.6f;
        [SerializeField] private float chaseSpeed = 4.2f;
        [SerializeField] private float retreatSpeed = 3.8f;
        [SerializeField] private float repathInterval = 0.25f;

        [Header("Ranged Attack")]
        [SerializeField] private float projectileDamage = 15f;
        [SerializeField] private float projectileSpeed = 24f;
        [SerializeField] private float attackWindup = 0.45f;
        [SerializeField] private float fireCooldown = 1.6f;

        [Header("References")]
        [SerializeField] private Transform targetPlayer;
        [SerializeField] private EnemyProjectilePool projectilePool;
        [SerializeField] private GameObject customEnemyPrefab;
        [SerializeField] private Transform firePointOverride;

        public RangedAIState CurrentState => currentState;
        public bool HasLineOfSight => _hasLineOfSight;
        public bool IsFiring => _isFiring;
        public float AttackCycleDuration => Mathf.Max(0.35f, fireCooldown);
        public Transform FirePoint => _firePoint;
        public event Action OnAttackStarted;

        private NavMeshAgent _agent;
        private EnemyHealth _health;
        private Transform _firePoint;
        private Renderer _muzzleRenderer;
        private MaterialPropertyBlock _muzzleBlock;
        private Vector3 _spawnOrigin, _lastKnownPlayerPos;
        private bool _hasLineOfSight, _hasSeenPlayer, _isFiring, _encounterHeld, _initialized;
        private float _stateTimer, _nextPerceptionTime, _nextRepathTime, _lastFireTime = -999f;

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
            if (GetComponentInChildren<Renderer>(true) == null)
            {
                var handles = EnemyRangedVisuals.EnsureVisuals(transform, customEnemyPrefab);
                _firePoint = handles.FirePoint;
                _muzzleRenderer = handles.MuzzleRenderer;
            }
            else
            {
                _firePoint = firePointOverride != null ? firePointOverride : transform.Find("FirePoint");
            }

            _health.RefreshRenderers();
            _health.OnDamageTaken += HandleDamageTaken;
            _health.OnDeath += HandleDeath;

            if (!_initialized)
            {
                _initialized = true;
                _encounterHeld = startInIdle;
                if (startInIdle) _health?.SetInvulnerable(true);
                TransitionToState(startInIdle ? RangedAIState.Idle : RangedAIState.Patrol);
            }
        }

        private void OnDestroy()
        {
            if (_health == null) return;
            _health.OnDamageTaken -= HandleDamageTaken;
            _health.OnDeath -= HandleDeath;
        }

        public void BindProjectilePool(EnemyProjectilePool pool) => projectilePool = pool;
        public void SetStartInIdle(bool idle) => startInIdle = idle;

        public void EnterEncounterIdle(Transform lookTarget = null)
        {
            _initialized = true;
            _encounterHeld = true;
            _health?.SetInvulnerable(true);
            if (lookTarget != null) targetPlayer = lookTarget;
            TransitionToState(RangedAIState.Idle);
        }

        public void StartRelentlessChase(Transform target = null)
        {
            _initialized = true;
            _encounterHeld = false;
            relentlessChase = true;
            _health?.SetInvulnerable(false);
            if (target != null) targetPlayer = target;
            else ResolveReferences();
            if (targetPlayer != null) { _hasSeenPlayer = true; _lastKnownPlayerPos = targetPlayer.position; }
            TransitionToState(RangedAIState.Chase);
        }

        public void ConfigureAsRoamingRespawn()
        {
            _initialized = true;
            _encounterHeld = false;
            startInIdle = false;
            relentlessChase = false;
            _health?.SetInvulnerable(false);
            ResolveReferences();
            TransitionToState(RangedAIState.Patrol);
        }

        private void Update()
        {
            if (_health != null && _health.IsDead) return;
            if (targetPlayer == null) { ResolveReferences(); if (targetPlayer == null) return; }

            float dist = Vector3.Distance(transform.position, targetPlayer.position);
            if (!_encounterHeld && Time.time >= _nextPerceptionTime)
            {
                _nextPerceptionTime = Time.time + perceptionInterval;
                _hasLineOfSight = dist <= loseTargetRadius && EnemyPerception.HasLineOfSight(transform, targetPlayer, losObstructionLayers);
                if (_hasLineOfSight) { _hasSeenPlayer = true; _lastKnownPlayerPos = targetPlayer.position; }
            }

            switch (currentState)
            {
                case RangedAIState.Idle:
                    if (_agent.isOnNavMesh) _agent.isStopped = true;
                    if (_encounterHeld && targetPlayer != null && Vector3.Distance(transform.position, targetPlayer.position) < 12f)
                        RotateTowardPlayer(5f);
                    break;
                case RangedAIState.Patrol: UpdatePatrol(dist); break;
                case RangedAIState.Chase: UpdateChase(dist); break;
                case RangedAIState.Attack: UpdateAttack(dist); break;
                case RangedAIState.Retreat: UpdateRetreat(dist); break;
            }
        }

        private void UpdatePatrol(float dist)
        {
            if (dist <= detectionRadius && _hasLineOfSight) { TransitionToState(RangedAIState.Chase); return; }
            if (!_agent.isOnNavMesh || _agent.pathPending) return;
            if (!_agent.hasPath) { SetRandomPatrolDestination(); return; }
            if (_agent.remainingDistance > _agent.stoppingDistance + 0.35f) return;

            _stateTimer += Time.deltaTime;
            if (_stateTimer >= patrolWaitTime)
            {
                _stateTimer = 0f;
                SetRandomPatrolDestination();
            }
        }

        private void UpdateChase(float dist)
        {
            if (!relentlessChase && dist > loseTargetRadius && !_hasLineOfSight)
            {
                _hasSeenPlayer = false;
                TransitionToState(RangedAIState.Patrol);
                return;
            }

            if (_hasLineOfSight && dist <= attackRange)
            {
                TransitionToState(dist < retreatRange ? RangedAIState.Retreat : RangedAIState.Attack);
                return;
            }

            if (_agent.isOnNavMesh && Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + repathInterval;
                _agent.isStopped = false;
                _agent.SetDestination(relentlessChase ? targetPlayer.position : (_hasSeenPlayer ? _lastKnownPlayerPos : targetPlayer.position));
            }
        }

        private void UpdateAttack(float dist)
        {
            if (!_isFiring)
            {
                if (!_hasLineOfSight || dist > attackExitRange) { TransitionToState(RangedAIState.Chase); return; }
                if (dist < retreatRange) { TransitionToState(RangedAIState.Retreat); return; }
            }

            if (_agent.isOnNavMesh) _agent.isStopped = true;
            RotateTowardPlayer(10f);
            if (!_isFiring && _hasLineOfSight && Time.time >= _lastFireTime + fireCooldown)
                StartCoroutine(PerformRangedAttackRoutine());
        }

        private void UpdateRetreat(float dist)
        {
            if (!_hasLineOfSight) { TransitionToState(RangedAIState.Chase); return; }
            if (dist >= retreatSafeRange) { TransitionToState(RangedAIState.Attack); return; }

            if (_agent.isOnNavMesh && Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + repathInterval;
                if (EnemyPerception.TrySampleRetreatPoint(transform, targetPlayer, 6f, out Vector3 rp))
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(rp);
                }
                else TransitionToState(RangedAIState.Attack);
            }
        }

        private IEnumerator PerformRangedAttackRoutine()
        {
            _isFiring = true;
            _lastFireTime = Time.time;
            OnAttackStarted?.Invoke();

            float cycle = AttackCycleDuration;
            float windup = Mathf.Clamp(attackWindup, 0.1f, cycle * 0.75f);
            float recovery = Mathf.Max(0f, cycle - windup);

            EnemyRangedVisuals.SetMuzzleColor(_muzzleRenderer, _muzzleBlock, EnemyRangedVisuals.ChargingMuzzleColor);
            yield return new WaitForSeconds(windup);

            if (_health != null && !_health.IsDead && targetPlayer != null
                && EnemyPerception.HasLineOfSight(transform, targetPlayer, losObstructionLayers))
            {
                FireProjectile();
            }

            EnemyRangedVisuals.SetMuzzleColor(_muzzleRenderer, _muzzleBlock, EnemyRangedVisuals.IdleMuzzleColor);
            if (recovery > 0.01f) yield return new WaitForSeconds(recovery);
            _isFiring = false;
        }

        private void FireProjectile()
        {
            if (projectilePool == null) projectilePool = FindAnyObjectByType<EnemyProjectilePool>();
            if (projectilePool == null) return;

            Vector3 origin = _firePoint != null ? _firePoint.position : transform.position + transform.forward * 0.85f + Vector3.up * 1.35f;
            Vector3 dir = ((targetPlayer.position + Vector3.up * 1.15f) - origin).normalized;
            if (dir == Vector3.zero) dir = transform.forward;
            projectilePool.SpawnProjectile(origin, dir, projectileDamage, projectileSpeed, transform);
        }

        private void RotateTowardPlayer(float speed)
        {
            Vector3 lookDir = targetPlayer.position - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * speed);
        }

        public void TransitionToState(RangedAIState newState)
        {
            currentState = newState;
            _stateTimer = 0f;
            _nextRepathTime = 0f;
            if (_agent == null || !_agent.isOnNavMesh) return;

            if (currentState == RangedAIState.Idle || currentState == RangedAIState.Attack) _agent.isStopped = true;
            else if (currentState == RangedAIState.Patrol)
            {
                _agent.speed = patrolSpeed; _agent.stoppingDistance = 0.5f; _agent.isStopped = false;
                SetRandomPatrolDestination();
            }
            else if (currentState == RangedAIState.Chase)
            {
                _agent.speed = chaseSpeed; _agent.stoppingDistance = 1.0f; _agent.isStopped = false;
                if (targetPlayer != null) _agent.SetDestination(targetPlayer.position);
            }
            else if (currentState == RangedAIState.Retreat)
            {
                _agent.speed = retreatSpeed; _agent.stoppingDistance = 0.4f; _agent.isStopped = false;
                if (EnemyPerception.TrySampleRetreatPoint(transform, targetPlayer, 6f, out Vector3 rp)) _agent.SetDestination(rp);
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

            if (EnemyPerception.TrySamplePatrolPoint(_spawnOrigin, patrolRadius, out Vector3 pt))
            {
                _agent.SetDestination(pt);
            }
        }

        private void HandleDamageTaken(Vector3 hitPoint)
        {
            if (_encounterHeld) return;
            if (targetPlayer != null) { _hasSeenPlayer = true; _lastKnownPlayerPos = targetPlayer.position; }
            if (currentState == RangedAIState.Patrol || currentState == RangedAIState.Idle) TransitionToState(RangedAIState.Chase);
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
                var p = GameObject.FindWithTag("Player");
                if (p != null) targetPlayer = p.transform;
            }
            if (projectilePool == null) projectilePool = FindAnyObjectByType<EnemyProjectilePool>();
        }
    }
}
