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

        public async UniTask<string> CreateRoomAsync(int maxPlayers, CancellationToken cancellationToken = default)
        {
            await EnsureCleanRunnerAsync();

            string roomCode = RoomCodeGenerator.Generate();

            StartGameResult result = await Runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Host,
                SessionName = roomCode,
                PlayerCount = maxPlayers
            });

            cancellationToken.ThrowIfCancellationRequested();

            if (!result.Ok)
            {
                throw new NetworkServiceException($"Không thể tạo phòng: {result.ShutdownReason}");
            }

            CurrentRoomCode = roomCode;
            return roomCode;
        }

        public async UniTask JoinRoomByCodeAsync(string roomCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(roomCode))
            {
                throw new ArgumentException("Room code không được để trống.", nameof(roomCode));
            }

            await EnsureCleanRunnerAsync();

            string normalizedCode = roomCode.Trim().ToUpperInvariant();

            StartGameResult result = await Runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Client,
                SessionName = normalizedCode
            });

            cancellationToken.ThrowIfCancellationRequested();

            if (!result.Ok)
            {
                throw new NetworkServiceException(
                    result.ShutdownReason == ShutdownReason.GameNotFound
                        ? $"Không tìm thấy phòng '{normalizedCode}'."
                        : $"Không thể vào phòng: {result.ShutdownReason}");
            }

            CurrentRoomCode = normalizedCode;
        }

        public async UniTask<List<SessionInfo>> BrowseRoomsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureCleanRunnerAsync();

            StartGameResult result = await Runner.JoinSessionLobby(SessionLobby.ClientServer);
            cancellationToken.ThrowIfCancellationRequested();

            if (!result.Ok)
            {
                throw new NetworkServiceException($"Không thể lấy danh sách phòng: {result.ShutdownReason}");
            }

            // Danh sách phòng đến qua callback OnSessionListUpdated (bất đồng bộ, không có
            // "await" trực tiếp được) - đợi 1 nhịp để callback đầu tiên kịp chạy trước khi trả kết quả.
            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);

            return _lastSessionList.Where(s => s.IsOpen && s.IsVisible).ToList();
        }

        public async UniTask QuickMatchAsync(int maxPlayers, CancellationToken cancellationToken = default)
        {
            List<SessionInfo> rooms = await BrowseRoomsAsync(cancellationToken);

            SessionInfo joinable = rooms.FirstOrDefault(s => s.PlayerCount < s.MaxPlayers);

            if (joinable != null)
            {
                await JoinRoomByCodeAsync(joinable.Name, cancellationToken);
            }
            else
            {
                await CreateRoomAsync(maxPlayers, cancellationToken);
            }
        }

        public async UniTask LeaveRoomAsync()
        {
            if (Runner == null) return;

            await Runner.Shutdown();
            CurrentRoomCode = null;
        }

        private async UniTask EnsureCleanRunnerAsync()
        {
            if (Runner != null)
            {
                await Runner.Shutdown();
                UnityEngine.Object.Destroy(Runner.gameObject);
                Runner = null;
            }

            var runnerObject = new GameObject(RunnerObjectName);
            UnityEngine.Object.DontDestroyOnLoad(runnerObject);

            Runner = runnerObject.AddComponent<NetworkRunner>();
            Runner.ProvideInput = true;

            _callbacks = runnerObject.AddComponent<NetworkRunnerCallbacks>();
            _callbacks.Initialize(
                onPlayerJoined: p => PlayerJoined?.Invoke(p),
                onPlayerLeft: p => PlayerLeft?.Invoke(p),
                onSessionListUpdated: list =>
                {
                    _lastSessionList = list;
                    SessionListUpdated?.Invoke(list);
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