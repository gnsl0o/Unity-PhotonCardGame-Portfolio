using UnityEngine;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    // 방 정보를 담을 딕셔너리
    private Dictionary<string, RoomItem> roomItemDict = new Dictionary<string, RoomItem>();

    private void Awake()
    {
        // 싱글턴 패턴을 사용하여 전역으로 접근할 수 있도록 한다.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 이미 인스턴스가 존재하면 현재 객체를 파괴
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 객체를 유지
        }
    }

    // 방 정보를 추가하는 메서드
    public void AddRoomItem(string roomName, RoomItem roomItem)
    {
        // 키가 없거나, 키는 있지만 해당 RoomItem이 null 또는 gameObject가 파괴된 경우 덮어씀
        if (!roomItemDict.ContainsKey(roomName) || roomItemDict[roomName] == null || roomItemDict[roomName].gameObject == null)
        {
            roomItemDict[roomName] = roomItem;
        }
    }

    // 방 정보를 가져오는 메서드
    public RoomItem GetRoomItem(string roomName)
    {
        roomItemDict.TryGetValue(roomName, out RoomItem roomItem);
        Debug.Log("이번에 들어온 roomName " +  roomName);
        if(roomItem != null)
        {
            Debug.Log("값: " + roomItemDict[roomName]);
        }
        return roomItem;
    }

    // 방 정보를 제거하는 메서드
    public void RemoveRoomItem(string roomName)
    {
        roomItemDict.Remove(roomName);
    }

    // 방 아이템을 반환할 수 있는 메서드
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
            Debug.Log("디렉토리에는 아무것도 없네용");
        }
    }
}