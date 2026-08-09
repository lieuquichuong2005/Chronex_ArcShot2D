using System;
using Fusion;
using Random = UnityEngine.Random;

namespace Chronex.Networking
{
    /// <summary>
    /// Dữ liệu networked của 1 player trong phòng chờ (tên, trạng thái Ready, có phải Host không).
    /// Spawned bởi Host cho mỗi player khi vào RoomScene. Chỉ State Authority (Host) mới có quyền
    /// ghi trực tiếp property [Networked] - Client phải gọi RPC để yêu cầu Host cập nhật giúp.
    /// </summary>
    public sealed class RoomPlayerNetworkObject : NetworkBehaviour
    {
        [Networked, OnChangedRender(nameof(OnPlayerNameChanged))]
        public NetworkString<_32> PlayerName { get; set; }

        [Networked, OnChangedRender(nameof(OnReadyChanged))]
        public NetworkBool IsReady { get; set; }

        [Networked, OnChangedRender(nameof(OnHostChanged))]
        public NetworkBool IsHost { get; set; }

        public event Action OnDespawned;

        public event Action PlayerNameChanged;
        public event Action ReadyChanged;
        public event Action HostChanged;

        public override void Spawned()
        {
            if (Object.HasInputAuthority)
            {
                string playerName = QuiChuong2005.ServiceLocator.Instance
                    .Get<Chronex.Services.Profile.IPlayerProfileService>()
                    .PlayerName;

                RPC_SetName(playerName);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            OnDespawned?.Invoke();
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SetName(string name) => PlayerName = name;

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SetReady(NetworkBool ready) => IsReady = ready;

        private void OnPlayerNameChanged()
        {
            PlayerNameChanged?.Invoke();
        }

        private void OnReadyChanged()
        {
            ReadyChanged?.Invoke();
        }

        private void OnHostChanged()
        {
            HostChanged?.Invoke();
        }
    }
}