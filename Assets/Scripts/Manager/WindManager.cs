using Chronex.Networking;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace ArcShot
{
    /// <summary>
    /// Quản lý hướng & lực gió, đổi mỗi khi TurnManagerNetwork chuyển sang lượt player khác.
    /// Chỉ Host (State Authority) random giá trị mới; Client đọc qua Networked property (OnChangedRender).
    /// </summary>
    public sealed class WindManager : NetworkBehaviour
    {
        public static WindManager Instance { get; private set; }

        [SerializeField]
        private float forcePerLevel = 2f;

        // -1 = trái, 1 = phải, 0 = lặng gió (khi WindLevel == 0)
        [Networked]
        [OnChangedRender(nameof(OnWindChanged))]
        public int WindDirection { get; set; }

        // 0 = lặng gió, 1-3 = cấp gió
        [Networked] public int WindLevel { get; set; }

        public event System.Action<int, int> WindChanged; // (direction, level)

        public override void Spawned()
        {
            Instance = this;
            SubscribeTurnManagerWhenReady().Forget();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this) Instance = null;

            if (TurnManagerNetwork.Instance != null)
                TurnManagerNetwork.Instance.OnTurnStarted -= HandleTurnStarted;
        }

        private async UniTaskVoid SubscribeTurnManagerWhenReady()
        {
            while (TurnManagerNetwork.Instance == null)
                await UniTask.Yield();

            TurnManagerNetwork.Instance.OnTurnStarted += HandleTurnStarted;

            // Phòng trường hợp turn đầu tiên đã bắt đầu trước khi subscribe kịp.
            if (Object.HasStateAuthority && TurnManagerNetwork.Instance.CurrentPlayerIndex >= 0 && WindLevel == 0 &&
                WindDirection == 0)
                HandleTurnStarted(TurnManagerNetwork.Instance.CurrentPlayerIndex);
        }

        private void HandleTurnStarted(int playerIndex)
        {
            if (!Object.HasStateAuthority) return;

            WindLevel = Random.Range(0, 4); // 0..3, 0 = lặng gió
            WindDirection = WindLevel == 0 ? 0 : Random.value < 0.5f ? -1 : 1;
        }

        private void OnWindChanged()
        {
            WindChanged?.Invoke(WindDirection, WindLevel);
        }

        /// <summary>Lực gió (đơn vị Force) để cộng vào Bullet, dùng ở FixedUpdateNetwork của BulletNetwork.</summary>
        public float GetWindForce()
        {
            return WindDirection * WindLevel * forcePerLevel;
        }
    }
}