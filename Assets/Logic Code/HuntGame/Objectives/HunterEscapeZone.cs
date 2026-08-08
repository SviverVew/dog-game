using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HunterEscapeZone : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        NetworkCharacterBase character = other.GetComponentInParent<NetworkCharacterBase>();
        if (character == null || character.Role.Value == HuntRole.GuardDog) return;
        HuntObjectiveManager.Instance?.TryHunterEscapeServer(character);
    }

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }
}
