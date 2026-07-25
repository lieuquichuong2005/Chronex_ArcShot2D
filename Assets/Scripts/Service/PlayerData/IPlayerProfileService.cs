using System;

namespace Chronex.Services.Profile
{
    public interface IPlayerProfileService
    {
        string PlayerName { get; }
        int Level { get; }
        int CurrentExp { get; }
        int RequiredExp { get; }

        event Action ProfileChanged;

        void SetPlayerName(string name);
        void AddExp(int amount);
    }
}