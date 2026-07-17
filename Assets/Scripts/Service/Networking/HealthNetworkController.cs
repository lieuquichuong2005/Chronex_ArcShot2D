using System;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

namespace ArcShot.Networking
{
    /// <summary>
    /// Thay HealthController offline. CurrentHP là [Networked] - chỉ Host (State Authority)
    /// được phép TakeDamage/Heal trực tiếp. Client hiển thị qua ChangeDetector, không tự trừ máu.
    /// Giữ nguyên event OnHealthChanged/OnDeath để PlayerController cũ không cần đổi cách subscribe.
    /// </summary>
    public sealed class HealthNetworkController : NetworkBehaviour
    {
        [SerializeField]
        private int maxHP = 100;

        [SerializeField]
        private Image _healthBar;

        [Networked] public int CurrentHP { get; set; }

        public event Action<int, int> OnHealthChanged;
        public event Action OnDeath;

        private ChangeDetector _changeDetector;
        private bool _deathFired;

        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

            if (Object.HasStateAuthority) CurrentHP = maxHP;

            UpdateHealthBar();
        }

        /// <summary>Chỉ được gọi ở Host - VD từ Bullet khi trúng đòn (Bullet chỉ simulate ở Host).</summary>
        public void HostTakeDamage(int damage)
        {
            if (!Object.HasStateAuthority) return;

            CurrentHP = Mathf.Max(CurrentHP - damage, 0);
        }

        public void HostHeal(int amount)
        {
            if (!Object.HasStateAuthority) return;

            CurrentHP = Mathf.Min(CurrentHP + amount, maxHP);
        }

        public override void Render()
        {
            if (_changeDetector == null) return;

            foreach (var change in _changeDetector.DetectChanges(this))
            {
                if (change != nameof(CurrentHP)) continue;

                UpdateHealthBar();
                OnHealthChanged?.Invoke(CurrentHP, maxHP);

                if (CurrentHP <= 0 && !_deathFired)
                {
                    _deathFired = true;
                    OnDeath?.Invoke();
                }
            }
        }

        private void UpdateHealthBar()
        {
            if (_healthBar == null) return;
            _healthBar.fillAmount = maxHP > 0 ? (float)CurrentHP / maxHP : 0f;
        }
    }
}