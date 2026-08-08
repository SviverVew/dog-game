using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Owns the UGS Lobby + Relay lifecycle. Keep this object in Menu_Game and mark it
/// DontDestroyOnLoad. Lobby is used for discovery; Relay carries Netcode traffic.
/// </summary>
public class NetworkRelayManager : MonoBehaviour
{
    public static NetworkRelayManager Instance { get; private set; }

    public string PlayerName { get; set; } = "Player";
    public string CurrentJoinCode { get; private set; }
    public Lobby CurrentLobby { get; private set; }
    public bool IsLobbyHost => CurrentLobby != null &&
        AuthenticationService.Instance.IsSignedIn &&
        CurrentLobby.HostId == AuthenticationService.Instance.PlayerId;

    private const string RelayCodeKey = "relayCode";
    private Task initializationTask;
    private float heartbeatTimer;
    private float lobbyPollTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        initializationTask = InitializeUnityServicesAsync();
    }

    private async void Update()
    {
        if (CurrentLobby == null) return;

        if (IsLobbyHost)
        {
            heartbeatTimer -= Time.unscaledDeltaTime;
            if (heartbeatTimer <= 0f)
            {
                heartbeatTimer = 15f;
                try { await LobbyService.Instance.SendHeartbeatPingAsync(CurrentLobby.Id); }
                catch (Exception e) { Debug.LogWarning("Lobby heartbeat: " + e.Message); }
            }
        }

        lobbyPollTimer -= Time.unscaledDeltaTime;
        if (lobbyPollTimer <= 0f)
        {
            lobbyPollTimer = 2f;
            try { CurrentLobby = await LobbyService.Instance.GetLobbyAsync(CurrentLobby.Id); }
            catch (Exception e) { Debug.LogWarning("Lobby refresh: " + e.Message); }
        }
    }

    public async Task EnsureReadyAsync()
    {
        if (initializationTask == null) initializationTask = InitializeUnityServicesAsync();
        await initializationTask;
    }

    private async Task InitializeUnityServicesAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private Player MakeLobbyPlayer()
    {
        return new Player(data: new Dictionary<string, PlayerDataObject>
        {
            ["name"] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayerName)
        });
    }

    public async Task<Lobby> CreateRoomAsync(string roomName, bool isPublic, int maxPlayers = 6)
    {
        await EnsureReadyAsync();
        string relayCode = await CreateRelayHost(maxPlayers - 1);
        if (string.IsNullOrEmpty(relayCode)) return null;

        CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(
            string.IsNullOrWhiteSpace(roomName) ? PlayerName + "'s room" : roomName.Trim(),
            maxPlayers,
            new CreateLobbyOptions
            {
                IsPrivate = !isPublic,
                Player = MakeLobbyPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    // Member-only: users must join the Lobby before receiving Relay access.
                    [RelayCodeKey] = new DataObject(DataObject.VisibilityOptions.Member, relayCode)
                }
            });

        heartbeatTimer = 0f;
        return CurrentLobby;
    }

    public async Task<List<Lobby>> QueryPublicRoomsAsync()
    {
        await EnsureReadyAsync();
        QueryResponse result = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
        {
            Count = 25,
            Order = new List<QueryOrder>
            {
                new QueryOrder(false, QueryOrder.FieldOptions.Created)
            }
        });
        return result.Results;
    }

    public async Task<bool> JoinLobbyCodeAsync(string lobbyCode)
    {
        await EnsureReadyAsync();
        CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(
            lobbyCode.Trim().ToUpperInvariant(),
            new JoinLobbyByCodeOptions { Player = MakeLobbyPlayer() });
        return await ConnectToCurrentLobbyRelayAsync();
    }

    public async Task<bool> JoinPublicLobbyAsync(string lobbyId)
    {
        await EnsureReadyAsync();
        CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(
            lobbyId, new JoinLobbyByIdOptions { Player = MakeLobbyPlayer() });
        return await ConnectToCurrentLobbyRelayAsync();
    }

    private async Task<bool> ConnectToCurrentLobbyRelayAsync()
    {
        if (CurrentLobby == null || CurrentLobby.Data == null ||
            !CurrentLobby.Data.TryGetValue(RelayCodeKey, out DataObject relayData))
            return false;

        bool connected = await JoinRelayClient(relayData.Value);
        if (!connected) await LeaveLobbyAsync();
        return connected;
    }

    public async Task LeaveLobbyAsync()
    {
        Lobby lobby = CurrentLobby;
        CurrentLobby = null;
        if (lobby != null)
        {
            try
            {
                if (lobby.HostId == AuthenticationService.Instance.PlayerId)
                    await LobbyService.Instance.DeleteLobbyAsync(lobby.Id);
                else
                    await LobbyService.Instance.RemovePlayerAsync(lobby.Id, AuthenticationService.Instance.PlayerId);
            }
            catch (Exception e) { Debug.LogWarning("Leave lobby: " + e.Message); }
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
    }

    // Parameter name kept for compatibility with the old LobbyUIController.
    public async Task<string> CreateRelayHost(int maxPlayers = 3)
    {
        await EnsureReadyAsync();
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
        CurrentJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
            AllocationUtils.ToRelayServerData(allocation, "dtls"));
        return NetworkManager.Singleton.StartHost() ? CurrentJoinCode : null;
    }

    public async Task<bool> JoinRelayClient(string joinCode)
    {
        await EnsureReadyAsync();
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        CurrentJoinCode = joinCode;
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
            AllocationUtils.ToRelayServerData(allocation, "dtls"));
        return NetworkManager.Singleton.StartClient();
    }

    private async void OnApplicationQuit()
    {
        if (CurrentLobby != null && IsLobbyHost)
        {
            try { await LobbyService.Instance.DeleteLobbyAsync(CurrentLobby.Id); }
            catch { }
        }
    }
}
