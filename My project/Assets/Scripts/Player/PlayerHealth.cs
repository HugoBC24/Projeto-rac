using System;
using UnityEngine;
using ProjectRac.Combat;

namespace ProjectRac.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float invulnerabilityDuration = 0.5f;
        [SerializeField] private Vector3 defaultRespawnPosition = new Vector3(0f, 0.1f, 0f);

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public float HealthNormalized => maxHealth > 0f ? Mathf.Clamp01(CurrentHealth / maxHealth) : 0f;
        public bool IsInvulnerable => Time.time < _lastDamageTime + invulnerabilityDuration;

        public event Action<float, float> OnHealthChanged;
        public event Action OnPlayerDamaged;
        public event Action OnPlayerDied;

        private CharacterController _characterController;
        private PlayerStamina _playerStamina;
        private float _lastDamageTime = -999f;
        private bool _isDead;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _playerStamina = GetComponent<PlayerStamina>();
            CurrentHealth = maxHealth;
            _isDead = false;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (_isDead || IsInvulnerable || amount <= 0f) return;

            _lastDamageTime = Time.time;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            OnPlayerDamaged?.Invoke();

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (_isDead || amount <= 0f) return;

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void SetRespawnPosition(Vector3 position)
        {
            defaultRespawnPosition = position;
        }

        private void Die()
        {
            _isDead = true;
            OnPlayerDied?.Invoke();

            Respawn();
        }

        private void Respawn()
        {
            // CharacterController must be disabled during manual transform teleportation to prevent physics override
            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            transform.position = defaultRespawnPosition;

            if (_characterController != null)
            {
                _characterController.enabled = true;
            }

            CurrentHealth = maxHealth;
            _isDead = false;
            _lastDamageTime = Time.time; // Brief invulnerability right after respawning

            if (_playerStamina != null)
            {
                _playerStamina.RestoreStamina(_playerStamina.MaxStamina);
            }

            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }
}
