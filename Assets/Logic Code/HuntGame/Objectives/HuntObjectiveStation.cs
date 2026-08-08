using Unity.Netcode;
using UnityEngine;

/// <summary>Attach to a sample, signal, camp-delivery or trap-building station.</summary>
public class HuntObjectiveStation : NetworkBehaviour
{
    [SerializeField] private string objectiveId = "samples";
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private float interactionCooldown = 1f;
    [SerializeField] private bool animalsCanSabotage = true;
    [SerializeField] private float noiseRadius = 90f;
    public NetworkVariable<bool> Used { get; } = new();
    private float nextUseTime;

    private void Update()
    {
        if (!IsSpawned || Time.time < nextUseTime || !Input.GetKeyDown(KeyCode.E)) return;
        NetworkCharacterBase local = FindLocalCharacter();
        if (local == null || Vector3.Distance(local.transform.position, transform.position) > interactionRange) return;
        nextUseTime = Time.time + interactionCooldown;
        InteractServerRpc(local.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractServerRpc(ulong characterNetworkId, ServerRpcParams rpcParams = default)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(characterNetworkId, out NetworkObject obj)) return;
        if (obj.OwnerClientId != rpcParams.Receive.SenderClientId) return;
        NetworkCharacterBase character = obj.GetComponent<NetworkCharacterBase>();
        if (character == null || !character.IsAlive || Vector3.Distance(character.transform.position, transform.position) > interactionRange + 0.5f) return;

        if (character.Team.Value == HuntTeam.HunterTeam)
        {
            if (Used.Value) return;
            HuntObjectiveManager.Instance?.AddProgressServer(objectiveId, 1);
            Used.Value = true;
        }
        else if (animalsCanSabotage)
            HuntObjectiveManager.Instance?.SabotageServer(objectiveId);

        NoiseSystem.Instance?.EmitServer(transform.position, noiseRadius, NoiseKind.HunterWork, character.Team.Value);
    }

    private static NetworkCharacterBase FindLocalCharacter()
    {
        foreach (NetworkCharacterBase candidate in FindObjectsByType<NetworkCharacterBase>(FindObjectsSortMode.None))
            if (candidate.IsOwner) return candidate;
        return null;
    }
}
