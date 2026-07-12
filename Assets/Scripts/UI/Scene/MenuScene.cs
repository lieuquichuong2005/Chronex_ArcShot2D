using System;
using System.Collections.Generic;
using Arcshot;
using Chronex.UI.Room;
using Photon.Realtime;
using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Core.DI;
using QuiChuong2005.Framework.Services.Scenes;
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
    private ISceneService _sceneService;

    [SerializeField]
    private List<MenuTabPanel> _tabConfigs = new();

    [Header("Room")]
    [SerializeField]
    private Button _createRoomButton;

    [SerializeField]
    private Button _quickMatchButton;

    [SerializeField]
    private Button _joinRoomButton;

    private MenuTabPanel _currentTab;
    private bool _isConnecting;

    private void Awake()
    {
        InitTabButton();

        _currentTab = _tabConfigs.Find(x => x.tabKind == MenuTab.Home);
        _currentTab.tabButton.IsActivate = true;
        SetTab(MenuTab.Home);

        RegisterNetworkServiceIfNeeded();
    }

    private void Start()
    {
        ServiceLocator.Instance.Resolve(this);

        _createRoomButton.onClick.AddListener(OnClickCreateRoom);
        _quickMatchButton.onClick.AddListener(OnClickQuickMatch);
        _joinRoomButton.onClick.AddListener(OnClickJoinRoom);
    }

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
        Debug.Log("Comming Soon");
        // TODO: mở LobbyScene khi class đó xong
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
}