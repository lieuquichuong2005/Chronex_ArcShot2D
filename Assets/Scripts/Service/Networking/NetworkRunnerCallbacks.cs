using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace Chronex.Services.Networking
{
    /// <summary>
    /// Implement toàn bộ INetworkRunnerCallbacks nhưng chỉ forward ra 3 event
    /// mà NetworkService thực sự cần - tránh NetworkService phải cài đặt
    /// hàng chục method không dùng đến.
    /// </summary>
    public sealed class NetworkRunnerCallbacks : MonoBehaviour, INetworkRunnerCallbacks
    {
        private Action<PlayerRef> _onPlayerJoined;
        private Action<PlayerRef> _onPlayerLeft;
        private Action<List<SessionInfo>> _onSessionListUpdated;

        public void Initialize(
            Action<PlayerRef> onPlayerJoined,
            Action<PlayerRef> onPlayerLeft,
            Action<List<SessionInfo>> onSessionListUpdated)
        {
            _onPlayerJoined = onPlayerJoined;
            _onPlayerLeft = onPlayerLeft;
            _onSessionListUpdated = onSessionListUpdated;
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) => _onPlayerJoined?.Invoke(player);
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) => _onPlayerLeft?.Invoke(player);
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) => _onSessionListUpdated?.Invoke(sessionList);

        // Các callback bắt buộc còn lại của interface - để trống vì NetworkService chưa cần.
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    }
}