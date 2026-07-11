using System;
using System.Collections.Generic;
using EditorAttributes;
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
    }

    public class TabButton : MonoBehaviour
    {
        [Serializable]
        public class ButtonConfig
        {
            public MenuTab Tab;
            public Sprite Icon;
        }

        [SerializeField]
        private MenuTab _tab;

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

        public Action OnClick;


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
            _selectedImage.sprite = GetIcon(_tab);
            _title.text = $"{_tab.ToString().ToUpper()}";
            IsActivate = false;
        }

        public void OnPressed()
        {
            if (!_isReleased)
            {
                Debug.Log($"Coming Soon ...");
                return;
            }

            if (_isLocked)
            {
                Debug.Log($"Tab Is Being Locked.");
                return;
            }

            OnClick?.Invoke();
        }

        private void SetSelectState()
        {
            _selectedImage.enabled = _isActivate;
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