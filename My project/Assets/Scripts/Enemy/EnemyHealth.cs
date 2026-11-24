using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectRac.Combat;

namespace ProjectRac.Enemy
{
    [RequireComponent(typeof(Collider))]
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 80f;
        [SerializeField] private float flashDuration = 0.12f;
        [SerializeField] private Color hitFlashColor = new Color(0.95f, 0.15f, 0.15f, 1f);

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        public event Action<float, float> OnHealthChanged;
        public event Action<Vector3> OnDamageTaken;
        public event Action OnDeath;

        private Collider _collider;
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<Color> _originalColors = new List<Color>();
        private MaterialPropertyBlock _propBlock;
        private Coroutine _flashCoroutine;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _propBlock = new MaterialPropertyBlock();
            CurrentHealth = maxHealth;
            IsDead = false;

            CacheRenderers();
        }

        public void SetMaxHealth(float newMaxHealth)
        {
            maxHealth = Mathf.Max(1f, newMaxHealth);
            CurrentHealth = maxHealth;
        }

        public void RefreshRenderers()
        {
            CacheRenderers();
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (IsDead || amount <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            // Zero physical knockback on NavMesh: strictly visual flash and AI alert
            FlashDamageVisual();
            OnDamageTaken?.Invoke(hitPoint);

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        private void CacheRenderers()
        {
            _renderers.Clear();
            _originalColors.Clear();
            GetComponentsInChildren(true, _renderers);

            for (int i = 0; i < _renderers.Count; i++)
            {
                var rend = _renderers[i];
                if (rend != null && rend.sharedMaterial != null)
                {
                    Color baseCol = rend.sharedMaterial.HasProperty("_BaseColor")
                        ? rend.sharedMaterial.GetColor("_BaseColor")
                        : (rend.sharedMaterial.HasProperty("_Color") ? rend.sharedMaterial.GetColor("_Color") : Color.gray);
                    _originalColors.Add(baseCol);
                }
                else
                {
                    _originalColors.Add(Color.gray);
                }
            }
        }

        private void FlashDamageVisual()
        {
            if (!gameObject.activeInHierarchy) return;
            if (_renderers.Count == 0) CacheRenderers();

            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }
            _flashCoroutine = StartCoroutine(DamageFlashRoutine());
        }

        private IEnumerator DamageFlashRoutine()
        {
            for (int i = 0; i < _renderers.Count; i++)
            {
                if (_renderers[i] == null) continue;
                _renderers[i].GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", hitFlashColor);
                _renderers[i].SetPropertyBlock(_propBlock);
            }

            yield return new WaitForSeconds(flashDuration);

            for (int i = 0; i < _renderers.Count; i++)
            {
                if (_renderers[i] == null) continue;
                _renderers[i].GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", _originalColors[i]);
                _renderers[i].SetPropertyBlock(_propBlock);
            }

            _flashCoroutine = null;
        }

        private void Die()
        {
            IsDead = true;
            if (_collider != null) _collider.enabled = false;

            OnDeath?.Invoke();
            StartCoroutine(DeathSinkRoutine());
        }

        private IEnumerator DeathSinkRoutine()
        {
            float elapsed = 0f;
            float duration = 2.0f;
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + Vector3.down * 1.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
