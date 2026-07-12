using System;
using System.Collections.Generic;
using Arcshot;
using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Core.DI;
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
        try
        {
            string code = await _networkService.CreateRoomAsync(maxPlayers: 4);
            Debug.Log($"[Menu] Tạo phòng thành công, code: {code}");
            // TODO: chuyển sang LobbyScene khi class đó xong
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Menu] Tạo phòng lỗi: {ex.Message}");
        }
    }

    private async void OnClickQuickMatch()
    {
        try
        {
            await _networkService.QuickMatchAsync(maxPlayers: 4);
            Debug.Log("[Menu] Quick match thành công.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Menu] Quick match lỗi: {ex.Message}");
        }
    }

    private void OnClickJoinRoom()
    {
        // TODO: mở JoinRoomDialog khi class đó xong
        Debug.Log("[Menu] Mở dialog Join Room (chưa implement).");
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