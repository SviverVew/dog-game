using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum PlayerRole
{
    Dog,
    Person
}

public class GameMatchManager : NetworkBehaviour
{
    public static GameMatchManager Instance;

    [Header("Prefabs")]
    public GameObject dogPrefab;    // Prefab Chó (Có NetworkObject)
    public GameObject personPrefab; // Prefab Người (Có NetworkObject)

    [Header("Spawn Points")]
    public Transform[] dogSpawnPoints;
    public Transform[] personSpawnPoints;

    // Lưu danh sách Role đã phân cho từng ClientId
    private Dictionary<ulong, PlayerRole> playerRoles = new Dictionary<ulong, PlayerRole>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Hàm này CHỈ ĐƯỢC GỌI BỞI HOST/SERVER
    public void StartMatchAndAssignRoles()
    {
        if (!IsServer) return;

        IReadOnlyList<ulong> connectedClients = NetworkManager.Singleton.ConnectedClientsIds;

        if (connectedClients.Count == 0) return;

        // 1. Tạo danh sách ngẫu nhiên các Role (ví dụ 6 người -> 3 Dog, 3 Person)
        List<PlayerRole> rolesToAssign = new List<PlayerRole>();
        
        int dogCount = connectedClients.Count / 2; // Ví dụ 6 người thì 3 chó, 3 người
        int personCount = connectedClients.Count - dogCount;

        for (int i = 0; i < dogCount; i++) rolesToAssign.Add(PlayerRole.Dog);
        for (int i = 0; i < personCount; i++) rolesToAssign.Add(PlayerRole.Person);

        // Tráo đổi ngẫu nhiên danh sách Role (Fisher-Yates Shuffle)
        for (int i = 0; i < rolesToAssign.Count; i++)
        {
            PlayerRole temp = rolesToAssign[i];
            int randomIndex = Random.Range(i, rolesToAssign.Count);
            rolesToAssign[i] = rolesToAssign[randomIndex];
            rolesToAssign[randomIndex] = temp;
        }

        // 2. Phân Role cho từng Client và Spawn nhân vật tương ứng
        int dogSpawnIdx = 0;
        int personSpawnIdx = 0;

        for (int i = 0; i < connectedClients.Count; i++)
        {
            ulong clientId = connectedClients[i];
            PlayerRole assignedRole = rolesToAssign[i];

            playerRoles[clientId] = assignedRole;

            // Xóa nhân vật mặc định cũ nếu NetworkManager lỡ Spawn sẵn
            if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null)
            {
                Destroy(NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject);
            }

            // Chọn Prefab & Vị trí Spawn
            GameObject prefabToSpawn = (assignedRole == PlayerRole.Dog) ? dogPrefab : personPrefab;
            Vector3 spawnPos = Vector3.zero;

            if (assignedRole == PlayerRole.Dog && dogSpawnPoints.Length > 0)
            {
                spawnPos = dogSpawnPoints[dogSpawnIdx % dogSpawnPoints.Length].position;
                dogSpawnIdx++;
            }
            else if (assignedRole == PlayerRole.Person && personSpawnPoints.Length > 0)
            {
                spawnPos = personSpawnPoints[personSpawnIdx % personSpawnPoints.Length].position;
                personSpawnIdx++;
            }

            // Spawn nhân vật qua mạng cho Client tương ứng
            GameObject playerInstance = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

            Debug.Log($"Client [{clientId}] được phân Role: {assignedRole}");
        }
    }
}