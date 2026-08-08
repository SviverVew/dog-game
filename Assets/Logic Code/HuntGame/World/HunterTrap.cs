using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HunterTrap : NetworkBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private float immobilizeSeconds = 3f;
    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || triggered) return;
        NetworkCharacterBase animal = other.GetComponentInParent<NetworkCharacterBase>();
        if (animal == null || animal.Team.Value != HuntTeam.WildAnimalTeam) return;
        triggered = true;
        animal.DamageServer(damage, OwnerClientId);
        StartCoroutine(Immobilize(animal));
    }

    private IEnumerator Immobilize(NetworkCharacterBase animal)
    {
        Rigidbody body = animal.GetComponent<Rigidbody>();
        if (body != null) body.constraints |= RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        yield return new WaitForSeconds(immobilizeSeconds);
        if (body != null) body.constraints &= ~(RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ);
        if (NetworkObject != null && NetworkObject.IsSpawned) NetworkObject.Despawn(true);
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;
}
