using System;
using Arcshot.Networking;
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
        [Networked] public PlayerNetworkController Shooter { get; set; }

        private bool _resolved;

        public static event Action<BulletNetwork> AnyBulletSpawned;
        public static event Action AnyBulletResolved;

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                LifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
            }

            AnyBulletSpawned?.Invoke(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            AnyBulletResolved?.Invoke();
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            if (LifeTimer.Expired(Runner)) Resolve();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_resolved) return;
            if (Object == null || !Object.HasStateAuthority) return;

            var receiverObj = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : null;
            var receiver = receiverObj != null ? receiverObj.GetComponent<DamageReceiverNetwork>() : null;

            if (receiver != null)
            {
                receiver.HostReceiveDamage(damage);

                var victim = receiverObj.GetComponent<PlayerNetworkController>();
                if (victim != null)
                {
                    victim.HostRegisterDamageTaken(damage);
                }

                if (Shooter != null)
                {
                    Shooter.HostRegisterHit(damage);
                }
            }

            Resolve();
        }

        public void SetShooter(PlayerNetworkController shooter)
        {
            if (Object.HasStateAuthority) Shooter = shooter;
        }


        private void OnBecameInvisible()
        {
            if (_resolved) return; // THÊM
            if (Object == null || !Object.HasStateAuthority) return;

            Resolve();
        }

        private void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            OnResolved?.Invoke();

            if (Object != null && Object.IsValid) Runner.Despawn(Object);
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
    }
}