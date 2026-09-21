using System;
using System.Collections.Generic;
using Arcshot;
using Chronex.Services;
using Chronex.Services.Profile;
using Chronex.UI.Room;
using Cysharp.Threading.Tasks;
using Photon.Realtime;
using QuiChuong2005;
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
    private IAudioService _audioService;

    [Inject]
    private Chronex.Services.Networking.INetworkService _networkService;

    [Inject]
    private IDataService _dataService;

    [Inject]
    private ISceneService _sceneService;

    [Inject]
    private AuthenticationService _authService;

    [Inject]
    private IDialogService _dialogService;

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
    private bool _isLoggingOut;

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
        _audioService.PlayMusicAsync(Audio.MenuScene);
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
        _audioService.PlaySfx(Audio.SFX_Click);
        _playerNameInput.text = _playerProfileService.PlayerName;
        SetNameEditMode(true);

        _playerNameInput.Select();
        _playerNameInput.ActivateInputField();
    }

    private void OnClickCompleteChangeName()
    {
        _audioService.PlaySfx(Audio.SFX_Click);
        var newName = _playerNameInput.text.Trim();

        if (!string.IsNullOrEmpty(newName))
            _playerProfileService.SetPlayerName(newName); // RefreshProfileDisplay tự chạy qua event ProfileChanged

        SetNameEditMode(false);
    }

    private void SetNameEditMode(bool isEditing)
    {
        _playerNameText.gameObject.SetActive(!isEditing);
        _changeNameButton.gameObject.SetActive(!isEditing);

        _playerNameInput.gameObject.SetActive(isEditing);
        _completedChangeNameButton.gameObject.SetActive(isEditing);
    }

    private void RegisterPlayerProfileServiceIfNeeded()
    {
        var locator = ServiceLocator.Instance;

        if (_dataService == null) Debug.LogError("DataService is null");

        try
        {
            _playerProfileService = locator.Get<IPlayerProfileService>();
        }
        catch (InvalidOperationException)
        {
            string userId = _authService?.GetCurrentUser()?.UserId;
            locator.Register<IPlayerProfileService>(new PlayerProfileService(_dataService, userId));
        }
    }

    // ---------------- ROOM (giữ nguyên, không đổi) ----------------

    private async void OnClickCreateRoom()
    {
        _audioService.PlaySfx(Audio.SFX_Click);
        if (_isConnecting) return;
        _isConnecting = true;
        SetRoomButtonsInteractable(false);

        try
        {
            var code = await _networkService.CreateRoomAsync(4);
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
        _audioService.PlaySfx(Audio.SFX_Click);
        if (_isConnecting) return;
        _isConnecting = true;
        SetRoomButtonsInteractable(false);

        try
        {
            await _networkService.QuickMatchAsync(4);
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
        _audioService.PlaySfx(Audio.SFX_Click);
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
        var locator = ServiceLocator.Instance;

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
                if (config.tabKind == MenuTab.LogOut)
                {
                    OnLogOutButtonPressed();
                    return;
                }

                if (config.tabKind == MenuTab.Quit)
                {
                    OnQuitButtonPressed();
                    return;
                }

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
        foreach (var tabConfig in _tabConfigs)
        {
            if (tabConfig.tabPanel == null) continue;

            tabConfig.tabPanel.SetActive(tabConfig.tabKind == tab);
        }
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

    private void OnLogOutButtonPressed()
    {
        _audioService.PlaySfx(Audio.SFX_Click);
        if (_isLoggingOut) return;
        _isLoggingOut = true;

        UserServiceRegister.Unregister(); // gỡ + Save() service của user hiện tại
        _authService?.Logout();
        _sceneService.LoadSceneAsync<LogInScene>(nameof(LogInScene)).Forget();
    }

    private void OnQuitButtonPressed()
    {
        _audioService.PlaySfx(Audio.SFX_Click);
        Application.Quit();
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