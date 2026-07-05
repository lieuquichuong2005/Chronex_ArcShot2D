using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterConfigManager", menuName = "ArcShot2D/Manager/PlayerConfigManager")]
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