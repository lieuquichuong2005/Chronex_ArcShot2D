using UnityEngine;

namespace ArcShot
{
    /// <summary>
    /// Spawn DamageText tại vị trí world. Local-only - đặt sẵn trong Level scene
    /// (KHÔNG phải NetworkObject), mỗi client tự chạy khi nhận RPC từ BulletNetwork.
    /// </summary>
    public class DamageTextSpawner : MonoBehaviour
    {
        public static DamageTextSpawner Instance { get; private set; }

        [SerializeField]
        private DamageFloatingText _damageTextPrefab;

        [SerializeField]
        private Vector3 _spawnOffset = new(0f, 1.2f, 0f);

        [SerializeField]
        private float _randomHorizontalOffset = 0.3f;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Spawn(Vector3 worldPosition, int damage, bool isCritical)
        {
            if (_damageTextPrefab == null) return;

            var randX = Random.Range(-_randomHorizontalOffset, _randomHorizontalOffset);
            var pos = worldPosition + _spawnOffset + new Vector3(randX, 0f, 0f);

            var instance = Instantiate(_damageTextPrefab, pos, Quaternion.identity);
            instance.Setup(damage, isCritical);
        }
    }
}