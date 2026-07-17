using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcShot
{
    public interface ITurnParticipant
    {
        void SetTurnActive(bool active);
    }

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

    public class TurnManager
    {
        public event Action<int> OnTurnStarted;
        public event Action<int> OnTurnEnded;
        public event Action<int> OnRoundCompleted;
        public event Action<float> OnTurnTimeChanged;

        private readonly List<ITurnParticipant> participants;
        private readonly float turnDuration;

        private int currentPlayerIndex = -1;
        private float remainingTime;
        private int roundCount;
        private bool isRunning;

        public int CurrentPlayerIndex => currentPlayerIndex;
        public int PlayerCount => participants.Count;
        public bool IsRunning => isRunning;
        public float RemainingTime => remainingTime;

        public TurnManager(List<ITurnParticipant> participants, TurnManagerConfig config)
        {
            this.participants = participants ?? new List<ITurnParticipant>();
            this.turnDuration = config != null ? config.turnDuration : 0f;

            if (config != null && config.playerCount != this.participants.Count)
            {
                Debug.LogWarning($"[TurnManager] playerCount config ({config.playerCount}) " +
                                 $"khác số lượng participant truyền vào ({this.participants.Count}).");
            }
        }

        public void StartGame()
        {
            if (participants.Count == 0)
            {
                Debug.LogWarning("[TurnManager] Không có participant nào để bắt đầu turn.");
                return;
            }

            isRunning = true;
            roundCount = 0;
            currentPlayerIndex = -1;

            DisableAllPlayers();
            NextTurn();
        }

        public void StopGame()
        {
            isRunning = false;
            DisableAllPlayers();
        }

        public void Tick(float deltaTime)
        {
            if (!isRunning || turnDuration <= 0f) return;

            remainingTime -= deltaTime;
            OnTurnTimeChanged?.Invoke(Mathf.Max(remainingTime, 0f));

            if (remainingTime <= 0f)
            {
                NextTurn();
            }
        }

        public void LockAllPlayers()
        {
            for (int i = 0; i < participants.Count; i++)
            {
                SetPlayerActive(i, false);
            }
        }

        public void EndCurrentTurn()
        {
            if (!isRunning) return;
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
            currentPlayerIndex = (currentPlayerIndex + 1) % participants.Count;

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
            if (index < 0 || index >= participants.Count) return;

            participants[index]?.SetTurnActive(active);
        }

        private void DisableAllPlayers()
        {
            for (int i = 0; i < participants.Count; i++)
            {
                SetPlayerActive(i, false);
            }
        }
    }
}