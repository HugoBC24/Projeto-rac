using System.Collections.Generic;
using UnityEngine;

namespace ProjectRac.Weapons
{
    public class ShotgunPelletPool : MonoBehaviour
    {
        [Header("Pool Configuration")]
        [SerializeField] private GameObject pelletPrefab;
        [SerializeField] private int initialPoolSize = 48;

        private readonly Queue<ShotgunPellet> _availableQueue = new Queue<ShotgunPellet>();
        private readonly List<ShotgunPellet> _activePellets = new List<ShotgunPellet>();
        private Transform _poolRoot;
        private static Material _sharedTrailMaterial;

        private void Awake()
        {
            InitializePool();
        }

        public void Configure(GameObject prefab, int size = 48)
        {
            pelletPrefab = prefab;
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

        private ShotgunPellet CreateNewPooledInstance()
        {
            GameObject instanceObj;
            if (pelletPrefab != null)
            {
                instanceObj = Instantiate(pelletPrefab, _poolRoot);
            }
            else
            {
                instanceObj = CreatePelletPrimitive(_poolRoot);
            }

            var pellet = instanceObj.GetComponent<ShotgunPellet>();
            if (pellet == null)
            {
                pellet = instanceObj.AddComponent<ShotgunPellet>();
            }

            instanceObj.SetActive(false);
            _availableQueue.Enqueue(pellet);
            return pellet;
        }

        public ShotgunPellet SpawnPellet(Vector3 position, Vector3 direction, Transform owner = null)
        {
            ShotgunPellet pellet = _availableQueue.Count > 0
                ? _availableQueue.Dequeue()
                : CreateNewPooledInstance();

            pellet.Launch(position, direction, this, owner);
            IgnoreCollisionsWithActivePellets(pellet);
            _activePellets.Add(pellet);
            return pellet;
        }

        // PhysX ignores Physics.IgnoreCollision calls on inactive colliders, so mutual pellet
        // immunity must be registered only after each pellet is activated in Launch().
        private void IgnoreCollisionsWithActivePellets(ShotgunPellet newlyActivated)
        {
            var newCol = newlyActivated.PelletCollider;
            if (newCol == null) return;

            for (int i = _activePellets.Count - 1; i >= 0; i--)
            {
                var other = _activePellets[i];
                if (other == null || !other.gameObject.activeInHierarchy)
                {
                    _activePellets.RemoveAt(i);
                    continue;
                }

                var otherCol = other.PelletCollider;
                if (otherCol != null && otherCol != newCol)
                {
                    Physics.IgnoreCollision(newCol, otherCol, true);
                }
            }
        }

        public void ReturnToPool(ShotgunPellet pellet)
        {
            if (pellet == null) return;

            _activePellets.Remove(pellet);
            pellet.transform.SetParent(_poolRoot);
            _availableQueue.Enqueue(pellet);
        }

        public static GameObject CreatePelletPrimitive(Transform parent)
        {
            GameObject pellet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pellet.name = "ShotgunPellet";
            pellet.transform.SetParent(parent);
            pellet.transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);

            var rb = pellet.GetComponent<Rigidbody>();
            if (rb == null) rb = pellet.AddComponent<Rigidbody>();
            rb.mass = 0.08f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.useGravity = false;

            var renderer = pellet.GetComponent<Renderer>();
            if (renderer != null)
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", new Color(1f, 0.75f, 0.25f));
                block.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.1f));
                renderer.SetPropertyBlock(block);
            }

            var trail = pellet.AddComponent<TrailRenderer>();
            trail.time = 0.12f;
            trail.startWidth = 0.06f;
            trail.endWidth = 0.01f;
            trail.startColor = new Color(1f, 0.85f, 0.35f, 0.95f);
            trail.endColor = new Color(1f, 0.45f, 0.1f, 0f);
            if (_sharedTrailMaterial == null)
            {
                _sharedTrailMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            }
            trail.sharedMaterial = _sharedTrailMaterial;

            pellet.AddComponent<ShotgunPellet>();
            return pellet;
        }
    }
}
