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
    public UnityEngine.UI.Image[] playerEmojiImages;    // 플레이어 이모티콘 배열
    public UnityEngine.UI.Image[] opponentEmojiImages;  // 상대방 이모티콘 배열

    public Image playerActivatedEmoji;
    public Image opponentActivatedEmoji;

    public RectTransform  playerEmojiBoxTransform;
    public RectTransform playerSpeechBubbleTransform;  
    public Image playerSpeechBubbleImage;

    public RectTransform opponentEmojiBoxTransform;
    public RectTransform opponentSpeechBubbleTransform;
    public Image opponentSpeechBubbleImage;

    private Coroutine playerHideCoroutine; // 플레이어의 Coroutine을 추적하기 위한 변수
    private Coroutine opponentHideCoroutine; // 상대방의 Coroutine을 추적하기 위한 변수

    private void Start()
    {
        // 초기 위치를 버튼의 위치로 설정
        for (int i = 0; i < emojisTransform.Length; i++)
        {
            emojisTransform[i].position = button.position;
        }

        UpdateEmojiSprites();

        photonView.RPC("SendCharacterInfo", RpcTarget.Others, currentCharacter.characterName);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // 새로 들어온 플레이어에게 내 캐릭터 정보를 보냅니다.
        photonView.RPC("SendCharacterInfo", newPlayer, currentCharacter.characterName);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        characterImage[1].gameObject.SetActive(false);
    }

    public void OnEmojiIconClicked()
    {
        emojis.SetActive(true);

        // 이모티콘들이 퍼져나가는 애니메이션 실행
        for (int i = 0; i < emojisTransform.Length; i++)
        {
            // 기존 애니메이션 중단
            emojisTransform[i].DOKill();

            // 위치 이동 애니메이션
            emojisTransform[i].DOAnchorPos(targetPositions[i], duration).SetEase(Ease.OutBack).SetDelay(i * 0.05f);

            // 스케일 애니메이션: 0에서 1로 점진적으로 커지도록 설정
            emojisTransform[i].localScale = Vector3.zero;  // 초기 스케일을 0으로 설정
            emojisTransform[i].DOScale(Vector3.one, duration).SetEase(Ease.OutBack).SetDelay(i * 0.05f);
        }
    }

    public void OnEmojiButtonClicked(int emojiIndex)
    {
        // 자신에게 이모티콘 표시
        DisplayEmoji(playerEmojiImages, emojiIndex, true);

        emojis.SetActive(false);

        // 초기 위치를 버튼의 위치로 설정
        for (int i = 0; i < emojisTransform.Length; i++)
        {
            emojisTransform[i].position = button.position;
        }

        // 상대방에게 이모티콘 정보를 전달
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
            Debug.Log("이미지를 찾을 수 없음");
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
                ShowSpeechBubble(true); // 플레이어의 말풍선 표시

                // 이전에 실행 중인 플레이어의 Coroutine이 있으면 중지
                if (playerHideCoroutine != null)
                {
                    StopCoroutine(playerHideCoroutine);
                }

                // 새롭게 HideSpeechBubble을 실행
                playerHideCoroutine = StartCoroutine(HideSpeechBubbleAfterDelay(2f, true));
            }
            else
            {
                opponentActivatedEmoji.sprite = emojiImagesArray[emojiIndex].sprite;
                ShowSpeechBubble(false); // 상대방의 말풍선 표시

                // 이전에 실행 중인 상대방의 Coroutine이 있으면 중지
                if (opponentHideCoroutine != null)
                {
                    StopCoroutine(opponentHideCoroutine);
                }

                // 새롭게 HideSpeechBubble을 실행
                opponentHideCoroutine = StartCoroutine(HideSpeechBubbleAfterDelay(2f, false));
            }
        }
    }

    public void ShowSpeechBubble(bool isPlayer)
    {
        RectTransform targetSpeechBubbleTransform = isPlayer ? playerSpeechBubbleTransform : opponentSpeechBubbleTransform;
        Image targetSpeechBubbleImage = isPlayer ? playerSpeechBubbleImage : opponentSpeechBubbleImage;
        RectTransform targetEmojiBoxTransform = isPlayer ? playerEmojiBoxTransform : opponentEmojiBoxTransform;

        // 애니메이션을 시작하기 전에 기존의 애니메이션을 종료
        targetSpeechBubbleTransform.DOKill();
        targetSpeechBubbleImage.DOKill();

        // 1. 말풍선의 초기 위치를 캐릭터 이미지 위에 설정
        targetSpeechBubbleTransform.position = targetEmojiBoxTransform.position;

        // 2. 말풍선 이미지의 크기와 투명도 초기화
        targetSpeechBubbleTransform.localScale = Vector3.zero; // 크기를 0으로 설정
        targetSpeechBubbleImage.color = new Color(1, 1, 1, 0); // 투명하게 설정

        // 3. DoTween 시퀀스를 통해 애니메이션 실행
        Sequence bubbleSequence = DOTween.Sequence();

        // 크기와 투명도 애니메이션
        if (isPlayer)
        {
            bubbleSequence.Append(targetSpeechBubbleTransform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack)); // 크기 확대
        }
        else
        {
            bubbleSequence.Append(targetSpeechBubbleTransform.DOScale(new Vector3(-1,-1,0), 0.5f).SetEase(Ease.OutBack)); // 크기 확대
        }
        bubbleSequence.Join(targetSpeechBubbleImage.DOFade(1f, 0.5f)); // 투명도 증가
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

        // 숨기기 애니메이션
        targetSpeechBubbleTransform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
        targetSpeechBubbleImage.DOFade(0f, 0.5f);
    }
}
