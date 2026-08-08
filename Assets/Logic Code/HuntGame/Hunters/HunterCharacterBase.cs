using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkCharacterBase))]
public abstract class HunterCharacterBase : NetworkBehaviour
{
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected float abilityCooldown = 8f;
    protected NetworkCharacterBase Character { get; private set; }
    private float nextAbilityTime;
    protected abstract HuntRole HunterRole { get; }

    protected virtual void Awake() => Character = GetComponent<NetworkCharacterBase>();

    public override void OnNetworkSpawn()
    {
        if (IsServer) Character.ConfigureServer(HuntTeam.HunterTeam, HunterRole, maxHealth);
    }

    protected virtual void Update()
    {
        if (!IsOwner || !Character.IsAlive) return;
        if (Input.GetKeyDown(KeyCode.Q) && Time.time >= nextAbilityTime)
        {
            nextAbilityTime = Time.time + abilityCooldown;
            UseClassAbilityServerRpc(transform.position, transform.forward);
        }
    }

    [ServerRpc]
    private void UseClassAbilityServerRpc(Vector3 position, Vector3 forward)
    {
        if (Character.IsAlive) ExecuteClassAbilityServer(position, forward.normalized);
    }

    protected abstract void ExecuteClassAbilityServer(Vector3 position, Vector3 forward);
}
