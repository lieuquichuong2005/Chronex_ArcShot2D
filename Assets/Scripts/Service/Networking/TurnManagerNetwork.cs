using System.Collections.Generic;
using ArcShot;
using Fusion;
using UnityEngine;

namespace Chronex.Networking
{
    /// <summary>
    /// Bọc TurnManager (pure C# class, giữ nguyên logic gốc) thành networked.
    /// CHỈ Host (State Authority) thực sự Tick() TurnManager thật và ghi property [Networked].
    /// Client đọc property qua ChangeDetector, tự bắn lại các event y hệt bản offline
    /// để LevelView không cần sửa gì phần lắng nghe event.
    /// </summary>
    public sealed class TurnManagerNetwork : NetworkBehaviour
    {
        public static TurnManagerNetwork Instance { get; private set; }

        [Networked] public int CurrentPlayerIndex { get; set; } = -1;
        [Networked] public float RemainingTime { get; set; }
        [Networked] public int RoundCount { get; set; }
        [Networked] public NetworkBool IsRunning { get; set; }

        public event System.Action<int> OnTurnStarted;
        public event System.Action<int> OnTurnEnded;
        public event System.Action<int> OnRoundCompleted;
        public event System.Action<float> OnTurnTimeChanged;

        private TurnManager _turnManager;
        private ChangeDetector _changeDetector;
        private int _lastKnownPlayerIndex = -1;

        public void HostInitialize(List<ITurnParticipant> participants, TurnManagerConfig config)
        {
            if (!Object.HasStateAuthority)
            {
                Debug.LogWarning("[TurnManagerNetwork] HostInitialize chỉ được gọi ở Host.");
                return;
            }

            _turnManager = new TurnManager(participants, config);

            _turnManager.OnTurnStarted += index => { CurrentPlayerIndex = index; };
            _turnManager.OnTurnEnded += index => { OnTurnEnded?.Invoke(index); };
            _turnManager.OnRoundCompleted += round =>
            {
                RoundCount = round;
                OnRoundCompleted?.Invoke(round);
            };
            _turnManager.OnTurnTimeChanged += time => { RemainingTime = time; };

            IsRunning = true;
            _turnManager.StartGame();
        }

        public override void Spawned()
        {
            Instance = this;
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this) Instance = null;
        }

        public override void FixedUpdateNetwork()
        {
            if (Object.HasStateAuthority && _turnManager != null && IsRunning)
            {
                _turnManager.Tick(Runner.DeltaTime);
            }
        }

        // Client (và cả Host, vì Render chạy mỗi frame render bất kể HasStateAuthority) -
        // phát lại event dựa trên thay đổi property networked, để UI/LevelView không cần biết
        // gì về Fusion, vẫn subscribe y hệt bản offline.
        public override void Render()
        {
            if (_changeDetector == null) return;

            foreach (string change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(CurrentPlayerIndex):
                        if (CurrentPlayerIndex != _lastKnownPlayerIndex)
                        {
                            _lastKnownPlayerIndex = CurrentPlayerIndex;
                            OnTurnStarted?.Invoke(CurrentPlayerIndex);
                        }

                        break;

                    case nameof(RemainingTime):
                        OnTurnTimeChanged?.Invoke(RemainingTime);
                        break;
                }
            }
        }

        /// <summary>Gọi bởi GunNetworkController (Host) khi bắn xong, đạn đã resolved.</summary>
        public void HostEndCurrentTurn()
        {
            if (Object.HasStateAuthority) _turnManager?.EndCurrentTurn();
        }

        /// <summary>Gọi bởi GunNetworkController (Host) ngay khi đạn bắn ra - khoá toàn bộ player.</summary>
        public void HostLockAllPlayers()
        {
            if (Object.HasStateAuthority) _turnManager?.LockAllPlayers();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_RequestEndTurn()
        {
            _turnManager?.EndCurrentTurn();
        }
    }
}