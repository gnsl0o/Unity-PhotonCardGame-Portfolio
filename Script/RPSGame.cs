using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using DG.Tweening;
using ExitGames.Client.Photon;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public enum RPS { Rock, Paper, Scissors }

public enum GameState
{
    Waiting,
    Playing,
    Ended
}

public enum GamePhase
{
    DealCards,
    SelectCards,
    ExchangeCards,
    ResolveRound
}

public class RPS_GameManager : MonoBehaviourPunCallbacks
{
    private List<CardType> selectedCards = new List<CardType>();

    private const int CardsPerRound = 5;
    private const int TotalRounds = 5;

    public GameObject rockPrefab;
    public GameObject paperPrefab;
    public GameObject scissorsPrefab;

    public Transform playerHandTransform;
    public Transform opponentHandTransform;
    public Transform deckTransform;

    public GridLayoutGroup playerGridLayout;
    public GridLayoutGroup opponentGridLayout;

    private List<Card> deck;
    private List<GameObject> playerHandUI = new List<GameObject>();
    private List<GameObject> opponentHandUI = new List<GameObject>();

    public List<UnityEngine.UI.Button> cardButtons = new List<UnityEngine.UI.Button>();

    public GameObject waitingMessage;
    public GameObject cards;
    public TextMeshProUGUI currentStateMessage;
    public TextMeshProUGUI cannotExitMessage;
    public GameObject exitPanel;

    private GameState currentState;
    private GamePhase currentPhase;

    private int playerScore = 0;
    private int opponentScore = 0;
    private int roundCount = 0;
    private const int MaxRound = 3;

    public TextMeshProUGUI opponentReadyState;
    public TextMeshProUGUI playerReadyState;
    private bool isPlayerReady = false;
    private bool isOpponentReady = false;

    public void ChooseRock() { SetChoice(RPS.Rock); }
    public void ChoosePaper() { SetChoice(RPS.Paper); }
    public void ChooseScissors() { SetChoice(RPS.Scissors); }

    private void Awake()
    {
        PhotonPeer.RegisterType(typeof(Card), (byte)'C', SerializeCard, DeserializeCard);
    }

    private static byte[] SerializeCard(object customType)
    {
        Card card = (Card)customType;
        List<byte> bytes = new List<byte>();
        bytes.AddRange(BitConverter.GetBytes((int)card.Type)); // RPS 타입
        bytes.AddRange(BitConverter.GetBytes(card.cardID));     // 카드 ID
        return bytes.ToArray();
    }

    private static object DeserializeCard(byte[] data)
    {
        if (data.Length < 8) // 카드 데이터는 8바이트여야 함
        {
            throw new ArgumentException("데이터 길이가 올바르지 않습니다.");
        }

        RPS type = (RPS)BitConverter.ToInt32(data, 0);
        int cardID = BitConverter.ToInt32(data, 4);
        return new Card(type, cardID);
    }

    private void Start()
    {
        if (!(PhotonNetwork.IsMasterClient) && PhotonNetwork.CurrentRoom != null)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount != 0)
            {
                waitingMessage.SetActive(true);
                cards.SetActive(false);
            }
        }

        SetState(GameState.Waiting);

        photonView.RPC("StartGame", RpcTarget.All);
    }

    private void SetState(GameState newState)
    {
        currentState = newState;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom 호출");
        if (PhotonNetwork.CurrentRoom != null)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount != 0)
            {
                waitingMessage.SetActive(true);
                cards.SetActive(false);
            }
        }
    }

    public void OnReadyButtonClicked()
    {
        Hashtable playerProperties = new Hashtable();
        playerProperties["IsReady"] = true;
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);

        photonView.RPC("PlayerReadyUIUpdate", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);

        CheckAllPlayersReady();
    }

    private void CheckAllPlayersReady()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.ContainsKey("IsReady"))
            {
                bool isReady = (bool)player.CustomProperties["IsReady"];
                if (!isReady)
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        photonView.RPC("StartGame", RpcTarget.All);
    }

    [PunRPC]
    public void PlayerReadyUIUpdate(int playerActorNumber)
    {
        Player player = PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber == playerActorNumber);
        if (player != null)
        {
            bool isReady = player.CustomProperties.ContainsKey("IsReady") && (bool)player.CustomProperties["IsReady"];

            if (player == PhotonNetwork.LocalPlayer)
            {
                playerReadyState.text = "준비 완료!";
            }
            else
            {
                opponentReadyState.text = "준비 완료!";
            }
        }
    }

    public void OnExitButtonClicked()
    {
        if (currentState == GameState.Playing)
        {
            StartCoroutine(ShowMessageAndFadeOut());
        }
        else
        {
            exitPanel.SetActive(true);
        }
    }

    // 방에서 로비로 이동
    public void OnExitConfirmButtonClicked()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom(); // 방을 떠남
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Lobby");
    }

    public void OnCancelButtonClicked()
    {
        exitPanel.SetActive(false);
    }

    private void SetChoice(RPS choice)
    {

    }

    [PunRPC]
    private void StartGame()
    {
        // Custom Properties 초기화
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            Hashtable playerProperties = new Hashtable();
            playerProperties["IsReady"] = false; // 초기화
            player.SetCustomProperties(playerProperties);
        }
        Debug.Log("게임 시작!");

        if (PhotonNetwork.IsMasterClient)
        {
            CreateDeck();
            SetGamePhase(GamePhase.DealCards);
        }

        waitingMessage.SetActive(false);
        StartCoroutine(FadeText(currentStateMessage, "고르는 중...", 1f, true)); // 페이드 인 효과
        SetState(GameState.Playing);
    }

    private void SetGamePhase(GamePhase gamePhase)
    {
        currentPhase = gamePhase;

        switch (gamePhase)
        {
            case GamePhase.DealCards:
                DealCard();
                break;
            case GamePhase.ExchangeCards:
                OnExchangePhase();
                break;
        }
    }

    public void OnExchangePhase()
    {
        foreach (var cardButton in cardButtons)
        {
            cardButton.onClick.AddListener(() => OnCardClicked(cardButton));
        }
    }

    public void OnCardClicked(UnityEngine.UI.Button cardButton)
    {
        CardType cardType = cardButton.GetComponent<CardType>();

        if (cardType != null)
        {
            if (selectedCards.Contains(cardType))
            {
                selectedCards.Remove(cardType);
                cardButton.GetComponent<UnityEngine.UI.Image>().color = Color.white; // 버튼 색상 변경으로 선택 취소 표시
            }
            else
            {
                // 선택되지 않은 카드일 경우, 선택
                selectedCards.Add(cardType);
                cardButton.GetComponent<UnityEngine.UI.Image>().color = Color.green; // 버튼 색상 변경으로 선택 표시
            }
        }
    }

    public void OnSelectionButtonClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            HandleSelectedCards(selectedCards.ToArray(), PhotonNetwork.LocalPlayer); // 마스터 클라이언트 처리
        }
        else
        {
            SendSelectedCardsToMaster(); // 일반 클라이언트 처리
        }
    }

    // 일반 클라이언트가 선택된 카드를 마스터 클라이언트로 전송하는 메서드
    private void SendSelectedCardsToMaster()
    {
        photonView.RPC("ReceiveSelectedCards", RpcTarget.MasterClient, selectedCards.ToArray());
        Debug.Log("선택된 카드를 마스터 클라이언트로 전송");
    }

    private void HandleSelectedCards(CardType[] selectedCardTypes, Player player)
    {
        foreach (CardType cardType in selectedCardTypes)
        {
            deck.Add(new Card(cardType.cardType, cardType.cardID));
        }

        Debug.Log(player.NickName + "의 카드가 덱에 추가됨");

        if (PhotonNetwork.IsMasterClient)
        {
            // 모든 플레이어가 카드를 제출했는지 확인
            if (AllPlayersSubmitted())
            {
                ShuffleDeck();
                DealCardsToPlayers();
            }
        }
    }

    // 모든 플레이어가 카드를 제출했는지 확인하는 메서드
    private bool AllPlayersSubmitted()
    {
        // 예: 모든 플레이어가 제출했는지 확인하는 로직
        // (기본적으로는 플레이어 수에 따라 모든 카드가 제출되었는지 확인)
        return PhotonNetwork.PlayerList.Length * 5 == deck.Count;
    }

    // 덱을 셔플하는 메서드 (마스터 클라이언트만 호출)
    private void ShuffleDeck()
    {
        deck = deck.OrderBy(x => UnityEngine.Random.value).ToList();
        Debug.Log("덱이 셔플됨");
    }

    // 셔플된 덱을 플레이어들에게 나눠주는 메서드 (마스터 클라이언트만 호출)
    private void DealCardsToPlayers()
    {
        List<Card> player1Cards = deck.Take(5).Select(c => c).ToList();
        List<Card> player2Cards = deck.Skip(5).Take(5).Select(c => c).ToList();

        // photonView.RPC("ReceiveCards", RpcTarget.Others, player2Cards.ToArray(), player1Cards.ToArray());

        // 마스터 클라이언트는 자신의 카드를 직접 처리
        // ReceiveCards(player1Cards.ToArray(), player2Cards.ToArray());
    }

    private void EndGame()
    {
        string resultMessage;

        if (playerScore > opponentScore)
        {
            resultMessage = "게임 종료! 당신의 승리!";
        }
        else if (playerScore < opponentScore)
        {
            resultMessage = "게임 종료! 상대의 승리!";
        }
        else
        {
            resultMessage = "게임 종료! 무승부!";
        }

        currentStateMessage.text = resultMessage;

        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("ResetGame", RpcTarget.All);
        }
    }

    [PunRPC]
    private void ResetGame()
    {
        cards.SetActive(false);

        // Reset scores and round count for a new game
        playerScore = 0;
        opponentScore = 0;
        roundCount = 0;

        isPlayerReady = false;
        playerReadyState.text = "준비";
        isOpponentReady = false;
        opponentReadyState.text = "준비중";

        SetState(GameState.Waiting);
    }

    [PunRPC]
    private void SendChoice(RPS choice)
    {

    }

    [PunRPC]
    private void StartCountdown()
    {
        StartCoroutine(CountdownAndDetermineWinner());
    }

    private void DetermineWinner()
    {

    }

    private IEnumerator FadeText(TextMeshProUGUI textObject, string message, float duration, bool fadeIn)
    {
        textObject.text = message;
        float startAlpha = fadeIn ? 0 : 1;
        float endAlpha = fadeIn ? 1 : 0;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            SetTextAlpha(textObject, alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        SetTextAlpha(textObject, endAlpha);
    }

    private IEnumerator ShowMessageAndFadeOut()
    {
        // 페이드 인
        yield return StartCoroutine(FadeText(cannotExitMessage, "게임 도중에는 나갈 수 없습니다", 1f, true));

        // 메시지를 잠시 유지합니다.
        yield return new WaitForSeconds(2f); // 2초 동안 메시지를 유지

        yield return StartCoroutine(FadeText(cannotExitMessage, "게임 도중에는 나갈 수 없습니다", 1f, false));
    }

    private void SetTextAlpha(TextMeshProUGUI textObject, float alpha)
    {
        Color color = textObject.color;
        color.a = alpha;
        textObject.color = color;
    }

    private IEnumerator CountdownAndDetermineWinner()
    {
        for (int i = 3; i > 0; i--)
        {
            StartCoroutine(FadeText(currentStateMessage, i.ToString(), 1f, true));
            yield return new WaitForSeconds(1f);
        }

        DetermineWinner();
    }

    private void CreateDeck()
    {
        deck = new List<Card>();
        int idCounter = 0;

        for (int i = 0; i < 17; i++)
        {
            deck.Add(new Card(RPS.Rock, idCounter++));
            deck.Add(new Card(RPS.Paper, idCounter++));
            deck.Add(new Card(RPS.Scissors, idCounter++));
        }

        deck = deck.OrderBy(x => UnityEngine.Random.value).ToList();
    }

    private static byte[] SerializeCardArray(Card[] cards)
    {
        List<byte> bytes = new List<byte>();
        foreach (Card card in cards)
        {
            bytes.AddRange(SerializeCard(card));
        }
        return bytes.ToArray();
    }

    private static Card[] DeserializeCardArray(byte[] data)
    {
        List<Card> cards = new List<Card>();
        int cardSize = SerializeCard(new Card(RPS.Rock, 0)).Length; // Example card size
        for (int i = 0; i < data.Length; i += cardSize)
        {
            byte[] cardData = data.Skip(i).Take(cardSize).ToArray();
            Card card = (Card)DeserializeCard(cardData);
            cards.Add(card);
        }
        return cards.ToArray();
    }

    private void DealCard()
    {
        List<Card> playerCards = new List<Card>();
        List<Card> opponentCards = new List<Card>();

        for (int i = 0; i < CardsPerRound; i++)
        {
            if (deck.Count > 1) // Ensure there are enough cards for both players
            {
                playerCards.Add(deck[0]);
                opponentCards.Add(deck[1]);

                deck.RemoveAt(0); // Remove the player's card from the deck
                deck.RemoveAt(0); // Remove the opponent's card from the deck
            }
        }

        // Convert lists to arrays
        Card[] playerCardsArray = playerCards.ToArray();
        Card[] opponentCardsArray = opponentCards.ToArray();

        // Serialize arrays to byte arrays
        byte[] playerCardsData = SerializeCardArray(playerCardsArray);
        byte[] opponentCardsData = SerializeCardArray(opponentCardsArray);

        // Send serialized data to each player
        photonView.RPC("ReceiveCards", RpcTarget.Others, opponentCardsData, playerCardsData);

        // Master client processes its own cards
        ReceiveCards(playerCardsData, opponentCardsData);
    }

    [PunRPC]
    public void ReceiveCards(byte[] playerCardsData, byte[] opponentCardsData)
    {
        Debug.Log("ReceiveCards 호출");

        Card[] playerCards = DeserializeCardArray(playerCardsData);
        Card[] opponentCards = DeserializeCardArray(opponentCardsData);

        for (int i = 0; i < playerCards.Length; i++)
        {
            // Process received cards
            GameObject playerCardObj = InstantiateCardPrefab(playerCards[i], deckTransform.position);
            playerCardObj.transform.SetParent(deckTransform, true);
            playerCardObj.transform.position = deckTransform.position;
            playerHandUI.Add(playerCardObj);

            GameObject opponentCardObj = InstantiateCardPrefab(opponentCards[i], deckTransform.position);
            opponentCardObj.transform.SetParent(deckTransform, true);
            opponentCardObj.transform.position = deckTransform.position;
            opponentHandUI.Add(opponentCardObj);
        }

        // Start the animation after all cards are created
        StartCoroutine(AnimateCardMovement());
    } 

    private GameObject InstantiateCardPrefab(Card card, Vector3 position)
    {
        GameObject cardPrefab = null;

        switch (card.Type)
        {
            case RPS.Rock:
                cardPrefab = rockPrefab;
                break;
            case RPS.Paper:
                cardPrefab = paperPrefab;
                break;
            case RPS.Scissors:
                cardPrefab = scissorsPrefab;
                break;
        }

        if (cardPrefab != null)
        {
            // 카드 프리팹을 생성
            GameObject cardInstance = Instantiate(cardPrefab, position, Quaternion.identity);

            // 프리팹에 있는 Button 컴포넌트를 찾아서 cardButtonsList에 추가
            UnityEngine.UI.Button cardButton = cardInstance.GetComponentInChildren<UnityEngine.UI.Button>();

            cardInstance.GetComponent<CardType>().cardID = card.cardID;

            if (cardButton != null)
            {
                cardButtons.Add(cardButton);
            }

            return cardInstance;
        }

        return null;
    }

    private IEnumerator AnimateCardMovement()
    {
        yield return new WaitForSeconds(2f);

        // Animate player cards
        for (int i = 0; i < playerHandUI.Count; i++)
        {
            GameObject card = playerHandUI[i];
            Vector3 targetPosition = playerHandTransform.GetChild(i).position;
            Debug.Log("타겟 포지션: " + targetPosition.x + ", " + targetPosition.y);
            card.transform.DOMove(targetPosition, 0.5f).SetEase(Ease.OutQuad).SetDelay(i * 0.1f);
            yield return null;
        }

        // Animate opponent cards
        for (int i = 0; i < opponentHandUI.Count; i++)
        {
            GameObject card = opponentHandUI[i];
            Vector3 targetPosition = opponentHandTransform.GetChild(i).position;
            card.transform.DOMove(targetPosition, 0.5f).SetEase(Ease.OutQuad).SetDelay(i * 0.1f);
            yield return null;
        }

        SetGamePhase(GamePhase.ExchangeCards);
    }

    public void testBu()
    {
        for (int i = 0; i < playerHandUI.Count; i++)
        {
            Vector3 targetPosition = playerHandTransform.GetChild(i).position;
            Debug.Log("타겟 포지션: " + targetPosition.x + ", " + targetPosition.y);
        }
    }
}