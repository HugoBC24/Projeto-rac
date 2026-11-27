using UnityEngine;
using ProjectRac.Interaction;
using ProjectRac.Combat;

namespace ProjectRac.Weapons
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class ShotgunPellet : MonoBehaviour
    {
        [Header("Pellet Settings")]
        [SerializeField] private float speed = 105f;
        [SerializeField] private float impulseForce = 5.5f;
        [SerializeField] private float maxLifetime = 2.0f;

        public Collider PelletCollider => _collider != null ? _collider : GetComponent<Collider>();

        private Rigidbody _rigidbody;
        private Collider _collider;
        private TrailRenderer _trailRenderer;
        private ShotgunPelletPool _pool;
        private Transform _owner;
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

        public void Launch(Vector3 origin, Vector3 direction, ShotgunPelletPool pool, Transform owner = null)
        {
            if (_collider == null) _collider = GetComponent<Collider>();
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
            if (_trailRenderer == null) _trailRenderer = GetComponent<TrailRenderer>();

            _pool = pool;
            _owner = owner;

            Vector3 dirNormalized = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            Quaternion lookRot = Quaternion.LookRotation(dirNormalized);
            transform.SetPositionAndRotation(origin, lookRot);

            gameObject.SetActive(true);

            // PhysX ignores IgnoreCollision on inactive colliders, so register after SetActive(true).
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

            // 1. Ignore collision with other pellets!
            if (collision.gameObject.GetComponent<ShotgunPellet>() != null)
            {
                return;
            }

            // 2. Ignore collision with the shooter/player
            if (_owner != null && (collision.transform == _owner || collision.transform.IsChildOf(_owner)))
            {
                return;
            }

            if (collision.gameObject.CompareTag("Player") || collision.collider is CharacterController)
            {
                return;
            }

            Vector3 hitPoint = collision.contactCount > 0 ? collision.contacts[0].point : transform.position;

            // Apply damage if target implements IDamageable
            var damageable = collision.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(16f, hitPoint, _currentVelocity.normalized);
            }

            // 3. Apply physics impulse
            if (collision.rigidbody != null)
            {
                collision.rigidbody.AddForceAtPosition(_currentVelocity.normalized * impulseForce, hitPoint, ForceMode.Impulse);
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
