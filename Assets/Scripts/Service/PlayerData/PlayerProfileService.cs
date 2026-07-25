using System;
using QuiChuong2005.Framework.Services.Data;
using UnityEngine;

namespace Chronex.Services.Profile
{
    public sealed class PlayerProfileService : IPlayerProfileService
    {
        private readonly IDataService _dataService;
        private PlayerProfileData _data;

        public string PlayerName => _data.PlayerName;
        public int Level => _data.Level;
        public int CurrentExp => _data.CurrentExp;
        public int RequiredExp => _data.RequiredExp;

        public event Action ProfileChanged;

        public PlayerProfileService(IDataService dataService)
        {
            _dataService = dataService;

            _data = _dataService.HasKey(DataKeys.Player) // ĐỔI: dùng DataKeys.Player thay vì string tự bịa
                ? _dataService.Get(DataKeys.Player, CreateDefault())
                : CreateDefault();

            if (!_dataService.HasKey(DataKeys.Player)) // ĐỔI
            {
                _dataService.Set(DataKeys.Player, _data); // ĐỔI
            }
        }

        public void SetPlayerName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            _data.PlayerName = name.Trim();
            _dataService.Set(DataKeys.Player, _data); // ĐỔI

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
                _data.RequiredExp = Mathf.RoundToInt(_data.RequiredExp * 1.2f); // TODO: chỉnh công thức lên cấp theo thiết kế thật.
            }

            _dataService.Set(DataKeys.Player, _data); // ĐỔI
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
    }
}