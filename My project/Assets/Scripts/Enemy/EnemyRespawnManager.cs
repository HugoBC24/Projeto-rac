using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectRac.Enemy
{
    public class EnemyRespawnManager : MonoBehaviour
    {
        [Header("Respawn Timing")]
        [Tooltip("Tempo em segundos ate um inimigo aleatorio spawnar apos ser derrotado.")]
        [SerializeField] private float respawnDelay = 20f;
        [SerializeField] private float floorSplitY = 2.5f;
        [Tooltip("Quantas mortes na mansao sao necessarias para liberar o respawn aleatorio.")]
        [SerializeField] private int killsToUnlockRespawn = 1;
        [SerializeField] private int maxSimultaneousRespawns = 1;

        [Header("References")]
        [SerializeField] private GameObject[] enemyPrefabs;
        [SerializeField] private MansionPatrolNetwork patrolNetwork;
        [SerializeField] private EnemyProjectilePool projectilePool;
        [SerializeField] private Transform playerTransform;

        public float RespawnDelay { get => respawnDelay; set => respawnDelay = Mathf.Max(0.1f, value); }
        public bool IsRespawnLoopUnlocked => _respawnUnlocked;
        public int ActiveRespawnedCount => _activeRespawned.Count;
        public GameObject LastSpawnedEnemy { get; private set; }

        private readonly HashSet<EnemyHealth> _activeRespawned = new HashSet<EnemyHealth>();
        private int _mansionKills;
        private bool _respawnUnlocked;
        private bool _timerRunning;
        private float _spawnCountdown;

        private void OnEnable()
        {
            EnemyHealth.OnAnyEnemyDied += HandleAnyEnemyDied;
        }

        private void OnDisable()
        {
            EnemyHealth.OnAnyEnemyDied -= HandleAnyEnemyDied;
        }

        private void Start()
        {
            if (patrolNetwork == null) patrolNetwork = FindAnyObjectByType<MansionPatrolNetwork>();
            if (projectilePool == null) projectilePool = FindAnyObjectByType<EnemyProjectilePool>();
            ResolvePlayer();
        }

        private void Update()
        {
            if (!_respawnUnlocked || !_timerRunning) return;

            _spawnCountdown -= Time.deltaTime;
            if (_spawnCountdown <= 0f)
            {
                _timerRunning = false;
                SpawnRandomEnemyOnOppositeFloor();
            }
        }

        public void Configure(GameObject[] prefabs, MansionPatrolNetwork network, EnemyProjectilePool pool)
        {
            enemyPrefabs = prefabs;
            patrolNetwork = network;
            projectilePool = pool;
        }

        private void HandleAnyEnemyDied(EnemyHealth enemy)
        {
            if (enemy == null) return;

            bool wasRespawned = _activeRespawned.Remove(enemy);
            if (!wasRespawned)
            {
                _mansionKills++;
                if (!_respawnUnlocked && _mansionKills >= Mathf.Max(1, killsToUnlockRespawn))
                {
                    _respawnUnlocked = true;
                }
            }

            if (_respawnUnlocked)
            {
                TryScheduleNextSpawn();
            }
        }

        private void TryScheduleNextSpawn()
        {
            if (!_respawnUnlocked || _timerRunning) return;
            if (_activeRespawned.Count >= Mathf.Max(1, maxSimultaneousRespawns)) return;

            _spawnCountdown = Mathf.Max(0.1f, respawnDelay);
            _timerRunning = true;
        }

        public GameObject SpawnRandomEnemyOnOppositeFloor()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0 || patrolNetwork == null) return null;
            ResolvePlayer();

            bool playerOnSecondFloor = playerTransform != null && playerTransform.position.y >= floorSplitY;
            bool spawnOnSecondFloor = !playerOnSecondFloor;

            if (!patrolNetwork.TryGetSpawnPointForFloor(spawnOnSecondFloor, out Vector3 spawnPos))
            {
                return null;
            }

            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            if (prefab == null) return null;

            GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity);
            instance.name = prefab.name + "_Respawned";
            LastSpawnedEnemy = instance;

            if (instance.TryGetComponent<NavMeshAgent>(out var agent))
            {
                agent.Warp(spawnPos);
            }

            if (instance.GetComponent<EnemyDoorOpener>() == null)
            {
                instance.AddComponent<EnemyDoorOpener>();
            }

            if (instance.TryGetComponent<EnemyMeleeAI>(out var melee))
            {
                melee.ConfigureAsRoamingRespawn();
            }

            if (instance.TryGetComponent<EnemyRangedAI>(out var ranged))
            {
                if (projectilePool != null) ranged.BindProjectilePool(projectilePool);
                ranged.ConfigureAsRoamingRespawn();
            }

            if (instance.TryGetComponent<EnemyHealth>(out var health))
            {
                health.SetInvulnerable(false);
                _activeRespawned.Add(health);
            }

            return instance;
        }

        private void ResolvePlayer()
        {
            if (playerTransform != null) return;
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }
}
