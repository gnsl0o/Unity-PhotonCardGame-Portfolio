using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomItem : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public Button joinButton;

    public void Setup(RoomInfo roomInfo)
    {
        roomNameText.text = roomInfo.Name;
        joinButton.onClick.RemoveAllListeners(); // ��ư Ŭ�� �̺�Ʈ�� �缳��
        joinButton.onClick.AddListener(() => OnJoinRoom(roomInfo.Name));
    }

    private void OnJoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }
}
