using UnityEngine;
using ProjectRac.Interaction;
using ProjectRac.Combat;

namespace ProjectRac.Weapons
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class SlingshotProjectile : MonoBehaviour
    {
        [Header("Impact Settings")]
        [SerializeField] private float impactForceMultiplier = 1.2f;
        [SerializeField] private float maxLifetime = 4.0f;

        private Rigidbody _rigidbody;
        private Collider _collider;
        private SlingshotProjectilePool _pool;
        private Transform _owner;
        private float _spawnTime;
        private bool _isLaunched;
        private Vector3 _currentVelocity;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        private void FixedUpdate()
        {
            if (_isLaunched)
            {
                _currentVelocity = _rigidbody.linearVelocity;
            }
        }

        private void Update()
        {
            if (_isLaunched && Time.time >= _spawnTime + maxLifetime)
            {
                Recycle();
            }
        }

        public void Launch(Vector3 origin, Vector3 velocity, SlingshotProjectilePool pool, Transform owner = null)
        {
            if (_collider == null) _collider = GetComponent<Collider>();
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();

            _pool = pool;
            _owner = owner;
            transform.position = origin;

            gameObject.SetActive(true);

            // Register IgnoreCollision after activation so PhysX honours it.
            if (_owner != null && _collider != null)
            {
                var ownerColliders = _owner.GetComponentsInChildren<Collider>();
                foreach (var col in ownerColliders)
                {
                    if (col != null && col != _collider && col.enabled && col.gameObject.activeInHierarchy)
                    {
                        Physics.IgnoreCollision(_collider, col, true);
                    }
                }
            }

            _rigidbody.position = origin;
            _rigidbody.linearVelocity = velocity;
            _rigidbody.angularVelocity = Random.insideUnitSphere * 5f;
            _currentVelocity = velocity;
            _spawnTime = Time.time;
            _isLaunched = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isLaunched) return;

            if (_owner != null && (collision.transform == _owner || collision.transform.IsChildOf(_owner)))
            {
                return;
            }

            if (collision.gameObject.CompareTag("Player"))
            {
                return;
            }

            Vector3 hitPoint = collision.contactCount > 0 ? collision.contacts[0].point : transform.position;

            // Apply damage if target implements IDamageable
            var damageable = collision.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(30f, hitPoint, _currentVelocity.normalized);
            }

            // Apply physical impulse to Rigidbody target
            if (collision.rigidbody != null)
            {
                Vector3 impulse = _currentVelocity * impactForceMultiplier;
                collision.rigidbody.AddForceAtPosition(impulse, hitPoint, ForceMode.Impulse);
            }

            Recycle();
        }

        public void Recycle()
        {
            if (!_isLaunched) return;

            _isLaunched = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            gameObject.SetActive(false);

            if (_pool != null)
            {
                _pool.ReturnToPool(this);
            }
        }
    }
}
