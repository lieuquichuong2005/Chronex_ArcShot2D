using QuiChuong2005;
using UnityEngine;

namespace ArcShot
{
    public class DamageTextSpawner : MonoBehaviour
    {
        public static DamageTextSpawner Instance { get; private set; }

        [SerializeField]
        private Vector3 _spawnOffset = new(0f, 1.2f, 0f);

        [SerializeField]
        private float _randomHorizontalOffset = 0.3f;

        [Inject]
        private IEntityService _entityService;

        private void Awake()
        {
            Instance = this;
            ServiceLocator.Instance.Resolve(this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Spawn(Vector3 worldPosition, int damage, bool isCritical)
        {
            var randX = Random.Range(-_randomHorizontalOffset, _randomHorizontalOffset);
            var pos = worldPosition + _spawnOffset + new Vector3(randX, 0f, 0f);

            var floatingDamage = _entityService.Spawn<DamageFloatingText>();
            floatingDamage.transform.position = pos;
            floatingDamage.transform.rotation = Quaternion.identity;
            floatingDamage.Setup(damage, isCritical);
        }
    }
}