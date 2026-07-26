using System;

namespace Chronex.Services.Profile
{
    public interface IPlayerProfileService
    {
        string PlayerName { get; }
        int Level { get; }
        int CurrentExp { get; }
        int RequiredExp { get; }
        Arcshot.CharacterSkin Skin { get; } 

        event Action ProfileChanged;

        void SetPlayerName(string name);
        void AddExp(int amount);
        void SetSkin(Arcshot.CharacterSkin skin); 
    }
}