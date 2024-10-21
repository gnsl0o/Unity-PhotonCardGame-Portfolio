using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using UnityEngine.UI;

public class Emoji : MonoBehaviourPunCallbacks
{
    public GameObject emojis;

    public RectTransform[] emojisTransform;
    public RectTransform button;
    public Vector2[] targetPositions;
    public float duration = 0.5f;

    public PlayerCharacterSO currentCharacter;
    public CharacterEmojiSO[] allCharacterEmojis;
    public UnityEngine.UI.Image[] characterImage;
    public UnityEngine.UI.Image[] playerEmojiImages;    // �÷��̾� �̸�Ƽ�� �迭
    public UnityEngine.UI.Image[] opponentEmojiImages;  // ���� �̸�Ƽ�� �迭

    public Image playerActivatedEmoji;
    public Image opponentActivatedEmoji;

    public RectTransform  playerEmojiBoxTransform;
    public RectTransform playerSpeechBubbleTransform;  
    public Image playerSpeechBubbleImage;

    public RectTransform opponentEmojiBoxTransform;
    public RectTransform opponentSpeechBubbleTransform;
    public Image opponentSpeechBubbleImage;

    private Coroutine playerHideCoroutine; // �÷��̾��� Coroutine�� �����ϱ� ���� ����
    private Coroutine opponentHideCoroutine; // ������ Coroutine�� �����ϱ� ���� ����

    private void Start()
    {
        // �ʱ� ��ġ�� ��ư�� ��ġ�� ����
        for (int i = 0; i < emojisTransform.Length; i++)
        {
            emojisTransform[i].position = button.position;
        }

        UpdateEmojiSprites();

        photonView.RPC("SendCharacterInfo", RpcTarget.Others, currentCharacter.characterName);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // ���� ���� �÷��̾�� �� ĳ���� ������ �����ϴ�.
        photonView.RPC("SendCharacterInfo", newPlayer, currentCharacter.characterName);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        characterImage[1].gameObject.SetActive(false);
    }

    public void OnEmojiIconClicked()
    {
        emojis.SetActive(true);

        // �̸�Ƽ�ܵ��� ���������� �ִϸ��̼� ����
        for (int i = 0; i < emojisTransform.Length; i++)
        {
            // ���� �ִϸ��̼� �ߴ�
            emojisTransform[i].DOKill();

            // ��ġ �̵� �ִϸ��̼�
            emojisTransform[i].DOAnchorPos(targetPositions[i], duration).SetEase(Ease.OutBack).SetDelay(i * 0.05f);

            // ������ �ִϸ��̼�: 0���� 1�� ���������� Ŀ������ ����
            emojisTransform[i].localScale = Vector3.zero;  // �ʱ� �������� 0���� ����
            emojisTransform[i].DOScale(Vector3.one, duration).SetEase(Ease.OutBack).SetDelay(i * 0.05f);
        }
    }

    public void OnEmojiButtonClicked(int emojiIndex)
    {
        // �ڽſ��� �̸�Ƽ�� ǥ��
        DisplayEmoji(playerEmojiImages, emojiIndex, true);

        emojis.SetActive(false);

        // �ʱ� ��ġ�� ��ư�� ��ġ�� ����
        for (int i = 0; i < emojisTransform.Length; i++)
        {
            emojisTransform[i].position = button.position;
        }

        // ���濡�� �̸�Ƽ�� ������ ����
        photonView.RPC("ReceiveEmoji", RpcTarget.Others, emojiIndex);
    }

    [PunRPC]
    public void ReceiveEmoji(int emojiIndex)
    {
        DisplayEmoji(opponentEmojiImages, emojiIndex, false);
    }

    [PunRPC]
    public void SendCharacterInfo(string characterName)
    {
        UpdateOpponentCharacterInfo(characterName);
    }

    public void UpdateEmojiSprites()
    {
        CharacterEmojiSO characterEmojis = allCharacterEmojis
            .FirstOrDefault(emojis => emojis.characterName == currentCharacter.characterName);

        if (characterEmojis != null)
        {
            characterImage[0].sprite = characterEmojis.characterImage;

            for (int i = 0; i < playerEmojiImages.Length; i++)
            {
                if (i < characterEmojis.emojiSprites.Count)
                {
                    playerEmojiImages[i].sprite = characterEmojis.emojiSprites[i];
                }
            }
        }
        else
        {
            Debug.Log("�̹����� ã�� �� ����");
        }
    }

    public void UpdateOpponentCharacterInfo(string characterName)
    {
        CharacterEmojiSO opponentCharacterEmojis = allCharacterEmojis
        .FirstOrDefault(emojis => emojis.characterName == characterName);

        if (opponentCharacterEmojis != null)
        {
            characterImage[1].sprite = opponentCharacterEmojis.characterImage;
            characterImage[1].gameObject.SetActive(true);

            for (int i = 0; i < opponentEmojiImages.Length; i++)
            {
                if (i < opponentCharacterEmojis.emojiSprites.Count)
                {
                    opponentEmojiImages[i].sprite = opponentCharacterEmojis.emojiSprites[i];
                }
            }
        }
    }

    private void DisplayEmoji(UnityEngine.UI.Image[] emojiImagesArray, int emojiIndex, bool isPlayer)
    {
        if (emojiIndex >= 0 && emojiIndex < emojiImagesArray.Length)
        {
            if (isPlayer)
            {
                playerActivatedEmoji.sprite = emojiImagesArray[emojiIndex].sprite;
                ShowSpeechBubble(true); // �÷��̾��� ��ǳ�� ǥ��

                // ������ ���� ���� �÷��̾��� Coroutine�� ������ ����
                if (playerHideCoroutine != null)
                {
                    StopCoroutine(playerHideCoroutine);
                }

                // ���Ӱ� HideSpeechBubble�� ����
                playerHideCoroutine = StartCoroutine(HideSpeechBubbleAfterDelay(2f, true));
            }
            else
            {
                opponentActivatedEmoji.sprite = emojiImagesArray[emojiIndex].sprite;
                ShowSpeechBubble(false); // ������ ��ǳ�� ǥ��

                // ������ ���� ���� ������ Coroutine�� ������ ����
                if (opponentHideCoroutine != null)
                {
                    StopCoroutine(opponentHideCoroutine);
                }

                // ���Ӱ� HideSpeechBubble�� ����
                opponentHideCoroutine = StartCoroutine(HideSpeechBubbleAfterDelay(2f, false));
            }
        }
    }

    public void ShowSpeechBubble(bool isPlayer)
    {
        RectTransform targetSpeechBubbleTransform = isPlayer ? playerSpeechBubbleTransform : opponentSpeechBubbleTransform;
        Image targetSpeechBubbleImage = isPlayer ? playerSpeechBubbleImage : opponentSpeechBubbleImage;
        RectTransform targetEmojiBoxTransform = isPlayer ? playerEmojiBoxTransform : opponentEmojiBoxTransform;

        // �ִϸ��̼��� �����ϱ� ���� ������ �ִϸ��̼��� ����
        targetSpeechBubbleTransform.DOKill();
        targetSpeechBubbleImage.DOKill();

        // 1. ��ǳ���� �ʱ� ��ġ�� ĳ���� �̹��� ���� ����
        targetSpeechBubbleTransform.position = targetEmojiBoxTransform.position;

        // 2. ��ǳ�� �̹����� ũ��� ���� �ʱ�ȭ
        targetSpeechBubbleTransform.localScale = Vector3.zero; // ũ�⸦ 0���� ����
        targetSpeechBubbleImage.color = new Color(1, 1, 1, 0); // �����ϰ� ����

        // 3. DoTween �������� ���� �ִϸ��̼� ����
        Sequence bubbleSequence = DOTween.Sequence();

        // ũ��� ���� �ִϸ��̼�
        if (isPlayer)
        {
            bubbleSequence.Append(targetSpeechBubbleTransform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack)); // ũ�� Ȯ��
        }
        else
        {
            bubbleSequence.Append(targetSpeechBubbleTransform.DOScale(new Vector3(-1,-1,0), 0.5f).SetEase(Ease.OutBack)); // ũ�� Ȯ��
        }
        bubbleSequence.Join(targetSpeechBubbleImage.DOFade(1f, 0.5f)); // ���� ����
    }

    private IEnumerator HideSpeechBubbleAfterDelay(float delay, bool isPlayer)
    {
        yield return new WaitForSeconds(delay);
        HideSpeechBubble(isPlayer);
    }

    public void HideSpeechBubble(bool isPlayer)
    {
        RectTransform targetSpeechBubbleTransform = isPlayer ? playerSpeechBubbleTransform : opponentSpeechBubbleTransform;
        Image targetSpeechBubbleImage = isPlayer ? playerSpeechBubbleImage : opponentSpeechBubbleImage;

        // ����� �ִϸ��̼�
        targetSpeechBubbleTransform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
        targetSpeechBubbleImage.DOFade(0f, 0.5f);
    }
}
