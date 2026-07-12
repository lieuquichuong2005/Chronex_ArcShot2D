using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;

namespace Chronex.Services.Networking
{
    public interface INetworkService
    {
        NetworkRunner Runner { get; }
        bool IsConnected { get; }
        string CurrentRoomCode { get; }

        event Action<PlayerRef> PlayerJoined;
        event Action<PlayerRef> PlayerLeft;
        event Action<List<SessionInfo>> SessionListUpdated;

        UniTask<string> CreateRoomAsync(int maxPlayers, CancellationToken cancellationToken = default);
        UniTask JoinRoomByCodeAsync(string roomCode, CancellationToken cancellationToken = default);
        UniTask QuickMatchAsync(int maxPlayers, CancellationToken cancellationToken = default);
        UniTask<List<SessionInfo>> BrowseRoomsAsync(CancellationToken cancellationToken = default);
        UniTask LeaveRoomAsync();
    }
}