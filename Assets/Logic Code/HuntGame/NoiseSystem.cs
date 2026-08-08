using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>One in-scene NetworkObject. Server distributes gameplay noise to all clients.</summary>
public class NoiseSystem : NetworkBehaviour
{
    public static NoiseSystem Instance { get; private set; }
    public static event Action<Vector3, float, NoiseKind, HuntTeam> NoiseHeard;

    private void Awake() => Instance = this;

    public void EmitServer(Vector3 position, float radius, NoiseKind kind, HuntTeam sourceTeam)
    {
        if (!IsServer) return;
        BroadcastNoiseClientRpc(position, Mathf.Max(0f, radius), kind, sourceTeam);
    }

    [ClientRpc]
    private void BroadcastNoiseClientRpc(Vector3 position, float radius, NoiseKind kind, HuntTeam sourceTeam)
    {
        NoiseHeard?.Invoke(position, radius, kind, sourceTeam);
    }
}
