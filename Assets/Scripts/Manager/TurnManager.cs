using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcShot2D
{
    /// <summary>
    /// Config cho TurnManager, set từ LevelView (Inspector hoặc code).
    /// </summary>
    [Serializable]
    public class TurnManagerConfig
    {
        [Tooltip("Số lượng player tham gia, dùng để validate khi khởi tạo")]
        public int playerCount = 2;

        [Tooltip("Thời gian mỗi turn (giây). <= 0 nghĩa là không giới hạn thời gian, " +
                 "phải gọi EndCurrentTurn() thủ công (vd: khi bắn xong)")]
        public float turnDuration = 15f;
    }

    /// <summary>
    /// Quản lý việc luân phiên turn giữa các player.
    /// KHÔNG kế thừa MonoBehaviour - được LevelView khởi tạo bằng "new TurnManager(...)"
    /// và phải được "tick" thủ công từ Update() của LevelView.
    /// </summary>
    public class TurnManager
    {
        // ----- Events để LevelView / UI lắng nghe -----
        public event Action<int> OnTurnStarted;      // index player vừa bắt đầu turn
        public event Action<int> OnTurnEnded;        // index player vừa kết thúc turn
        public event Action<int> OnRoundCompleted;    // số round vừa hoàn thành (mọi player đã đi 1 lượt)
        public event Action<float> OnTurnTimeChanged; // thời gian còn lại của turn hiện tại

        private readonly List<PlayerController> players;
        private readonly List<GunController> guns;
        private readonly float turnDuration;

        private int currentPlayerIndex = -1;
        private float remainingTime;
        private int roundCount;
        private bool isRunning;

        public int CurrentPlayerIndex => currentPlayerIndex;
        public int PlayerCount => players.Count;
        public bool IsRunning => isRunning;
        public float RemainingTime => remainingTime;

        /// <summary>
        /// players và guns phải cùng thứ tự index (player[i] dùng gun[i]).
        /// guns có thể truyền null/rỗng nếu chưa cần khoá súng theo turn.
        /// </summary>
        public TurnManager(List<PlayerController> players, List<GunController> guns, TurnManagerConfig config)
        {
            this.players = players ?? new List<PlayerController>();
            this.guns = guns ?? new List<GunController>();
            this.turnDuration = config != null ? config.turnDuration : 0f;

            if (config != null && config.playerCount != this.players.Count)
            {
                Debug.LogWarning($"[TurnManager] playerCount config ({config.playerCount}) " +
                                  $"khác số lượng player truyền vào ({this.players.Count}).");
            }

            if (this.guns.Count > 0 && this.guns.Count != this.players.Count)
            {
                Debug.LogWarning("[TurnManager] Số lượng GunController không khớp số lượng PlayerController.");
            }
        }

        /// <summary>Bắt đầu game, vô hiệu hoá tất cả player rồi mở turn đầu tiên (index 0).</summary>
        public void StartGame()
        {
            if (players.Count == 0)
            {
                Debug.LogWarning("[TurnManager] Không có player nào để bắt đầu turn.");
                return;
            }

            isRunning = true;
            roundCount = 0;
            currentPlayerIndex = -1;

            DisableAllPlayers();
            NextTurn();
        }

        /// <summary>Dừng game, khoá input của mọi player.</summary>
        public void StopGame()
        {
            isRunning = false;
            DisableAllPlayers();
        }

        /// <summary>
        /// Gọi từ LevelView.Update(deltaTime) mỗi frame.
        /// Vì TurnManager không phải MonoBehaviour nên không tự có Update().
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!isRunning || turnDuration <= 0f)
                return;

            remainingTime -= deltaTime;
            OnTurnTimeChanged?.Invoke(Mathf.Max(remainingTime, 0f));

            if (remainingTime <= 0f)
            {
                NextTurn();
            }
        }

        /// <summary>
        /// Gọi thủ công khi player hiện tại hành động xong (vd: bắn xong viên đạn),
        /// dùng khi turnDuration <= 0 hoặc muốn kết thúc turn sớm.
        /// </summary>
        /// <summary>
        /// Khoá input của TẤT CẢ player, kể cả người đang trong turn -
        /// dùng trong lúc đạn đang bay/được xử lý va chạm, đảm bảo công bằng
        /// (không ai né được đạn bằng cách di chuyển trong lúc chờ kết quả).
        /// </summary>
        public void LockAllPlayers()
        {
            for (int i = 0; i < players.Count; i++)
            {
                SetPlayerActive(i, false);
            }
        }

        public void EndCurrentTurn()
        {
            if (!isRunning)
                return;

            NextTurn();
        }

        private void NextTurn()
        {
            if (currentPlayerIndex >= 0)
            {
                SetPlayerActive(currentPlayerIndex, false);
                OnTurnEnded?.Invoke(currentPlayerIndex);
            }

            int previousIndex = currentPlayerIndex;
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;

            // Hết 1 vòng quay lại player 0 -> tính là round mới
            if (currentPlayerIndex <= previousIndex)
            {
                roundCount++;
                OnRoundCompleted?.Invoke(roundCount);
            }

            remainingTime = turnDuration;

            SetPlayerActive(currentPlayerIndex, true);
            OnTurnStarted?.Invoke(currentPlayerIndex);
        }

        private void SetPlayerActive(int index, bool active)
        {
            if (index < 0 || index >= players.Count)
                return;

            if (players[index] != null)
            {
                players[index].enabled = active;

                if (active)
                    players[index].ResetStamina();
            }

            if (index < guns.Count && guns[index] != null)
                guns[index].enabled = active;
        }

        private void DisableAllPlayers()
        {
            for (int i = 0; i < players.Count; i++)
            {
                SetPlayerActive(i, false);
            }
        }

        public PlayerController GetCurrentPlayer()
        {
            if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count)
                return null;

            return players[currentPlayerIndex];
        }
    }
}