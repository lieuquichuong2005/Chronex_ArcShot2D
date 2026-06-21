using System;
using UnityEngine;
using UnityEngine.UI;

namespace ArcShot2D
{
    public class HealthController : MonoBehaviour
    {
        [SerializeField] private int maxHP = 100;

        [SerializeField] private Image _healthBar; // (Image With ImageType: Filled)

        public int CurrentHP { get; private set; }

        public event Action<int, int> OnHealthChanged;
        public event Action OnDeath;

        private void Awake()
        {
            CurrentHP = maxHP;

            UpdateHealthBar();
        }

        public void TakeDamage(int damage)
        {
            CurrentHP -= damage;

            CurrentHP = Mathf.Max(CurrentHP, 0);

            UpdateHealthBar();

            OnHealthChanged?.Invoke(CurrentHP, maxHP);

            if (CurrentHP <= 0)
            {
                OnDeath?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            CurrentHP += amount;

            CurrentHP = Mathf.Min(CurrentHP, maxHP);

            UpdateHealthBar();

            OnHealthChanged?.Invoke(CurrentHP, maxHP);
        }

        private void UpdateHealthBar()
        {
            if (_healthBar == null)
                return;

            _healthBar.fillAmount = maxHP > 0
                ? (float)CurrentHP / maxHP
                : 0f;
        }
    }
}