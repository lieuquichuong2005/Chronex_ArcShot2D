using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace Chronex.Services.Networking
{
    public sealed class NetworkService : INetworkService, IDisposable
    {
        private const string RunnerObjectName = "NetworkRunner(Chronex)";

        public NetworkRunner Runner { get; private set; }
        public bool IsConnected => Runner != null && Runner.IsRunning;
        public string CurrentRoomCode { get; private set; }

        public event Action<PlayerRef> PlayerJoined;
        public event Action<PlayerRef> PlayerLeft;
        public event Action<List<SessionInfo>> SessionListUpdated;

        private NetworkRunnerCallbacks _callbacks;
        private List<SessionInfo> _lastSessionList = new();
        private UniTaskCompletionSource<List<SessionInfo>> _sessionListTcs;

        public async UniTask<string> CreateRoomAsync(
            int maxPlayers,
            CancellationToken cancellationToken = default)
        {
            await EnsureRunnerAsync();

            string roomCode = RoomCodeGenerator.Generate();

            var result = await Runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Host,
                SessionName = roomCode,
                PlayerCount = maxPlayers
            });

            cancellationToken.ThrowIfCancellationRequested();

            if (!result.Ok)
                throw new NetworkServiceException(
                    $"Không thể tạo phòng: {result.ShutdownReason}");

            CurrentRoomCode = roomCode;

            return roomCode;
        }

        public async UniTask JoinRoomByCodeAsync(
            string roomCode,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(roomCode))
                throw new ArgumentException(
                    "Room code không được để trống.",
                    nameof(roomCode));

            await EnsureRunnerAsync();

            string normalizedCode = roomCode.Trim().ToUpperInvariant();

            var result = await Runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Client,
                SessionName = normalizedCode
            });

            cancellationToken.ThrowIfCancellationRequested();

            if (!result.Ok)
                throw new NetworkServiceException(
                    result.ShutdownReason == ShutdownReason.GameNotFound
                        ? $"Không tìm thấy phòng '{normalizedCode}'."
                        : $"Không thể vào phòng: {result.ShutdownReason}");

            CurrentRoomCode = normalizedCode;
        }

        public async UniTask<List<SessionInfo>> BrowseRoomsAsync(
            CancellationToken cancellationToken = default)
        {
            await EnsureRunnerAsync();

            _sessionListTcs = new UniTaskCompletionSource<List<SessionInfo>>();

            var result = await Runner.JoinSessionLobby(SessionLobby.ClientServer);

            if (!result.Ok)
                throw new NetworkServiceException(
                    $"Không thể lấy danh sách phòng: {result.ShutdownReason}");

            using (cancellationToken.Register(() =>
                       _sessionListTcs.TrySetCanceled()))
            {
                var rooms = await _sessionListTcs.Task;

                return rooms
                    .Where(x => x.IsOpen && x.IsVisible)
                    .ToList();
            }
        }

        public async UniTask QuickMatchAsync(
            int maxPlayers,
            CancellationToken cancellationToken = default)
        {
            var rooms = await BrowseRoomsAsync(cancellationToken);

            var room = rooms.FirstOrDefault(r => r.PlayerCount < r.MaxPlayers);

            if (room != null)
            {
                Debug.Log($"Join room: {room.Name}");
                await JoinRoomByCodeAsync(room.Name, cancellationToken);
            }
            else
            {
                Debug.Log("Không có phòng -> tạo mới");
                await CreateRoomAsync(maxPlayers, cancellationToken);
            }
        }

        public async UniTask LeaveRoomAsync()
        {
            if (Runner == null)
                return;

            await Runner.Shutdown();

            UnityEngine.Object.Destroy(Runner.gameObject);

            Runner = null;

            CurrentRoomCode = null;
        }

        private async UniTask EnsureRunnerAsync()
        {
            if (Runner != null)
                return;

            var runnerObject = new GameObject(RunnerObjectName);
            UnityEngine.Object.DontDestroyOnLoad(runnerObject);

            Runner = runnerObject.AddComponent<NetworkRunner>();
            Runner.ProvideInput = true;

            _callbacks = runnerObject.AddComponent<NetworkRunnerCallbacks>();

            _callbacks.Initialize(
                p => PlayerJoined?.Invoke(p),
                p => PlayerLeft?.Invoke(p),
                list =>
                {
                    _lastSessionList = list;
                    SessionListUpdated?.Invoke(list);

                    _sessionListTcs?.TrySetResult(list);
                });

            Runner.AddCallbacks(_callbacks);

            await UniTask.Yield();
        }

        public void Dispose()
        {
            LeaveRoomAsync().Forget();
        }
    }
}