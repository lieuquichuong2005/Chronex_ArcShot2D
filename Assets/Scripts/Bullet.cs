using UnityEngine;

namespace ArcShot2D
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 10f;

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }
    }
}