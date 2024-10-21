using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    public PlayerCharacterSO selectedCharacterSO;

    // 캐릭터 이름과 UI 이미지 매핑을 위한 딕셔너리
    public Dictionary<string, Image> characterImageMap;

    public List<CharacterImagePair> characterImagePairs;

    public Transform usingText_Transform;

    private void Start()
    {
        // 딕셔너리 초기화
        characterImageMap = new Dictionary<string, Image>();

        // 리스트를 딕셔너리로 변환
        foreach (var pair in characterImagePairs)
        {
            characterImageMap.Add(pair.characterName, pair.characterImage);
        }

        // 저장된 캐릭터 이름을 가져와 선택된 캐릭터를 설정
        if (PlayerPrefs.HasKey("SelectedCharacter"))
        {
            string savedCharacterName = PlayerPrefs.GetString("SelectedCharacter");
            selectedCharacterSO.characterName = savedCharacterName;
            UpdateCharacterUI(savedCharacterName);
        }
    }

    public void SelectCharacter(string characterName)
    {
        selectedCharacterSO.characterName = characterName;

        PlayerPrefs.SetString("SelectedCharacter", characterName);
        PlayerPrefs.Save();

        Debug.Log($"Character {characterName} has been selected and saved.");

        // 캐릭터 선택 시 UI 업데이트
        UpdateCharacterUI(characterName);
    }

    public string GetSelectedCharacter()
    {
        return selectedCharacterSO.characterName;
    }

    private void UpdateCharacterUI(string selectedCharacterName)
    {
        foreach (var pair in characterImageMap)
        {
            Color color = pair.Value.color;

            if (pair.Key == selectedCharacterName)
            {
                // 선택된 캐릭터의 이미지를 흐리게 (알파값을 낮춤)
                color.a = 0.5f; // 흐리게 하기 위해 알파값을 0.5로 설정
                usingText_Transform.position = pair.Value.transform.position;
            }
            else
            {
                // 선택되지 않은 캐릭터는 정상적인 알파값 유지
                color.a = 1.0f;
            }

            pair.Value.color = color;
        }
    }
}
