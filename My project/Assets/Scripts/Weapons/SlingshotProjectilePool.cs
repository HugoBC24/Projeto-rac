using System.Collections.Generic;
using UnityEngine;

namespace ProjectRac.Weapons
{
    public class SlingshotProjectilePool : MonoBehaviour
    {
        [Header("Pool Configuration")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private int initialPoolSize = 25;

        private readonly Queue<SlingshotProjectile> _availableQueue = new Queue<SlingshotProjectile>();
        private Transform _poolRoot;

        private void Awake()
        {
            InitializePool();
        }

        public void Configure(GameObject prefab, int size = 25)
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

        private SlingshotProjectile CreateNewPooledInstance()
        {
            GameObject instanceObj;
            if (projectilePrefab != null)
            {
                instanceObj = Instantiate(projectilePrefab, _poolRoot);
            }
            else
            {
                instanceObj = CreateStonePrimitive(_poolRoot);
            }

            var projectile = instanceObj.GetComponent<SlingshotProjectile>();
            if (projectile == null)
            {
                projectile = instanceObj.AddComponent<SlingshotProjectile>();
            }

            instanceObj.SetActive(false);
            _availableQueue.Enqueue(projectile);
            return projectile;
        }

        public SlingshotProjectile SpawnProjectile(Vector3 position, Vector3 velocity, Transform owner = null)
        {
            SlingshotProjectile projectile = _availableQueue.Count > 0
                ? _availableQueue.Dequeue()
                : CreateNewPooledInstance();

            projectile.Launch(position, velocity, this, owner);
            return projectile;
        }

        public void ReturnToPool(SlingshotProjectile projectile)
        {
            if (projectile == null) return;

            projectile.transform.SetParent(_poolRoot);
            _availableQueue.Enqueue(projectile);
        }

        public static GameObject CreateStonePrimitive(Transform parent)
        {
            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stone.name = "SlingshotStone";
            stone.transform.SetParent(parent);
            stone.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);

            var rb = stone.GetComponent<Rigidbody>();
            if (rb == null) rb = stone.AddComponent<Rigidbody>();
            rb.mass = 0.35f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var renderer = stone.GetComponent<Renderer>();
            if (renderer != null)
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", new Color(0.45f, 0.42f, 0.38f)); // Slate stone color
                renderer.SetPropertyBlock(block);
            }

            stone.AddComponent<SlingshotProjectile>();
            return stone;
        }
    }
}
