using Fusion;
using UnityEngine;

namespace Chronex.Networking
{
    /// <summary>
    /// Dữ liệu networked của 1 player trong phòng chờ (tên, trạng thái Ready, có phải Host không).
    /// Spawned bởi Host cho mỗi player khi vào RoomScene. Chỉ State Authority (Host) mới có quyền
    /// ghi trực tiếp property [Networked] - Client phải gọi RPC để yêu cầu Host cập nhật giúp.
    /// </summary>
    public sealed class RoomPlayerNetworkObject : NetworkBehaviour
    {
        [Networked] public NetworkString<_32> PlayerName { get; set; }
        [Networked] public NetworkBool IsReady { get; set; }
        [Networked] public NetworkBool IsHost { get; set; }

        public override void Spawned()
        {
            if (Object.HasInputAuthority)
            {
                // TODO: thay bằng tên thật khi có chức năng đặt tên ở Menu (đọc từ DataService).
                string autoName = $"Player{Random.Range(1000, 9999)}";
                RPC_SetName(autoName);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SetName(string name) => PlayerName = name;

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SetReady(NetworkBool ready) => IsReady = ready;
    }
}