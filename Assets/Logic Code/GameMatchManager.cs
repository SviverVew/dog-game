using System.Collections.Generic;
using System.Collections;
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

    public override void OnNetworkSpawn()
    {
        // House1_Scene is now gameplay-only. Once its in-scene NetworkObject is
        // spawned, the server assigns roles automatically instead of relying on
        // the removed lobby UI/Enter key flow.
        if (IsServer) StartCoroutine(StartMatchAfterSceneSpawn());
    }

    private IEnumerator StartMatchAfterSceneSpawn()
    {
        yield return null;
        StartMatchAndAssignRoles();
    }

    // Hàm này CHỈ ĐƯỢC GỌI BỞI HOST/SERVER
    // Hàm này CHỈ ĐƯỢC GỌI BỞI HOST/SERVER
    public void StartMatchAndAssignRoles()
    {
        if (!IsServer) return;

        IReadOnlyList<ulong> connectedClients = NetworkManager.Singleton.ConnectedClientsIds;

        if (connectedClients.Count == 0) return;

        // 1. Tạo danh sách ngẫu nhiên các Role (ví dụ 4 người -> 2 Dog, 2 Person)
        List<PlayerRole> rolesToAssign = new List<PlayerRole>();
        
        int dogCount = connectedClients.Count / 2;
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

            // 🚨 BƯỚC 1: Xóa con cũ ĐÚNG CHUẨN NETCODE (Despawn trước khi Destroy)
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                if (client.PlayerObject != null)
                {
                    var oldNetObj = client.PlayerObject;
                    oldNetObj.Despawn(true); // Despawn và Destroy luôn object cũ
                    Debug.Log($"[Match] Đã Despawn PlayerObject cũ cho client {clientId}");
                }
            }

            // 🚨 BƯỚC 2: Tính toán vị trí & Góc xoay Spawn chuẩn
            GameObject prefabToSpawn = (assignedRole == PlayerRole.Dog) ? dogPrefab : personPrefab;
            Vector3 spawnPos = Vector3.zero;
            Quaternion spawnRot = Quaternion.identity;

            if (assignedRole == PlayerRole.Dog && dogSpawnPoints != null && dogSpawnPoints.Length > 0)
            {
                Transform sp = dogSpawnPoints[dogSpawnIdx % dogSpawnPoints.Length];
                if (sp != null)
                {
                    spawnPos = sp.position;
                    spawnRot = sp.rotation;
                }
                dogSpawnIdx++;
            }
            else if (assignedRole == PlayerRole.Person && personSpawnPoints != null && personSpawnPoints.Length > 0)
            {
                Transform sp = personSpawnPoints[personSpawnIdx % personSpawnPoints.Length];
                if (sp != null)
                {
                    spawnPos = sp.position;
                    spawnRot = sp.rotation;
                }
                personSpawnIdx++;
            }

            // 🚨 BƯỚC 3: Instantiate tại đúng Position và Rotation
            if (prefabToSpawn == null)
            {
                Debug.LogError($"[Match] Prefab to spawn is null for role {assignedRole} (client {clientId}). Skipping.");
                continue;
            }

            GameObject playerInstance = Instantiate(prefabToSpawn, spawnPos, spawnRot);

            // Spawn qua mạng cho Client
            NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.SpawnAsPlayerObject(clientId, true);
                Debug.Log($"[Match] Spawned NetworkObject (id={netObj.NetworkObjectId}) for client {clientId}");
            }
            else
            {
                Debug.LogError($"[Match] Prefab {prefabToSpawn.name} does not contain a NetworkObject component!");
            }

            Debug.Log($"<color=green>[Match] Client [{clientId}] Spawn tại: {spawnPos} với Role: {assignedRole}</color>");
        }
    }
}
