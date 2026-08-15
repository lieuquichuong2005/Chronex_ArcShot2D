using System;
using Arcshot.Networking;
using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

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
        private enum HitQuality
        {
            Grazing,
            Normal,
            Direct,
            Perfect
        }

        [SerializeField]
        private float lifeTime = 10f;

        [Header("Hit Quality")]
        [SerializeField]
        private float perfectThreshold = 0.4f;

        [SerializeField]
        private float directThreshold = 0.8f;

        [SerializeField]
        private float normalThreshold = 1.2f;

        [Header("Damage Ranges")]
        [SerializeField]
        private Vector2Int grazingDamage = new(15, 18);

        [SerializeField]
        private Vector2Int normalDamage = new(21, 25);

        [SerializeField]
        private Vector2Int directDamage = new(26, 30);

        [SerializeField]
        private Vector2Int perfectDamage = new(29, 32);

        [Header("Critical Hit")]
        [SerializeField]
        private int grazingCritChance = 5;

        [SerializeField]
        private int normalCritChance = 15;

        [SerializeField]
        private int directCritChance = 30;

        [SerializeField]
        private int perfectCritChance = 50;

        [SerializeField]
        private int critBonusMin = 3;

        [SerializeField]
        private int critBonusMax = 5;

        [SerializeField]
        private int perfectCritBonusMin = 4;

        [SerializeField]
        private int perfectCritBonusMax = 6;

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
            if (Object.HasStateAuthority) LifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);

            AnyBulletSpawned?.Invoke(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            AnyBulletResolved?.Invoke();
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            if (WindManager.Instance != null)
            {
                var windForce = WindManager.Instance.GetWindForce();
                rb.AddForce(new Vector2(windForce, 0f), ForceMode2D.Force);
            }

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
                Vector2 impactPoint = transform.position;
                var targetCenter = other.attachedRigidbody != null
                    ? (Vector2)other.attachedRigidbody.transform.position
                    : (Vector2)other.transform.position;
                var distance = Vector2.Distance(impactPoint, targetCenter);

                var quality = GetHitQuality(distance);
                var baseDamage = GetBaseDamage(quality);

                var critChance = GetCritChance(quality);
                var isCritical = Random.Range(0, 100) < critChance;

                var finalDamage = baseDamage;
                if (isCritical)
                {
                    var bonus = quality == HitQuality.Perfect
                        ? Random.Range(perfectCritBonusMin, perfectCritBonusMax + 1)
                        : Random.Range(critBonusMin, critBonusMax + 1);
                    finalDamage = Mathf.Min(baseDamage + bonus, 35);
                }

                finalDamage = Mathf.Clamp(finalDamage, 15, 35);

                receiver.HostReceiveDamage(finalDamage);

                var victim = receiverObj.GetComponent<PlayerNetworkController>();
                if (victim != null) victim.HostRegisterDamageTaken(finalDamage);

                if (Shooter != null)
                {
                    if (isCritical)
                        Shooter.HostRegisterCriticalHit(finalDamage);
                    else
                        Shooter.HostRegisterHit(finalDamage);
                }

                RPC_ShowDamageText(receiverObj.transform.position, finalDamage, isCritical);
            }

            Resolve();
        }

        public void SetShooter(PlayerNetworkController shooter)
        {
            if (Object.HasStateAuthority) Shooter = shooter;
        }

        private HitQuality GetHitQuality(float distance)
        {
            if (distance < perfectThreshold) return HitQuality.Perfect;
            if (distance < directThreshold) return HitQuality.Direct;
            if (distance < normalThreshold) return HitQuality.Normal;
            return HitQuality.Grazing;
        }

        private int GetBaseDamage(HitQuality quality)
        {
            return quality switch
            {
                HitQuality.Grazing => Random.Range(grazingDamage.x, grazingDamage.y + 1),
                HitQuality.Normal => Random.Range(normalDamage.x, normalDamage.y + 1),
                HitQuality.Direct => Random.Range(directDamage.x, directDamage.y + 1),
                HitQuality.Perfect => Random.Range(perfectDamage.x, perfectDamage.y + 1),
                _ => Random.Range(normalDamage.x, normalDamage.y + 1)
            };
        }

        private int GetCritChance(HitQuality quality)
        {
            return quality switch
            {
                HitQuality.Grazing => grazingCritChance,
                HitQuality.Normal => normalCritChance,
                HitQuality.Direct => directCritChance,
                HitQuality.Perfect => perfectCritChance,
                _ => normalCritChance
            };
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

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ShowDamageText(Vector3 position, int damage, bool isCritical)
        {
            if (DamageTextSpawner.Instance != null)
                DamageTextSpawner.Instance.Spawn(position, damage, isCritical);
        }
    }
}