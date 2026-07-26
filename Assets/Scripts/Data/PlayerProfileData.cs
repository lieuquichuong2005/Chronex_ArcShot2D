using System;

namespace Chronex.Services.Profile
{
    [Serializable]
    public sealed class PlayerProfileData
    {
        public string PlayerName;
        public int Level = 1;
        public int CurrentExp;
        public int RequiredExp = 100;
        public Arcshot.CharacterSkin Skin;
    }
}