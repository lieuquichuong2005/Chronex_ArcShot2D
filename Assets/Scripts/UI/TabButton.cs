using System;
using System.Collections.Generic;
using EditorAttributes;
using QuiChuong2005;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arcshot
{
    public enum MenuTab
    {
        Home,
        Friends,
        Clan,
        Chat,
        Ranking,
        Inventory,
        Loadout,
        Shop,
        LogOut,
        Quit,
    }

    public class TabButton : MonoBehaviour
    {
        [Inject]
        private IAudioService _audioService;

        [Serializable]
        public class ButtonConfig
        {
            public MenuTab Tab;
            public Sprite Icon;
        }

        [SerializeField]
        private MenuTab _tab;

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TextMeshProUGUI _title;

        [SerializeField]
        private Image _selectedImage;

        [SerializeField]
        private bool _isReleased;

        [SerializeField]
        private List<ButtonConfig> _configs = new();

        private bool _isActivate;
        private bool _isLocked;

        public Action<MenuTab> OnClick;

        public bool IsActivate
        {
            get => _isActivate;
            set
            {
                if (_isActivate == value)
                {
                    return;
                }

                _isActivate = value;
                SetSelectState();
            }
        }


        private void Awake()
        {
            ServiceLocator.Instance.Resolve(this);

            _icon.sprite = GetIcon(_tab);
            _title.text = $"{_tab.ToString().ToUpper()}";
            IsActivate = false;
        }

        public void OnPressed()
        {
            Debug.Log($"OnPressed");
            _audioService.PlaySfx(Audio.SFX_Click);

            // if (!_isReleased)
            // {
            //     Debug.Log($"Coming Soon ...");
            //     return;
            // }
            //
            // if (_isLocked)
            // {
            //     Debug.Log($"Tab Is Being Locked.");
            //     return;
            // }

            OnClick?.Invoke(_tab);
        }

        private void SetSelectState()
        {
            _selectedImage.gameObject.SetActive(IsActivate);
        }

        private Sprite GetIcon(MenuTab tab)
        {
            foreach (var config in _configs)
            {
                if (config.Tab == tab)
                {
                    return config.Icon;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        [Button]
        public void AutoCreateConfigs()
        {
            _configs.Clear();
            foreach (MenuTab tab in Enum.GetValues(typeof(MenuTab)))
            {
                _configs.Add(new ButtonConfig()
                {
                    Tab = tab,
                    Icon = null,
                });
            }
        }
#endif
    }
}