using System;
using System.Collections.Generic;
using Arcshot;
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