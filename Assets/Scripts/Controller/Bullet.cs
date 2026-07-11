using System;
using UnityEngine;

namespace ArcShot
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 10f;
        [SerializeField] private int damage = 25;

        [Header("Rotation")]
        [SerializeField] private Rigidbody2D rb; // kéo Rigidbody2D vào đây trong Inspector

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

            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            DamageReceiver receiver = other.attachedRigidbody != null
                ? other.attachedRigidbody.GetComponent<DamageReceiver>()
                : null;

            if (receiver != null)
            {
                receiver.ReceiveDamage(damage);
            }

            Resolve();
        }

        // Renderer (sprite) của đạn ra khỏi vùng nhìn thấy của camera -> coi như bay quá xa khỏi map
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
    }
}