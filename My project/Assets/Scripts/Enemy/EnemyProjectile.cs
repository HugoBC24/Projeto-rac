using UnityEngine;
using ProjectRac.Combat;
using ProjectRac.Player;

namespace ProjectRac.Enemy
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class EnemyProjectile : MonoBehaviour
    {
        [Header("Lifetime")]
        [SerializeField] private float maxLifetime = 4.0f;

        private Rigidbody _rigidbody;
        private Collider _collider;
        private TrailRenderer _trailRenderer;
        private EnemyProjectilePool _pool;
        private Transform _owner;
        private float _damage = 15f;
        private float _spawnTime;
        private bool _isFlying;
        private Vector3 _currentVelocity;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _trailRenderer = GetComponent<TrailRenderer>();
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.useGravity = false;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void FixedUpdate()
        {
            if (_isFlying)
            {
                _currentVelocity = _rigidbody.linearVelocity;
            }
        }

        private void Update()
        {
            if (_isFlying && Time.time >= _spawnTime + maxLifetime)
            {
                Recycle();
            }
        }

        public void Launch(
            Vector3 origin,
            Vector3 direction,
            float damage,
            float speed,
            EnemyProjectilePool pool,
            Transform owner = null)
        {
            if (_collider == null) _collider = GetComponent<Collider>();
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
            if (_trailRenderer == null) _trailRenderer = GetComponent<TrailRenderer>();

            _pool = pool;
            _owner = owner;
            _damage = damage;

            Vector3 dirNormalized = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            Quaternion lookRot = Quaternion.LookRotation(dirNormalized);
            transform.SetPositionAndRotation(origin, lookRot);

            gameObject.SetActive(true);

            // PhysX ignores IgnoreCollision calls on inactive colliders (KI-05).
            if (_owner != null && _collider != null)
            {
                var ownerColliders = _owner.GetComponentsInChildren<Collider>();
                for (int i = 0; i < ownerColliders.Length; i++)
                {
                    var ownerCol = ownerColliders[i];
                    if (ownerCol != null && ownerCol != _collider && ownerCol.enabled && ownerCol.gameObject.activeInHierarchy)
                    {
                        Physics.IgnoreCollision(_collider, ownerCol, true);
                    }
                }
            }

            _rigidbody.position = origin;
            _rigidbody.rotation = lookRot;
            _rigidbody.angularVelocity = Vector3.zero;
            _trailRenderer?.Clear();

            Vector3 velocity = dirNormalized * speed;
            _rigidbody.linearVelocity = velocity;
            _currentVelocity = velocity;
            _spawnTime = Time.time;
            _isFlying = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isFlying) return;

            if (collision.gameObject.GetComponent<EnemyProjectile>() != null)
            {
                return;
            }

            // Ignore shooter and fellow enemies so ranged units do not friendly-fire.
            if (_owner != null && (collision.transform == _owner || collision.transform.IsChildOf(_owner)))
            {
                return;
            }

            if (collision.collider.GetComponentInParent<EnemyHealth>() != null)
            {
                return;
            }

            Vector3 hitPoint = collision.contactCount > 0 ? collision.contacts[0].point : transform.position;
            Vector3 hitDir = _currentVelocity.sqrMagnitude > 0.0001f ? _currentVelocity.normalized : transform.forward;

            // Only damage the player; walls, doors, and props stop the projectile without toggling interactables.
            var playerHealth = collision.collider.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(_damage, hitPoint, hitDir);
            }

            Recycle();
        }

        public void Recycle()
        {
            if (!_isFlying) return;

            _isFlying = false;
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
