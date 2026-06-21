using System;
using UnityEngine;

namespace ArcShot2D
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 10f;
        [SerializeField] private int damage = 25;

        // Báo cho LevelView biết đạn đã được xử lý xong (đã trừ máu nếu trúng, hoặc đã huỷ vì
        // hết thời gian / bay ra khỏi map) - lúc này mới được phép chuyển sang turn kế tiếp.
        public event Action OnResolved;

        private bool resolved;

        private void Start()
        {
            Invoke(nameof(ResolveTimeout), lifeTime);
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