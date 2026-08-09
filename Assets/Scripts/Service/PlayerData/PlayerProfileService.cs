using System;
using QuiChuong2005.Framework.Services.Data;
using UnityEngine;

namespace Chronex.Services.Profile
{
    public sealed class PlayerProfileService : IPlayerProfileService
    {
        private readonly IDataService _dataService;
        private readonly string _dataKey;
        private PlayerProfileData _data;
        public Arcshot.CharacterSkin Skin => _data.Skin;

        public string PlayerName => _data.PlayerName;
        public int Level => _data.Level;
        public int CurrentExp => _data.CurrentExp;
        public int RequiredExp => _data.RequiredExp;

        public event Action ProfileChanged;

        public PlayerProfileService(IDataService dataService, string userId = null)
        {
            _dataService = dataService;

            if (_dataService == null)
            {
                Debug.LogError("DataService is null");
            }

            // Nếu có userId -> dùng key riêng theo từng player để tránh đè dữ liệu
            // Nếu không có userId -> dùng key cũ để backward compatible
            _dataKey = string.IsNullOrEmpty(userId)
                ? DataKeys.Player
                : $"{DataKeys.Player}_{userId}";

            // Migration: nếu key mới chưa có dữ liệu nhưng key cũ có -> chuyển sang key mới
            if (!_dataService.HasKey(_dataKey) && _dataService.HasKey(DataKeys.Player))
            {
                _data = _dataService.Get(DataKeys.Player, CreateDefault());
                _dataService.Set(_dataKey, _data);
                _dataService.Delete(DataKeys.Player);
            }
            else
            {
                _data = _dataService.Get(_dataKey, CreateDefault());

                if (!_dataService.HasKey(_dataKey))
                {
                    _dataService.Set(_dataKey, _data);
                }
            }
        }

        public void SetPlayerName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            _data.PlayerName = name.Trim();
            _dataService.Set(_dataKey, _data);

            ProfileChanged?.Invoke();
        }

        public void AddExp(int amount)
        {
            if (amount <= 0) return;

            _data.CurrentExp += amount;

            while (_data.CurrentExp >= _data.RequiredExp)
            {
                _data.CurrentExp -= _data.RequiredExp;
                _data.Level++;
                _data.RequiredExp =
                    Mathf.RoundToInt(_data.RequiredExp * 1.2f); // TODO: chỉnh công thức lên cấp theo thiết kế thật.
            }

            _dataService.Set(_dataKey, _data);
            ProfileChanged?.Invoke();
        }

        private static PlayerProfileData CreateDefault()
        {
            return new PlayerProfileData
            {
                PlayerName = $"Player{UnityEngine.Random.Range(1000, 9999)}",
                Level = 1,
                CurrentExp = 0,
                RequiredExp = 100
            };
        }

        public void SetSkin(Arcshot.CharacterSkin skin)
        {
            _data.Skin = skin;
            _dataService.Set(_dataKey, _data);

            ProfileChanged?.Invoke();
        }
    }
}
