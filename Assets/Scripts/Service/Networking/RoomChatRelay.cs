using System;
using Fusion;

namespace Chronex.Networking
{
    public sealed class RoomChatRelay : NetworkBehaviour
    {
        public static RoomChatRelay Instance { get; private set; }

        public event Action<string, string> MessageReceived;
        public event Action GameStarted;

        public override void Spawned()
        {
            Instance = this;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this) Instance = null;
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