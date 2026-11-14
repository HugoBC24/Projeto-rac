using UnityEngine;
using ProjectRac.Interaction;

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
        private SlingshotProjectilePool _pool;
        private float _spawnTime;
        private bool _isLaunched;
        private Vector3 _currentVelocity;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
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

        public void Launch(Vector3 origin, Vector3 velocity, SlingshotProjectilePool pool)
        {
            _pool = pool;
            transform.position = origin;
            gameObject.SetActive(true);

            _rigidbody.linearVelocity = velocity;
            _rigidbody.angularVelocity = Random.insideUnitSphere * 5f;
            _currentVelocity = velocity;
            _spawnTime = Time.time;
            _isLaunched = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isLaunched) return;

            // Apply physical impulse to Rigidbody target
            if (collision.rigidbody != null)
            {
                Vector3 hitPoint = collision.contactCount > 0 ? collision.contacts[0].point : transform.position;
                Vector3 impulse = _currentVelocity * impactForceMultiplier;
                collision.rigidbody.AddForceAtPosition(impulse, hitPoint, ForceMode.Impulse);
            }

            // Trigger interactables if target implements IInteractable
            var interactable = collision.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact();
            }

            // Immediately destroy/recycle stone on collision as requested
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
