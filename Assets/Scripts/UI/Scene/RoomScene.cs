using System.Linq;
using Chronex.Networking;
using Chronex.Services.Networking;
using Cysharp.Threading.Tasks;
using Fusion;
using QuiChuong2005;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chronex.UI.Room
{
    /// <summary>
    /// Điều phối toàn bộ RoomScene: spawn/bind player view, đồng bộ Ready, quyết định quyền Start
    /// (chỉ Host), leave room, invite (copy room code), và chat realtime qua RoomChatRelay.
    /// </summary>
    public sealed class RoomScene : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField]
        private PlayerRoom _playerRoom; // Dùng làm PREFAB để Instantiate cho từng player.

        [SerializeField]
        private Transform _playerParent;

        [Space(5)]
        [SerializeField]
        private Transform _chatParent;

        [SerializeField]
        private TMP_InputField _chatInput;

        [SerializeField]
        private Button _sendButton;

        [Space(5)]
        [Header("Room Settings")]
        [SerializeField]
        private TextMeshProUGUI _roomId;

        [SerializeField]
        private TextMeshProUGUI _maxPlayer;

        [SerializeField]
        private Image _map;

        [SerializeField]
        private Button _previousMapButton;

        [SerializeField]
        private Button _nextMapButton;

        [Space(5)]
        [Header("Buttons")]
        [SerializeField]
        private Button _leaveRoomButton;

        [SerializeField]
        private Button _inviteButton;

        [SerializeField]
        private Button _readyStartButton;

        [SerializeField]
        private TextMeshProUGUI _readyStartButtonText;

        [SerializeField]
        private NetworkObject _roomPlayerNetworkPrefab;

        [SerializeField]
        private NetworkObject _chatRelayPrefab;

        [SerializeField]
        private MapConfigsManager _mapConfigsManager;

        [Inject]
        private INetworkService _networkService;

        [Inject]
        private ISceneService _sceneManager;

        [Inject]
        private IAudioService _audioService;

        [Inject]
        private IDialogService _dialogService;

        private readonly System.Collections.Generic.Dictionary<PlayerRef, PlayerRoom> _spawnedViews = new();
        private bool _isHost;
        private int _maxPlayerCount;

        private void Awake()
        {
            ServiceLocator.Instance.Resolve(this);
            Debug.Log("[RoomScene] Đã vào phòng.");
        }

        private void OnEnable()
        {
            _networkService.PlayerJoined += HandlePlayerJoined;
            _networkService.PlayerLeft += HandlePlayerLeft;
        }

        private void OnDisable()
        {
            _networkService.PlayerJoined -= HandlePlayerJoined;
            _networkService.PlayerLeft -= HandlePlayerLeft;

            if (RoomChatRelay.Instance != null)
            {
                RoomChatRelay.Instance.MessageReceived -= HandleChatMessageReceived;
                RoomChatRelay.Instance.GameStarted -= HandleGameStarted;
                RoomChatRelay.Instance.MapIndexChanged -= UpdateMapDisplay;
            }
        }

        private void Start()
        {
            _isHost = _networkService.Runner.IsServer;
            _maxPlayerCount =
                4; // TODO: lấy đúng số thật từ session nếu SessionInfo expose được, tạm hardcode theo maxPlayers đã tạo phòng.

            _roomId.text = "RoomId: " + _networkService.CurrentRoomCode;
            UpdatePlayerCountText();

            SetupButtons();

            if (_isHost)
            {
                SpawnMissingPlayerObjects();
                SpawnChatRelay();
            }
            else
            {
                BindExistingPlayersAsync().Forget();
            }

            TrySubscribeChatRelay();
        }

        private void SetupButtons()
        {
            _readyStartButtonText.text = _isHost ? "Bắt đầu" : "Sẵn sàng";
            _readyStartButton.interactable = !_isHost; // Client luôn bấm được để toggle Ready; Host chờ đủ điều kiện.

            _readyStartButton.onClick.AddListener(OnClickReadyOrStart);
            _leaveRoomButton.onClick.AddListener(OnClickLeaveRoom);
            _inviteButton.onClick.AddListener(OnClickInvite);
            _sendButton.onClick.AddListener(OnClickSendChat);

            _previousMapButton.onClick.AddListener(OnClickPreviousMap);
            _nextMapButton.onClick.AddListener(OnClickNextMap);
            _previousMapButton.interactable = _isHost;
            _nextMapButton.interactable = _isHost;
        }

        private void UpdatePlayerCountText()
        {
            _maxPlayer.text = $"MaxPlayers: {_networkService.Runner.ActivePlayers.Count()}/{_maxPlayerCount}";
        }

        // ---------------- PLAYER LIST ----------------

        private void SpawnMissingPlayerObjects()
        {
            var hostRef = _networkService.Runner.LocalPlayer;

            foreach (var player in _networkService.Runner.ActivePlayers)
                if (_networkService.Runner.GetPlayerObject(player) == null)
                    SpawnPlayerObject(player, player == hostRef);
        }

        private void SpawnPlayerObject(PlayerRef player, bool isHost)
        {
            Debug.Log($"Spawn player object: {player}");

            var spawned = _networkService.Runner.Spawn(_roomPlayerNetworkPrefab, inputAuthority: player);
            _networkService.Runner.SetPlayerObject(player, spawned);

            var data = spawned.GetComponent<RoomPlayerNetworkObject>();
            data.IsHost = isHost;

            BindPlayerView(player, data);
            UpdatePlayerCountText();
        }

        private void BindExistingPlayers()
        {
            foreach (var player in _networkService.Runner.ActivePlayers)
            {
                var obj = _networkService.Runner.GetPlayerObject(player);
                if (obj != null) BindPlayerView(player, obj.GetComponent<RoomPlayerNetworkObject>());
            }
        }

        private void BindPlayerView(PlayerRef player, RoomPlayerNetworkObject data)
        {
            if (_spawnedViews.ContainsKey(player)) return;

            var view = Instantiate(_playerRoom, _playerParent);
            view.ReadyStateChanged += RefreshRoomState;

            var isLocal = player == _networkService.Runner.LocalPlayer;
            view.Bind(data, isLocal);

            if (isLocal && !data.IsHost) view.PressedCallback += OnLocalPlayerTogglePressed;

            _spawnedViews[player] = view;
        }

        private void HandlePlayerJoined(PlayerRef player)
        {
            Debug.Log($"[RoomScene] PlayerJoined: {player}");

            if (_isHost)
                if (_networkService.Runner.GetPlayerObject(player) == null)
                    SpawnPlayerObject(player, false);

            BindPlayerViewWhenReady(player).Forget();
            UpdatePlayerCountText();
            RefreshRoomState();
        }

        private void HandlePlayerLeft(PlayerRef player)
        {
            if (_spawnedViews.TryGetValue(player, out var view))
            {
                Destroy(view.gameObject);
                _spawnedViews.Remove(player);
            }

            UpdatePlayerCountText();
            RefreshRoomState();
        }

        private bool AreAllClientsReady()
        {
            if (_networkService == null || _networkService.Runner == null || !_networkService.Runner.IsRunning)
                return false;

            var clients = _networkService.Runner.ActivePlayers
                .Select(p => _networkService.Runner.GetPlayerObject(p))
                .Where(o => o != null)
                .Select(o => o.GetComponent<RoomPlayerNetworkObject>())
                .Where(d => d != null && !d.IsHost)
                .ToList();

            return clients.Count > 0 && clients.All(d => d.IsReady);
        }

        // ---------------- READY / START ----------------

        private void OnLocalPlayerTogglePressed()
        {
            var myObj = _networkService.Runner.GetPlayerObject(_networkService.Runner.LocalPlayer);
            var data = myObj?.GetComponent<RoomPlayerNetworkObject>();
            if (data == null) return;

            data.RPC_SetReady(!data.IsReady);
        }

        private void OnClickReadyOrStart()
        {
            if (_isHost)
            {
                _readyStartButton.interactable = false;
                RoomChatRelay.Instance?.RPC_StartGame();
            }
            else
            {
                OnLocalPlayerTogglePressed();
            }
        }


        // ---------------- LEAVE / INVITE ----------------

        private async void OnClickLeaveRoom()
        {
            await _networkService.LeaveRoomAsync();
            await _sceneManager.LoadSceneAsync<MenuScene>(nameof(MenuScene));
        }

        private void OnClickInvite()
        {
            GUIUtility.systemCopyBuffer = $"{_roomId.text}";
            Debug.Log($"[RoomScene] Đã copy room code: {_roomId.text}");
            // TODO: hiện toast "Đã copy mã phòng" cho người chơi thấy rõ ràng hơn log console.
        }

        // ---------------- CHAT ----------------

        private void SpawnChatRelay()
        {
            _networkService.Runner.Spawn(_chatRelayPrefab);
            InitializeMapIndexWhenReady().Forget();
        }

        private void TrySubscribeChatRelay()
        {
            if (RoomChatRelay.Instance != null)
            {
                RoomChatRelay.Instance.MessageReceived += HandleChatMessageReceived;
                RoomChatRelay.Instance.GameStarted += HandleGameStarted;
                RoomChatRelay.Instance.MapIndexChanged += UpdateMapDisplay;

                UpdateMapDisplay(RoomChatRelay.Instance.MapIndex);
            }
            else
            {
                Invoke(nameof(TrySubscribeChatRelay), 0.5f);
            }
        }

        private void OnClickSendChat()
        {
            var message = _chatInput.text?.Trim();
            if (string.IsNullOrEmpty(message) || RoomChatRelay.Instance == null) return;

            var myObj = _networkService.Runner.GetPlayerObject(_networkService.Runner.LocalPlayer);
            var myName = myObj?.GetComponent<RoomPlayerNetworkObject>()?.PlayerName.ToString() ?? "???";

            RoomChatRelay.Instance.RPC_SendMessage(myName, message);
            _chatInput.text = "";
        }

        private void HandleChatMessageReceived(string playerName, string message)
        {
            // TODO: cần 1 chat message prefab (TMP Text) để Instantiate vào _chatParent -
            // hiện tại field _chatParent chỉ là Transform container, chưa có prefab dòng chat cụ thể.
            Debug.Log($"[Chat] {playerName}: {message}");
        }

        private async UniTaskVoid BindPlayerViewWhenReady(PlayerRef player)
        {
            while (_networkService.Runner.GetPlayerObject(player) == null) await UniTask.Yield();

            if (_spawnedViews.ContainsKey(player))
                return;

            var obj = _networkService.Runner.GetPlayerObject(player);

            BindPlayerView(
                player,
                obj.GetComponent<RoomPlayerNetworkObject>());
        }

        private async UniTaskVoid BindExistingPlayersAsync()
        {
            await UniTask.DelayFrame(1);

            foreach (var player in _networkService.Runner.ActivePlayers)
            {
                while (_networkService.Runner.GetPlayerObject(player) == null) await UniTask.Yield();

                BindPlayerView(
                    player,
                    _networkService.Runner
                        .GetPlayerObject(player)
                        .GetComponent<RoomPlayerNetworkObject>());
            }
        }

        private void RefreshRoomState()
        {
            UpdatePlayerCountText();

            if (_isHost)
                _readyStartButton.interactable = AreAllClientsReady();
        }

        private void HandleGameStarted()
        {
            if (RoomChatRelay.Instance != null && _mapConfigsManager != null)
            {
                int index = RoomChatRelay.Instance.MapIndex;
                if (index >= 0 && index < _mapConfigsManager.MapConfigs.Count)
                {
                    QuiChuong2005.Framework.Services.Bootstrap.Instance.SelectedMapId =
                        _mapConfigsManager.MapConfigs[index].MapId;
                }
            }

            _sceneManager.LoadSceneAsync<LevelScene>(nameof(LevelScene)).Forget();
        }

        private void OnClickPreviousMap()
        {
            if (!_isHost || RoomChatRelay.Instance == null) return;

            var count = _mapConfigsManager.MapConfigs.Count;
            var newIndex = (RoomChatRelay.Instance.MapIndex - 1 + count) % count;

            RoomChatRelay.Instance.MapIndex = newIndex;
        }

        private void OnClickNextMap()
        {
            if (!_isHost || RoomChatRelay.Instance == null) return;

            var count = _mapConfigsManager.MapConfigs.Count;
            var newIndex = (RoomChatRelay.Instance.MapIndex + 1) % count;

            RoomChatRelay.Instance.MapIndex = newIndex;
        }

        private void UpdateMapDisplay(int index)
        {
            if (_mapConfigsManager == null || index < 0 || index >= _mapConfigsManager.MapConfigs.Count) return;

            _map.sprite = _mapConfigsManager.MapConfigs[index].IconMap;
        }

        private async UniTaskVoid InitializeMapIndexWhenReady()
        {
            while (RoomChatRelay.Instance == null)
                await UniTask.Yield();

            RoomChatRelay.Instance.MapIndex = 0;
        }

        public void OnSettingButtonPressed()
        {
            _audioService.PlaySfx(Audio.SFX_Click);
            _ = ShowSettingDialog();
        }

        private async UniTask ShowSettingDialog()
        {
            var dialog = _dialogService.ShowAsync<SettingDialog>();
        }
    }
}