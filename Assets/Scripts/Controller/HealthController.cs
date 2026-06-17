using System;
using UnityEngine;

namespace ArcShot2D
{
    public class HealthController : MonoBehaviour
    {
        [SerializeField] private int maxHP = 100;

        public int CurrentHP { get; private set; }

        public event Action<int, int> OnHealthChanged;
        public event Action OnDeath;

        private void Awake()
        {
            CurrentHP = maxHP;
        }

        public void TakeDamage(int damage)
        {
            CurrentHP -= damage;

            CurrentHP = Mathf.Max(CurrentHP, 0);

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

            OnHealthChanged?.Invoke(CurrentHP, maxHP);
        }
    }
}