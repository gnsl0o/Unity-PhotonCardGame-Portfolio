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
        bytes.AddRange(BitConverter.GetBytes((int)card.Type)); // RPS Ÿ��
        bytes.AddRange(BitConverter.GetBytes(card.cardID));     // ī�� ID
        return bytes.ToArray();
    }

    private static object DeserializeCard(byte[] data)
    {
        if (data.Length < 8) // ī�� �����ʹ� 8����Ʈ���� ��
        {
            throw new ArgumentException("������ ���̰� �ùٸ��� �ʽ��ϴ�.");
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
        Debug.Log("OnJoinedRoom ȣ��");
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
                playerReadyState.text = "�غ� �Ϸ�!";
            }
            else
            {
                opponentReadyState.text = "�غ� �Ϸ�!";
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

    // �濡�� �κ�� �̵�
    public void OnExitConfirmButtonClicked()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom(); // ���� ����
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
        // Custom Properties �ʱ�ȭ
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            Hashtable playerProperties = new Hashtable();
            playerProperties["IsReady"] = false; // �ʱ�ȭ
            player.SetCustomProperties(playerProperties);
        }
        Debug.Log("���� ����!");

        if (PhotonNetwork.IsMasterClient)
        {
            CreateDeck();
            SetGamePhase(GamePhase.DealCards);
        }

        waitingMessage.SetActive(false);
        StartCoroutine(FadeText(currentStateMessage, "���� ��...", 1f, true)); // ���̵� �� ȿ��
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
                cardButton.GetComponent<UnityEngine.UI.Image>().color = Color.white; // ��ư ���� �������� ���� ��� ǥ��
            }
            else
            {
                // ���õ��� ���� ī���� ���, ����
                selectedCards.Add(cardType);
                cardButton.GetComponent<UnityEngine.UI.Image>().color = Color.green; // ��ư ���� �������� ���� ǥ��
            }
        }
    }

    public void OnSelectionButtonClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            HandleSelectedCards(selectedCards.ToArray(), PhotonNetwork.LocalPlayer); // ������ Ŭ���̾�Ʈ ó��
        }
        else
        {
            SendSelectedCardsToMaster(); // �Ϲ� Ŭ���̾�Ʈ ó��
        }
    }

    // �Ϲ� Ŭ���̾�Ʈ�� ���õ� ī�带 ������ Ŭ���̾�Ʈ�� �����ϴ� �޼���
    private void SendSelectedCardsToMaster()
    {
        photonView.RPC("ReceiveSelectedCards", RpcTarget.MasterClient, selectedCards.ToArray());
        Debug.Log("���õ� ī�带 ������ Ŭ���̾�Ʈ�� ����");
    }

    private void HandleSelectedCards(CardType[] selectedCardTypes, Player player)
    {
        foreach (CardType cardType in selectedCardTypes)
        {
            deck.Add(new Card(cardType.cardType, cardType.cardID));
        }

        Debug.Log(player.NickName + "�� ī�尡 ���� �߰���");

        if (PhotonNetwork.IsMasterClient)
        {
            // ��� �÷��̾ ī�带 �����ߴ��� Ȯ��
            if (AllPlayersSubmitted())
            {
                ShuffleDeck();
                DealCardsToPlayers();
            }
        }
    }

    // ��� �÷��̾ ī�带 �����ߴ��� Ȯ���ϴ� �޼���
    private bool AllPlayersSubmitted()
    {
        // ��: ��� �÷��̾ �����ߴ��� Ȯ���ϴ� ����
        // (�⺻�����δ� �÷��̾� ���� ���� ��� ī�尡 ����Ǿ����� Ȯ��)
        return PhotonNetwork.PlayerList.Length * 5 == deck.Count;
    }

    // ���� �����ϴ� �޼��� (������ Ŭ���̾�Ʈ�� ȣ��)
    private void ShuffleDeck()
    {
        deck = deck.OrderBy(x => UnityEngine.Random.value).ToList();
        Debug.Log("���� ���õ�");
    }

    // ���õ� ���� �÷��̾�鿡�� �����ִ� �޼��� (������ Ŭ���̾�Ʈ�� ȣ��)
    private void DealCardsToPlayers()
    {
        List<Card> player1Cards = deck.Take(5).Select(c => c).ToList();
        List<Card> player2Cards = deck.Skip(5).Take(5).Select(c => c).ToList();

        // photonView.RPC("ReceiveCards", RpcTarget.Others, player2Cards.ToArray(), player1Cards.ToArray());

        // ������ Ŭ���̾�Ʈ�� �ڽ��� ī�带 ���� ó��
        // ReceiveCards(player1Cards.ToArray(), player2Cards.ToArray());
    }

    private void EndGame()
    {
        string resultMessage;

        if (playerScore > opponentScore)
        {
            resultMessage = "���� ����! ����� �¸�!";
        }
        else if (playerScore < opponentScore)
        {
            resultMessage = "���� ����! ����� �¸�!";
        }
        else
        {
            resultMessage = "���� ����! ���º�!";
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
        playerReadyState.text = "�غ�";
        isOpponentReady = false;
        opponentReadyState.text = "�غ���";

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
        // ���̵� ��
        yield return StartCoroutine(FadeText(cannotExitMessage, "���� ���߿��� ���� �� �����ϴ�", 1f, true));

        // �޽����� ��� �����մϴ�.
        yield return new WaitForSeconds(2f); // 2�� ���� �޽����� ����

        yield return StartCoroutine(FadeText(cannotExitMessage, "���� ���߿��� ���� �� �����ϴ�", 1f, false));
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
        Debug.Log("ReceiveCards ȣ��");

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
            // ī�� �������� ����
            GameObject cardInstance = Instantiate(cardPrefab, position, Quaternion.identity);

            // �����տ� �ִ� Button ������Ʈ�� ã�Ƽ� cardButtonsList�� �߰�
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
            Debug.Log("Ÿ�� ������: " + targetPosition.x + ", " + targetPosition.y);
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
            Debug.Log("Ÿ�� ������: " + targetPosition.x + ", " + targetPosition.y);
        }
    }
}