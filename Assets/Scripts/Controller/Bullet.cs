using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ArcShot
{
    public class Bullet : MonoBehaviour
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

        [Header("Rotation")]
        [SerializeField]
        private Rigidbody2D rb;

        public event Action OnResolved;

        private bool resolved;

        private void Start()
        {
            Invoke(nameof(ResolveTimeout), lifeTime);
        }

        private void Update()
        {
            RotateTowardsVelocity();
        }

        private void RotateTowardsVelocity()
        {
            if (rb.linearVelocity.sqrMagnitude < 0.0001f)
                return;

            var angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var receiver = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<DamageReceiver>()
                : null;

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

                receiver.ReceiveDamage(finalDamage);
            }

            Resolve();
        }

        private void OnBecameInvisible()
        {
            Resolve();
        }

        private void ResolveTimeout()
        {
            Resolve();
        }

        private void Resolve()
        {
            if (resolved)
                return;

            resolved = true;

            CancelInvoke(nameof(ResolveTimeout));

            OnResolved?.Invoke();

            Destroy(gameObject);
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
    }
}