using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class NetworkRelayManager : MonoBehaviour
{
    public static NetworkRelayManager Instance { get; private set; }
    public string PlayerName { get; set; } = "Player";
    public string CurrentJoinCode { get; private set; }

    private async void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        await InitializeUnityServicesAsync();
    }

    private async Task InitializeUnityServicesAsync()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("<color=green>Đã đăng nhập UGS thành công! Player ID: " + AuthenticationService.Instance.PlayerId + "</color>");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Lỗi khởi tạo UGS: " + e.Message);
        }
    }

    // Host tạo phòng và lấy Mã Code
    public async Task<string> CreateRelayHost(int maxPlayers = 10)
    {
        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await InitializeUnityServicesAsync();
            }

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            CurrentJoinCode = joinCode;

            var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();

            // 🚨 SỬA LỖI TẠI ĐÂY: Thứ tự chuẩn là (IP, Port, AllocationId, Key, ConnectionData)
            utp.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,                // Key (HMAC 64 bytes)
                allocation.ConnectionData     // ConnectionData
            );

            NetworkManager.Singleton.StartHost();
            Debug.Log($"<color=cyan>[Relay] Đã tạo phòng thành công. Mã Code: {joinCode}</color>");
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Lỗi tạo Relay Host: " + e.Message);
            return null;
        }
    }

    // Client tham gia bằng Mã Code
    public async Task<bool> JoinRelayClient(string joinCode)
    {
        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await InitializeUnityServicesAsync();
            }

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            CurrentJoinCode = joinCode;

            var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();

            // 🚨 SỬA LỖI TẠI ĐÂY: Thứ tự chuẩn cho Client
            utp.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,                    // Key (64 bytes)
                joinAllocation.ConnectionData,         // ConnectionData
                joinAllocation.HostConnectionData      // HostConnectionData
            );

            bool success = NetworkManager.Singleton.StartClient();
            if (success)
            {
                Debug.Log($"<color=cyan>[Relay] Đã kết nối vào phòng: {joinCode}</color>");
            }
            return success;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Lỗi tham gia Relay: " + e.Message);
            return false;
        }
    }
}