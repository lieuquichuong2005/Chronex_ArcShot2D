using UnityEngine;

namespace ArcShot
{
    [RequireComponent(typeof(HealthController))]
    public class DamageReceiver : MonoBehaviour
    {
        private HealthController health;

        private void Awake()
        {
            health = GetComponent<HealthController>();
        }

        public void ReceiveDamage(int damage)
        {
            health.TakeDamage(damage);
        }
    }
}