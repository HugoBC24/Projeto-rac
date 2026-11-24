using System.Collections.Generic;
using UnityEngine;

namespace ProjectRac.Enemy
{
    public class EnemyProjectilePool : MonoBehaviour
    {
        [Header("Pool Configuration")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private int initialPoolSize = 20;

        private readonly Queue<EnemyProjectile> _availableQueue = new Queue<EnemyProjectile>();
        private Transform _poolRoot;
        private static Material _sharedBodyMaterial;
        private static Material _sharedTrailMaterial;

        private void Awake()
        {
            InitializePool();
        }

        public void Configure(GameObject prefab, int size = 20)
        {
            projectilePrefab = prefab;
            initialPoolSize = size;
        }

        private void InitializePool()
        {
            _poolRoot = transform;

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewPooledInstance();
            }
        }

        private EnemyProjectile CreateNewPooledInstance()
        {
            GameObject instanceObj = projectilePrefab != null
                ? Instantiate(projectilePrefab, _poolRoot)
                : CreateProjectilePrimitive(_poolRoot);

            var projectile = instanceObj.GetComponent<EnemyProjectile>();
            if (projectile == null)
            {
                projectile = instanceObj.AddComponent<EnemyProjectile>();
            }

            instanceObj.SetActive(false);
            _availableQueue.Enqueue(projectile);
            return projectile;
        }

        public EnemyProjectile SpawnProjectile(
            Vector3 position,
            Vector3 direction,
            float damage,
            float speed,
            Transform owner = null)
        {
            if (_poolRoot == null) _poolRoot = transform;

            EnemyProjectile projectile = _availableQueue.Count > 0
                ? _availableQueue.Dequeue()
                : CreateNewPooledInstance();

            projectile.Launch(position, direction, damage, speed, this, owner);
            return projectile;
        }

        public void ReturnToPool(EnemyProjectile projectile)
        {
            if (projectile == null) return;

            projectile.transform.SetParent(_poolRoot);
            _availableQueue.Enqueue(projectile);
        }

        public static GameObject CreateProjectilePrimitive(Transform parent)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "EnemyPlasmaBolt";
            obj.transform.SetParent(parent, false);
            obj.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);

            var rb = obj.GetComponent<Rigidbody>();
            if (rb == null) rb = obj.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (_sharedBodyMaterial == null)
                {
                    Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                    _sharedBodyMaterial = new Material(unlit);
                    if (_sharedBodyMaterial.HasProperty("_BaseColor"))
                        _sharedBodyMaterial.SetColor("_BaseColor", new Color(1f, 0.42f, 0.08f, 1f));
                    else
                        _sharedBodyMaterial.color = new Color(1f, 0.42f, 0.08f, 1f);
                }
                renderer.sharedMaterial = _sharedBodyMaterial;
            }

            var trail = obj.AddComponent<TrailRenderer>();
            trail.time = 0.2f;
            trail.startWidth = 0.16f;
            trail.endWidth = 0.02f;
            trail.startColor = new Color(1f, 0.62f, 0.15f, 0.95f);
            trail.endColor = new Color(1f, 0.18f, 0.04f, 0f);
            if (_sharedTrailMaterial == null)
            {
                _sharedTrailMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            }
            trail.sharedMaterial = _sharedTrailMaterial;

            obj.AddComponent<EnemyProjectile>();
            return obj;
        }
    }
}
