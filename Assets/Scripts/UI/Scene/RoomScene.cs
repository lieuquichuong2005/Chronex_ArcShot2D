using System.Linq;
using Chronex.Networking;
using Chronex.Services.Networking;
using Fusion;
using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Core.DI;
using QuiChuong2005.Framework.Services.Scenes;
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
        private Image
            _map; // TODO: chưa dùng - cần Custom Session Properties (đã đồng ý làm sau khi core flow ổn định).

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
        private NetworkObject _roomPlayerNetworkPrefab; // Thêm mới - prefab có NetworkObject + RoomPlayerNetworkObject.

        [SerializeField]
        private NetworkObject _chatRelayPrefab; // Thêm mới - prefab rỗng có NetworkObject + RoomChatRelay.

        [Inject]
        private INetworkService _networkService;

        [Inject]
        private ISceneService _sceneManager;

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
                BindExistingPlayers();
            }

            TrySubscribeChatRelay();
        }

        private void Update()
        {
            if (_isHost)
            {
                _readyStartButton.interactable = AreAllClientsReady();
            }
        }

        private void SetupButtons()
        {
            _readyStartButtonText.text = _isHost ? "Bắt đầu" : "Sẵn sàng";
            _readyStartButton.interactable = !_isHost; // Client luôn bấm được để toggle Ready; Host chờ đủ điều kiện.

            _readyStartButton.onClick.AddListener(OnClickReadyOrStart);
            _leaveRoomButton.onClick.AddListener(OnClickLeaveRoom);
            _inviteButton.onClick.AddListener(OnClickInvite);
            _sendButton.onClick.AddListener(OnClickSendChat);
        }

        private void UpdatePlayerCountText()
        {
            _maxPlayer.text = $"MaxPlayers: {_networkService.Runner.ActivePlayers.Count()}/{_maxPlayerCount}";
        }

        // ---------------- PLAYER LIST ----------------

        private void SpawnMissingPlayerObjects()
        {
            PlayerRef hostRef = _networkService.Runner.LocalPlayer;

            foreach (PlayerRef player in _networkService.Runner.ActivePlayers)
            {
                if (_networkService.Runner.GetPlayerObject(player) == null)
                {
                    SpawnPlayerObject(player, isHost: player == hostRef);
                }
            }
        }

        private void SpawnPlayerObject(PlayerRef player, bool isHost)
        {
            NetworkObject spawned = _networkService.Runner.Spawn(_roomPlayerNetworkPrefab, inputAuthority: player);
            _networkService.Runner.SetPlayerObject(player, spawned);

            var data = spawned.GetComponent<RoomPlayerNetworkObject>();
            data.IsHost = isHost;

            BindPlayerView(player, data);
            UpdatePlayerCountText();
        }

        private void BindExistingPlayers()
        {
            foreach (PlayerRef player in _networkService.Runner.ActivePlayers)
            {
                NetworkObject obj = _networkService.Runner.GetPlayerObject(player);
                if (obj != null)
                {
                    BindPlayerView(player, obj.GetComponent<RoomPlayerNetworkObject>());
                }
            }
        }

        private void BindPlayerView(PlayerRef player, RoomPlayerNetworkObject data)
        {
            if (_spawnedViews.ContainsKey(player)) return;

            PlayerRoom view = Instantiate(_playerRoom, _playerParent);
            bool isLocal = player == _networkService.Runner.LocalPlayer;

            view.Bind(data, isLocal);

            if (isLocal && !data.IsHost)
            {
                view.PressedCallback += OnLocalPlayerTogglePressed;
            }

            _spawnedViews[player] = view;
        }

        private void HandlePlayerJoined(PlayerRef player)
        {
            if (_isHost && _networkService.Runner.GetPlayerObject(player) == null)
            {
                SpawnPlayerObject(player, isHost: false);
            }

            UpdatePlayerCountText();
        }

        private void HandlePlayerLeft(PlayerRef player)
        {
            if (_spawnedViews.TryGetValue(player, out PlayerRoom view))
            {
                Destroy(view.gameObject);
                _spawnedViews.Remove(player);
            }

            UpdatePlayerCountText();
        }

        private bool AreAllClientsReady()
        {
            var clients = _networkService.Runner.ActivePlayers
                .Select(p => _networkService.Runner.GetPlayerObject(p))
                .Where(o => o != null)
                .Select(o => o.GetComponent<RoomPlayerNetworkObject>())
                .Where(d => !d.IsHost)
                .ToList();

            return clients.Count > 0 && clients.All(d => d.IsReady);
        }

        // ---------------- READY / START ----------------

        private void OnLocalPlayerTogglePressed()
        {
            NetworkObject myObj = _networkService.Runner.GetPlayerObject(_networkService.Runner.LocalPlayer);
            var data = myObj?.GetComponent<RoomPlayerNetworkObject>();
            if (data == null) return;

            data.RPC_SetReady(!data.IsReady);
        }

        private async void OnClickReadyOrStart()
        {
            if (_isHost)
            {
                _readyStartButton.interactable = false;
                await _sceneManager.LoadSceneAsync<LevelScene>(nameof(LevelScene));
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
        }

        private void TrySubscribeChatRelay()
        {
            if (RoomChatRelay.Instance != null)
            {
                RoomChatRelay.Instance.MessageReceived += HandleChatMessageReceived;
            }
            else
            {
                // Client vào trước khi Host kịp Spawn relay - thử lại sau 1 khoảng ngắn.
                Invoke(nameof(TrySubscribeChatRelay), 0.5f);
            }
        }

        private void OnClickSendChat()
        {
            string message = _chatInput.text?.Trim();
            if (string.IsNullOrEmpty(message) || RoomChatRelay.Instance == null) return;

            NetworkObject myObj = _networkService.Runner.GetPlayerObject(_networkService.Runner.LocalPlayer);
            string myName = myObj?.GetComponent<RoomPlayerNetworkObject>()?.PlayerName.ToString() ?? "???";

            RoomChatRelay.Instance.RPC_SendMessage(myName, message);
            _chatInput.text = "";
        }

        private void HandleChatMessageReceived(string playerName, string message)
        {
            // TODO: cần 1 chat message prefab (TMP Text) để Instantiate vào _chatParent -
            // hiện tại field _chatParent chỉ là Transform container, chưa có prefab dòng chat cụ thể.
            Debug.Log($"[Chat] {playerName}: {message}");
        }
    }
}