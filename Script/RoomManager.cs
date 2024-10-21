using UnityEngine;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    // �� ������ ���� ��ųʸ�
    private Dictionary<string, RoomItem> roomItemDict = new Dictionary<string, RoomItem>();

    private void Awake()
    {
        // �̱��� ������ ����Ͽ� �������� ������ �� �ֵ��� �Ѵ�.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // �̹� �ν��Ͻ��� �����ϸ� ���� ��ü�� �ı�
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // �� ��ȯ �ÿ��� ��ü�� ����
        }
    }

    // �� ������ �߰��ϴ� �޼���
    public void AddRoomItem(string roomName, RoomItem roomItem)
    {
        // Ű�� ���ų�, Ű�� ������ �ش� RoomItem�� null �Ǵ� gameObject�� �ı��� ��� ���
        if (!roomItemDict.ContainsKey(roomName) || roomItemDict[roomName] == null || roomItemDict[roomName].gameObject == null)
        {
            roomItemDict[roomName] = roomItem;
        }
    }

    // �� ������ �������� �޼���
    public RoomItem GetRoomItem(string roomName)
    {
        roomItemDict.TryGetValue(roomName, out RoomItem roomItem);
        Debug.Log("�̹��� ���� roomName " +  roomName);
        if(roomItem != null)
        {
            Debug.Log("��: " + roomItemDict[roomName]);
        }
        return roomItem;
    }

    // �� ������ �����ϴ� �޼���
    public void RemoveRoomItem(string roomName)
    {
        roomItemDict.Remove(roomName);
    }

    // �� �������� ��ȯ�� �� �ִ� �޼���
    public Dictionary<string, RoomItem> GetRoomItemDict()
    {
        return roomItemDict;
    }

    public void Test22()
    {
        foreach (var kvp in roomItemDict)
        {
            Debug.Log($"Room Name: {kvp.Key}, Room Item: {kvp.Value}");
        }

        if (roomItemDict.Count == 0)
        {
            Debug.Log("���丮���� �ƹ��͵� ���׿�");
        }
    }
}