using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

namespace Arcshot
{
    public enum CharacterSkin
    {
        Blue,
        Red,
        Purple,
    }

    [CreateAssetMenu(fileName = "CharacterConfigManager", menuName = "ArcShot/Manager/PlayerConfigManager")]
    public class CharacterManager : ScriptableObject
    {
        public List<GameObject> Characters = new();

        public GameObject GetRandomCharacter()
        {
            return Characters[UnityEngine.Random.Range(0, Characters.Count)];
        }

        [Button]
        public void ClearCharacters()
        {
            Characters.Clear();
        }
    }
}