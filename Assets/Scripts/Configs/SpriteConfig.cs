using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcshot
{
    [CreateAssetMenu(menuName = "ArcShot/Configs/SpriteConfig", fileName = "SpriteConfig")]
    public class SpriteConfig : ScriptableObject
    {
        [Serializable]
        public class CharacterSpriteConfig
        {
            public CharacterSkin Skin;
            public Sprite CharacterSprite;
            public Sprite TireSprite;
            public Sprite CannonSprite;
        }

        public List<CharacterSpriteConfig> characterConfig = new();
    }
}