using System;
using UnityEngine;

namespace ProjectRac.Player
{
    public class PlayerStamina : MonoBehaviour
    {
        [Header("Stamina Settings")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float sprintDrainRate = 20f;
        [SerializeField] private float jumpCost = 15f;
        [SerializeField] private float regenRate = 18f;
        [SerializeField] private float regenDelay = 1.2f;
        [SerializeField] private float minStaminaToSprintAfterExhaustion = 20f;

        public float MaxStamina => maxStamina;
        public float CurrentStamina { get; private set; }
        public float StaminaNormalized => maxStamina > 0f ? Mathf.Clamp01(CurrentStamina / maxStamina) : 0f;
        public bool IsExhausted { get; private set; }

        public event Action<float, float> OnStaminaChanged;
        public event Action<bool> OnExhaustionChanged;

        private float _lastDrainTime;

        private void Awake()
        {
            CurrentStamina = maxStamina;
            IsExhausted = false;
        }

        private void Update()
        {
            HandleRegeneration();
        }

        public bool TryConsumeSprint(float deltaTime)
        {
            if (IsExhausted)
            {
                return false;
            }

            float cost = sprintDrainRate * deltaTime;
            if (CurrentStamina <= 0f)
            {
                SetExhausted(true);
                return false;
            }

            CurrentStamina = Mathf.Max(0f, CurrentStamina - cost);
            _lastDrainTime = Time.time;
            OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);

            if (CurrentStamina <= 0f)
            {
                SetExhausted(true);
                return false;
            }

            return true;
        }

        public bool TryConsumeJump()
        {
            if (CurrentStamina < jumpCost)
            {
                return false;
            }

            CurrentStamina = Mathf.Max(0f, CurrentStamina - jumpCost);
            _lastDrainTime = Time.time;
            OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);

            if (CurrentStamina <= 0f)
            {
                SetExhausted(true);
            }

            return true;
        }

        public void RestoreStamina(float amount)
        {
            if (amount <= 0f) return;

            CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + amount);
            if (IsExhausted && CurrentStamina >= minStaminaToSprintAfterExhaustion)
            {
                SetExhausted(false);
            }
            OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
        }

        private void HandleRegeneration()
        {
            if (Time.time < _lastDrainTime + regenDelay)
            {
                return;
            }

            if (CurrentStamina < maxStamina)
            {
                CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + regenRate * Time.deltaTime);
                if (IsExhausted && CurrentStamina >= minStaminaToSprintAfterExhaustion)
                {
                    SetExhausted(false);
                }
                OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
            }
        }

        private void SetExhausted(bool exhausted)
        {
            if (IsExhausted != exhausted)
            {
                IsExhausted = exhausted;
                OnExhaustionChanged?.Invoke(IsExhausted);
            }
        }
    }
}
