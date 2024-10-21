using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    #region Variables

    public GameObject roomItemPrefab;
    public Transform contentTransform;

    public GameObject createRoomPopup;
    public TMP_InputField roomNameInputField;
    public Button createRoomButton;
    public Button cancelButton;
    public GameObject buttonBlocker;

    private enum RPS { Rock, Paper, Scissors, None }
    private RPS playerChoice = RPS.None;
    private RPS opponentChoice = RPS.None;

    private Coroutine updateCoroutine;

    private List<RoomInfo> cachedRoomList = new List<RoomInfo>();
    private bool isUpdatingRoomList = false;

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        createRoomPopup.SetActive(false);

        roomNameInputField.text = "";

        PhotonNetwork.ConnectUsingSettings();
    }

    #endregion

    #region Photon Callbacks

    public override void OnConnectedToMaster()
    {
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedLobby()
    {

    }

    public override void OnJoinedRoom()
    {
        SceneManager.LoadScene("RPSGame");  // �濡 ���� RPSGame ������ �̵�
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        // Debug.Log("OnRoomListUpdate ȣ��");

        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }

        cachedRoomList = roomList;
    }

    #endregion

    #region UI Event Handlers

    public void OnConfirmCreateRoomButtonClicked()
    {
        if (PhotonNetwork.InLobby)
        {
            string roomName = roomNameInputField.text;
            if (string.IsNullOrEmpty(roomName))
            {
                roomName = "Default Room Name";  // �� �̸��� ��������� �⺻�� ���
            }

            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = 2; // �ִ� �÷��̾� �� ����

            PhotonNetwork.CreateRoom(roomName, roomOptions);  // �� ����
            createRoomPopup.SetActive(false);  // �˾� �ݱ�

            SceneManager.LoadScene("RPSGame");
        }
        else
        {
            Debug.LogWarning("Not in lobby. Cannot create room.");
        }
    }

    public void OnCreateButtonClicked()
    {
        if (PhotonNetwork.InLobby)
        {
            createRoomPopup.SetActive(true);
            buttonBlocker.SetActive(true);
            string roomName = roomNameInputField.text;
        }
        else
        {
            Debug.LogWarning("Not in lobby. Cannot create room.");
        }
    }

    public void OnCancelButtonClicked()
    {
        buttonBlocker.SetActive(false);
        createRoomPopup.SetActive(false);
    }

    #endregion

    #region Custom Methods

    public void Test()
    {
        updateCoroutine = StartCoroutine(UpdateRoomListAfterDelay(cachedRoomList));
    }

    public void Test2()
    {
        foreach(RoomInfo roomInfo in cachedRoomList)
        {
            if (PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }

            Invoke("Test", 1f);
        }
    }

    public void Test3()
    {
        foreach(var item in RoomManager.Instance.GetRoomItemDict())
        {
            Debug.Log(item.Key);
        }
    }

    private void UpdateRoomListUI(List<RoomInfo> roomList)
    {
        // Debug.Log("Updating room list UI with " + roomList.Count + " rooms.");

        HashSet<string> updatedRoomNames = new HashSet<string>();
        foreach (RoomInfo room in roomList)
        {
            updatedRoomNames.Add(room.Name);
        }

        foreach (string roomName in updatedRoomNames)
        {
            // Debug.Log("���̸� " + roomName);
        }

        // Update existing or create new room items
        foreach (RoomInfo room in roomList)
        {
            RoomItem roomItem = RoomManager.Instance.GetRoomItem(room.Name);

            if (roomItem != null)
            {
                roomItem.Setup(room);
                roomItem.gameObject.SetActive(true);
                Debug.Log("�ش� ���� �����ϹǷ� Ȱ��ȭ");
            }
            else
            {
                Debug.Log("�ش� ���� �������� ���� ���ο� ���� ����");
                GameObject roomItemObj = Instantiate(roomItemPrefab, contentTransform);
                roomItem = roomItemObj.GetComponent<RoomItem>();
                roomItem.Setup(room);
                roomItem.gameObject.SetActive(true);
                RoomManager.Instance.AddRoomItem(room.Name, roomItem);
            }
        }

        // Remove items that are not in the updated room list
        List<string> keysToRemove = new List<string>();
        foreach (var pair in RoomManager.Instance.GetRoomItemDict())
        {
            if (!updatedRoomNames.Contains(pair.Key))
            {
                keysToRemove.Add(pair.Key);
            }
        }

        foreach (string key in keysToRemove)
        {
            Debug.Log($"Removing Room: {key}");
            Destroy(RoomManager.Instance.GetRoomItem(key).gameObject);
            RoomManager.Instance.RemoveRoomItem(key);
        }
    }

    private IEnumerator UpdateRoomListAfterDelay(List<RoomInfo> roomList)
    {
        // �̹� ������Ʈ ���� ��� ���� ����
        if (isUpdatingRoomList)
        {
            yield break;
        }

        isUpdatingRoomList = true;

        yield return new WaitForSeconds(0.5f); // ���� �ð�

        UpdateRoomListUI(roomList);

        isUpdatingRoomList = false;
    }

    #endregion
}