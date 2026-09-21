using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuiChuong2005.Arcshot
{
    public enum CurrencyKind
    {
        Coin,
        Gem,
    }


    public class Currency : MonoBehaviour
    {
        [Serializable]
        public class CurrencyInfo
        {
            public CurrencyKind Kind;
            public Sprite Icons;
        }

        [SerializeField]
        private List<CurrencyInfo> _configs = new();

        [SerializeField]
        private CurrencyKind _kind;

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TextMeshProUGUI _balanceText;

        private int _balance;

        public Action<int> OnBalanceChanged;

        public int Balance
        {
            get => _balance;
            set
            {
                _balance = value;
                _balanceText.text = _balance.ToString();
                OnBalanceChanged?.Invoke(_balance);
            }
        }

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            foreach (var item in _configs)
            {
                if (item.Kind == _kind)
                {
                    _icon.sprite = item.Icons;
                    Balance = AppConfig.CurrencyDefault;
                }
            }
        }
    }
}