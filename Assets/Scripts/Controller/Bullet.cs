using UnityEngine;

namespace ArcShot2D
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 10f;
        [SerializeField] private int damage = 25;

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            DamageReceiver receiver = other.attachedRigidbody.GetComponent<DamageReceiver>();

            if (receiver != null)
            {
                receiver.ReceiveDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}