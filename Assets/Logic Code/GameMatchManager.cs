using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>Creates 1 Hunter (+ optional player Guard Dog) versus up to 4 animals.</summary>
public class HuntMatchManager : NetworkBehaviour
{
    public static HuntMatchManager Instance { get; private set; }

    [Header("Legacy fallback prefabs")]
    [SerializeField] private GameObject dogPrefab;
    [SerializeField] private GameObject personPrefab;

    [Header("Role prefabs (null uses legacy fallback)")]
    [SerializeField] private GameObject hunterPrefab;
    [SerializeField] private GameObject guardDogPrefab;
    [SerializeField] private GameObject wolfPrefab;
    [SerializeField] private GameObject foxPrefab;
    [SerializeField] private GameObject monkeyPrefab;
    [SerializeField] private GameObject boarPrefab;

    [Header("Spawn points")]
    [SerializeField] private Transform[] hunterSpawnPoints;
    [SerializeField] private Transform[] animalSpawnPoints;
    // Kept so current House1_Scene Inspector data is not lost.
    [SerializeField] private Transform[] dogSpawnPoints;
    [SerializeField] private Transform[] personSpawnPoints;

    [Header("Match")]
    [SerializeField] private float matchDurationSeconds = 600f;
    [SerializeField] private bool useGuardDogWhenAtLeastThreePlayers = true;

    public NetworkVariable<float> TimeRemaining { get; } = new(600f);
    public NetworkVariable<bool> MatchRunning { get; } = new(false);
    public NetworkVariable<HuntTeam> WinningTeam { get; } = new();
    public NetworkVariable<HuntWinReason> WinReason { get; } = new();

    private readonly Dictionary<ulong, HuntRole> roles = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) StartCoroutine(StartAfterSceneSpawn());
    }

    private IEnumerator StartAfterSceneSpawn()
    {
        yield return null;
        StartMatchAndAssignRoles();
    }

    private void Update()
    {
        if (!IsServer || !MatchRunning.Value) return;
        TimeRemaining.Value = Mathf.Max(0f, TimeRemaining.Value - Time.deltaTime);
        if (TimeRemaining.Value <= 0f)
            EndMatchServer(HuntTeam.WildAnimalTeam, HuntWinReason.TimeExpired);
    }

    public void StartMatchAndAssignRoles()
    {
        if (!IsServer || MatchRunning.Value) return;
        IReadOnlyList<ulong> clients = NetworkManager.Singleton.ConnectedClientsIds;
        if (clients.Count < 2) return;

        roles.Clear();
        for (int i = 0; i < clients.Count; i++)
        {
            HuntRole role;
            if (i == 0) role = HuntRole.Trapper;
            else if (i == 1 && clients.Count >= 3 && useGuardDogWhenAtLeastThreePlayers) role = HuntRole.GuardDog;
            else role = WildRoleForIndex(i - (clients.Count >= 3 && useGuardDogWhenAtLeastThreePlayers ? 2 : 1));
            roles[clients[i]] = role;
            ReplacePlayerObject(clients[i], role, i);
        }

        TimeRemaining.Value = matchDurationSeconds;
        MatchRunning.Value = true;
        WinReason.Value = HuntWinReason.None;
    }

    private static HuntRole WildRoleForIndex(int index)
    {
        HuntRole[] wildRoles = { HuntRole.Wolf, HuntRole.Fox, HuntRole.Monkey, HuntRole.Boar };
        return wildRoles[Mathf.Abs(index) % wildRoles.Length];
    }

    private void ReplacePlayerObject(ulong clientId, HuntRole role, int spawnIndex)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client) && client.PlayerObject != null)
            client.PlayerObject.Despawn(true);

        GameObject prefab = GetPrefab(role);
        if (prefab == null)
        {
            Debug.LogError($"Missing prefab for {role}");
            return;
        }

        Transform spawn = GetSpawn(role, spawnIndex);
        GameObject instance = Instantiate(prefab,
            spawn == null ? Vector3.zero : spawn.position,
            spawn == null ? Quaternion.identity : spawn.rotation);
        NetworkObject networkObject = instance.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError($"{prefab.name} needs NetworkObject");
            Destroy(instance);
            return;
        }
        networkObject.SpawnAsPlayerObject(clientId, true);
    }

    private GameObject GetPrefab(HuntRole role)
    {
        return role switch
        {
            HuntRole.GuardDog => guardDogPrefab != null ? guardDogPrefab : dogPrefab,
            HuntRole.Wolf => wolfPrefab != null ? wolfPrefab : dogPrefab,
            HuntRole.Fox => foxPrefab != null ? foxPrefab : dogPrefab,
            HuntRole.Monkey => monkeyPrefab != null ? monkeyPrefab : dogPrefab,
            HuntRole.Boar => boarPrefab != null ? boarPrefab : dogPrefab,
            _ => hunterPrefab != null ? hunterPrefab : personPrefab
        };
    }

    private Transform GetSpawn(HuntRole role, int index)
    {
        bool hunterSide = role is HuntRole.Trapper or HuntRole.Ranger or HuntRole.Veterinarian or HuntRole.Photographer or HuntRole.GuardDog;
        Transform[] points = hunterSide
            ? (hunterSpawnPoints != null && hunterSpawnPoints.Length > 0 ? hunterSpawnPoints : personSpawnPoints)
            : (animalSpawnPoints != null && animalSpawnPoints.Length > 0 ? animalSpawnPoints : dogSpawnPoints);
        return points != null && points.Length > 0 ? points[index % points.Length] : null;
    }

    public void NotifyCharacterDownServer(NetworkCharacterBase character, ulong attackerClientId)
    {
        if (!IsServer || !MatchRunning.Value) return;
        bool hunterAlive = false;
        foreach (NetworkCharacterBase candidate in FindObjectsByType<NetworkCharacterBase>(FindObjectsSortMode.None))
        {
            if (candidate.Team.Value == HuntTeam.HunterTeam && candidate.Role.Value != HuntRole.GuardDog && candidate.IsAlive)
                hunterAlive = true;
        }
        if (!hunterAlive) EndMatchServer(HuntTeam.WildAnimalTeam, HuntWinReason.HunterDown);
    }

    public void EndMatchServer(HuntTeam winner, HuntWinReason reason)
    {
        if (!IsServer || !MatchRunning.Value) return;
        MatchRunning.Value = false;
        WinningTeam.Value = winner;
        WinReason.Value = reason;
        Debug.Log($"Match ended. Winner={winner}, reason={reason}");
    }
}

// Compatibility for the component already serialized in House1_Scene.
public class GameMatchManager : HuntMatchManager { }
