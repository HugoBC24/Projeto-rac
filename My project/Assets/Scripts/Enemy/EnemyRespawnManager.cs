using System.Collections.Generic;
using UnityEngine;

namespace ProjectRac.Enemy
{
    public class EnemyRespawnManager : MonoBehaviour
    {
        [Header("Respawn Timing")]
        [Tooltip("Tempo em segundos ate um inimigo aleatorio spawnar apos ser derrotado.")]
        [SerializeField] private float respawnDelay = 20f;
        [SerializeField] private float floorSplitY = 2.5f;
        [SerializeField] private bool requireAllInitialEnemiesDefeated = true;
        [SerializeField] private int maxSimultaneousRespawns = 1;

        [Header("References")]
        [SerializeField] private GameObject[] enemyPrefabs;
        [SerializeField] private MansionPatrolNetwork patrolNetwork;
        [SerializeField] private EnemyProjectilePool projectilePool;
        [SerializeField] private Transform playerTransform;

        public float RespawnDelay { get => respawnDelay; set => respawnDelay = Mathf.Max(0.1f, value); }
        public bool IsRespawnLoopUnlocked => _respawnUnlocked;
        public int ActiveRespawnedCount => _activeRespawned.Count;

        private readonly HashSet<EnemyHealth> _initialEnemies = new HashSet<EnemyHealth>();
        private readonly HashSet<EnemyHealth> _activeRespawned = new HashSet<EnemyHealth>();
        private bool _respawnUnlocked;
        private bool _timerRunning;
        private float _spawnCountdown;

        private void Start()
        {
            if (patrolNetwork == null) patrolNetwork = FindAnyObjectByType<MansionPatrolNetwork>();
            if (projectilePool == null) projectilePool = FindAnyObjectByType<EnemyProjectilePool>();
            ResolvePlayer();
            RegisterInitialEnemies();
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

        private void RegisterInitialEnemies()
        {
            var found = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                EnemyHealth eh = found[i];
                if (eh == null || eh.IsDead) continue;
                _initialEnemies.Add(eh);
                eh.OnDeath += () => HandleInitialEnemyDeath(eh);
            }

            if (_initialEnemies.Count == 0)
            {
                UnlockRespawnLoop();
            }
        }

        private void HandleInitialEnemyDeath(EnemyHealth enemy)
        {
            _initialEnemies.Remove(enemy);
            if (_respawnUnlocked) return;

            if (!requireAllInitialEnemiesDefeated || _initialEnemies.Count == 0)
            {
                UnlockRespawnLoop();
            }
        }

        private void UnlockRespawnLoop()
        {
            _respawnUnlocked = true;
            TryScheduleNextSpawn();
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
                health.OnDeath += () => HandleRespawnedEnemyDeath(health);
            }

            return instance;
        }

        private void HandleRespawnedEnemyDeath(EnemyHealth enemy)
        {
            _activeRespawned.Remove(enemy);
            TryScheduleNextSpawn();
        }

        private void ResolvePlayer()
        {
            if (playerTransform != null) return;
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }
}
