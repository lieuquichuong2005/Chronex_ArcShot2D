using System;
using System.Collections.Generic;
using Arcshot;
using Chronex.Services.Profile;
using Chronex.UI.Room;
using Photon.Realtime;
using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Core.DI;
using QuiChuong2005.Framework.Services.Data;
using QuiChuong2005.Framework.Services.Scenes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuScene : MonoBehaviour
{
    [Serializable]
    public class MenuTabPanel
    {
        public MenuTab tabKind;
        public Arcshot.TabButton tabButton;
        public GameObject tabPanel;
    }

    [Inject]
    private Chronex.Services.Networking.INetworkService _networkService;

    [Inject]
    private IDataService _dataService;

    [Inject]
    private ISceneService _sceneService;

    private IPlayerProfileService _playerProfileService;

    [SerializeField]
    private List<MenuTabPanel> _tabConfigs = new();

    [Header("Room")]
    [SerializeField]
    private Button _createRoomButton;

    [SerializeField]
    private Button _quickMatchButton;

    [SerializeField]
    private Button _joinRoomButton;

    [SerializeField]
    private JoinRoomDialog _joinRoomDialog;

    [Space(5)]
    [Header("UI REFERENCES")]
    [SerializeField]
    private Image _avatarImage;

    [SerializeField]
    private TextMeshProUGUI _playerNameText;

    [SerializeField]
    private Button _changeNameButton;

    [SerializeField]
    private TMP_InputField _playerNameInput;

    [SerializeField]
    private Button _completedChangeNameButton;

    [SerializeField]
    private TextMeshProUGUI _levelText;

    [SerializeField]
    private TextMeshProUGUI _levelProcessText;

    [SerializeField]
    private Image _expProcessBar;

    [SerializeField]
    private Image _rankIcon;

    [SerializeField]
    private TextMeshProUGUI _rankText;

    private MenuTabPanel _currentTab;
    private bool _isConnecting;

    private void Awake()
    {
        RegisterNetworkServiceIfNeeded();
        ServiceLocator.Instance.Resolve(this);

        InitTabButton();

        _currentTab = _tabConfigs.Find(x => x.tabKind == MenuTab.Home);
        _currentTab.tabButton.IsActivate = true;
        SetTab(MenuTab.Home);

        RegisterPlayerProfileServiceIfNeeded(); // THÊM
    }

    private void Start()
    {
        _createRoomButton.onClick.AddListener(OnClickCreateRoom);
        _quickMatchButton.onClick.AddListener(OnClickQuickMatch);
        _joinRoomButton.onClick.AddListener(OnClickJoinRoom);

        _joinRoomDialog.OnJoinRoom += JoinRoomById;

        InitPlayerProfile();
    }

    private void OnDestroy()
    {
        if (_joinRoomDialog != null)
            _joinRoomDialog.OnJoinRoom -= JoinRoomById;

        if (_playerProfileService != null) // THÊM
            _playerProfileService.ProfileChanged -= RefreshProfileDisplay;
    }

    // ---------------- PLAYER PROFILE (Name / Level / Exp) ----------------

    private void InitPlayerProfile()
    {
        _playerProfileService.ProfileChanged += RefreshProfileDisplay;

        RefreshProfileDisplay();

        _changeNameButton.onClick.AddListener(OnClickChangeName);
        _completedChangeNameButton.onClick.AddListener(OnClickCompleteChangeName);

        SetNameEditMode(false);
    }

    private void RefreshProfileDisplay()
    {
        _playerNameText.text = _playerProfileService.PlayerName;

        _levelText.text = $"Level {_playerProfileService.Level}";
        _levelProcessText.text = $"{_playerProfileService.CurrentExp}/{_playerProfileService.RequiredExp}";
        _expProcessBar.fillAmount = _playerProfileService.RequiredExp > 0
            ? Mathf.Clamp01((float)_playerProfileService.CurrentExp / _playerProfileService.RequiredExp)
            : 0f;

        // TODO: Rank icon/text chưa có hệ thống - giữ nguyên giá trị mặc định gán sẵn trong Inspector.
    }

    private void OnClickChangeName()
    {
        _playerNameInput.text = _playerProfileService.PlayerName;
        SetNameEditMode(true);

        _playerNameInput.Select();
        _playerNameInput.ActivateInputField();
    }

    private void OnClickCompleteChangeName()
    {
        string newName = _playerNameInput.text.Trim();

        if (!string.IsNullOrEmpty(newName))
        {
            _playerProfileService.SetPlayerName(newName); // RefreshProfileDisplay tự chạy qua event ProfileChanged
        }

        SetNameEditMode(false);
    }

    private void SetNameEditMode(bool isEditing)
    {
        _playerNameText.gameObject.SetActive(!isEditing);
        _changeNameButton.gameObject.SetActive(!isEditing);

        _playerNameInput.gameObject.SetActive(isEditing);
        _completedChangeNameButton.gameObject.SetActive(isEditing);
    }

    private void RegisterPlayerProfileServiceIfNeeded() // THÊM
    {
        var locator = ServiceLocator.Instance;

        if (_dataService == null)
        {
            Debug.LogError("DataService is null");
        }

        try
        {
            _playerProfileService = locator.Get<IPlayerProfileService>();
        }
        catch (InvalidOperationException)
        {
            locator.Register<IPlayerProfileService>(new PlayerProfileService(_dataService));
        }
    }

    // ---------------- ROOM (giữ nguyên, không đổi) ----------------

    private async void OnClickCreateRoom()
    {
        if (_isConnecting) return;
        _isConnecting = true;
        SetRoomButtonsInteractable(false);

        try
        {
            string code = await _networkService.CreateRoomAsync(maxPlayers: 4);
            Debug.Log($"[Menu] Tạo phòng thành công, code: {code}");

            await _sceneService.LoadSceneAsync<RoomScene>(nameof(RoomScene));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Menu] Tạo phòng lỗi: {ex.Message}");
            _isConnecting = false;
            SetRoomButtonsInteractable(true);
        }
    }


    private async void OnClickQuickMatch()
    {
        if (_isConnecting) return;
        _isConnecting = true;
        SetRoomButtonsInteractable(false);

        try
        {
            await _networkService.QuickMatchAsync(maxPlayers: 4);
            Debug.Log("[Menu] Quick match thành công.");

            await _sceneService.LoadSceneAsync<RoomScene>(nameof(RoomScene));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Menu] Quick match lỗi: {ex.Message}");
            _isConnecting = false;
            SetRoomButtonsInteractable(true);
        }
    }

    private void OnClickJoinRoom()
    {
        if (_isConnecting)
            return;

        _joinRoomDialog.Show();
    }

    private void SetRoomButtonsInteractable(bool interactable)
    {
        _createRoomButton.interactable = interactable;
        _quickMatchButton.interactable = interactable;
    }

    private void RegisterNetworkServiceIfNeeded()
    {
        var locator = QuiChuong2005.Framework.Core.ServiceLocator.Instance;

        try
        {
            locator.Get<Chronex.Services.Networking.INetworkService>();
        }
        catch (InvalidOperationException)
        {
            locator.Register<Chronex.Services.Networking.INetworkService>(
                new Chronex.Services.Networking.NetworkService());
        }
    }

    private void InitTabButton()
    {
        foreach (var config in _tabConfigs)
            config.tabButton.OnClick += tab =>
            {
                if (_currentTab == config)
                    return;

                _currentTab.tabButton.IsActivate = false;

                config.tabButton.IsActivate = true;
                _currentTab = config;

                SetTab(tab);
            };
    }

    private void SetTab(MenuTab tab)
    {
        foreach (var tabConfig in _tabConfigs) tabConfig.tabPanel.SetActive(tabConfig.tabKind == tab);
    }

    private async void JoinRoomById(string roomId)
    {
        if (_isConnecting)
            return;

        _isConnecting = true;
        SetRoomButtonsInteractable(false);

        try
        {
            await _networkService.JoinRoomByCodeAsync(roomId);

            Debug.Log($"[Menu] Join room success: {roomId}");

            _joinRoomDialog.Hide();

            await _sceneService.LoadSceneAsync<RoomScene>(nameof(RoomScene));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Menu] Join room failed: {ex.Message}");

            _isConnecting = false;
            SetRoomButtonsInteractable(true);
        }
    }
}