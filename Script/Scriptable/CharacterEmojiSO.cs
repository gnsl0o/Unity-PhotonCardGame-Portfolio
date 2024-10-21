using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterEmotes", menuName = "ScriptableObjects/CharacterEmotes", order = 1)]
public class CharacterEmojiSO : ScriptableObject
{
    public string characterName;
    public Sprite characterImage;
    public List<Sprite> emojiSprites;
}
