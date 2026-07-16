using System;
using Fusion;

namespace Chronex.Networking
{
    /// <summary>
    /// NetworkBehaviour riêng chỉ để relay tin nhắn chat - RoomScene (MonoBehaviour thường)
    /// không tự gọi RPC được nên cần 1 NetworkObject trung gian làm việc này.
    /// Host spawn 1 instance duy nhất khi tạo phòng, mọi client đều nhận được qua RPC_Broadcast.
    /// </summary>
    public sealed class RoomChatRelay : NetworkBehaviour
    {
        public static RoomChatRelay Instance { get; private set; }

        public event Action<string, string> MessageReceived; // (playerName, message)

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
    }
}