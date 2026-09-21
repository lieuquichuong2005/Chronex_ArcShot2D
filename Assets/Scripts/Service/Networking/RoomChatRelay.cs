using System;
using Fusion;

namespace Chronex.Networking
{
    public sealed class RoomChatRelay : NetworkBehaviour
    {
        public static RoomChatRelay Instance { get; private set; }

        public event Action<string, string> MessageReceived;
        public event Action GameStarted;
        public event Action<int> MapIndexChanged;

        [Networked] public int MapIndex { get; set; }

        private ChangeDetector _changeDetector;

        public override void Spawned()
        {
            Instance = this;
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this) Instance = null;
        }

        public override void Render()
        {
            if (_changeDetector == null) return;

            foreach (var change in _changeDetector.DetectChanges(this))
                if (change == nameof(MapIndex))
                    MapIndexChanged?.Invoke(MapIndex);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_SendMessage(string playerName, string message)
        {
            MessageReceived?.Invoke(playerName, message);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_StartGame()
        {
            GameStarted?.Invoke();
        }
    }
}