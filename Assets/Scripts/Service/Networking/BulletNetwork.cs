using System;
using Fusion;
using UnityEngine;

namespace ArcShot.Networking
{
    /// <summary>
    /// Thay Bullet offline. Chỉ Host spawn (Runner.Spawn), chỉ Host chạy vật lý/va chạm thật -
    /// NetworkRigidbody2D tự sync vị trí sang Client để hiển thị, Client KHÔNG tự tính va chạm.
    /// OnResolved chỉ có ý nghĩa ở Host (nơi GunNetworkController/TurnManagerNetwork lắng nghe).
    /// </summary>
    [RequireComponent(typeof(NetworkTransform))]
    public sealed class BulletNetwork : NetworkBehaviour
    {
        [SerializeField]
        private float lifeTime = 10f;

        [SerializeField]
        private int damage = 25;

        [SerializeField]
        private Rigidbody2D rb;

        public event Action OnResolved;

        [Networked] private TickTimer LifeTimer { get; set; }
        private bool _resolved;

        public override void Spawned()
        {
            if (Object.HasStateAuthority) LifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            if (LifeTimer.Expired(Runner)) Resolve();
        }

        // Chỉ chạy thật ở Host vì chỉ Host mới có physics simulation quyết định (State Authority).
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!Object.HasStateAuthority) return;

            var receiver = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<DamageReceiverNetwork>()
                : null;

            receiver?.HostReceiveDamage(damage);

            Resolve();
        }

        private void OnBecameInvisible()
        {
            if (Object.HasStateAuthority) Resolve();
        }

        private void Update()
        {
            RotateTowardsVelocity();
        }

        private void RotateTowardsVelocity()
        {
            if (rb.linearVelocity.sqrMagnitude < 0.0001f) return;

            var angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            OnResolved?.Invoke();
            Runner.Despawn(Object);
        }
    }

    /// <summary>
    /// TODO tạm: giả định DamageReceiver gốc chỉ forward sang HealthController.TakeDamage.
    /// Cần bạn xác nhận DamageReceiver.cs thật để mình sửa đúng - đây là bản đoán để code compile được.
    /// </summary>
    public sealed class DamageReceiverNetwork : NetworkBehaviour
    {
        [SerializeField]
        private HealthNetworkController _health;

        public void HostReceiveDamage(int damage)
        {
            if (Object.HasStateAuthority) _health.HostTakeDamage(damage);
        }
    }
}