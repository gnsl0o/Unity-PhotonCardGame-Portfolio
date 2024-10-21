using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    public PlayerCharacterSO selectedCharacterSO;

    // ĳ���� �̸��� UI �̹��� ������ ���� ��ųʸ�
    public Dictionary<string, Image> characterImageMap;

    public List<CharacterImagePair> characterImagePairs;

    public Transform usingText_Transform;

    private void Start()
    {
        // ��ųʸ� �ʱ�ȭ
        characterImageMap = new Dictionary<string, Image>();

        // ����Ʈ�� ��ųʸ��� ��ȯ
        foreach (var pair in characterImagePairs)
        {
            characterImageMap.Add(pair.characterName, pair.characterImage);
        }

        // ����� ĳ���� �̸��� ������ ���õ� ĳ���͸� ����
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

        // ĳ���� ���� �� UI ������Ʈ
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
                // ���õ� ĳ������ �̹����� �帮�� (���İ��� ����)
                color.a = 0.5f; // �帮�� �ϱ� ���� ���İ��� 0.5�� ����
                usingText_Transform.position = pair.Value.transform.position;
            }
            else
            {
                // ���õ��� ���� ĳ���ʹ� �������� ���İ� ����
                color.a = 1.0f;
            }

            pair.Value.color = color;
        }
    }
}
