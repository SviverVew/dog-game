using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Base class for every playable wild animal and the Guard Dog. Existing
/// StarterAssets.Player remains responsible for locomotion/camera.
/// </summary>
[RequireComponent(typeof(NetworkCharacterBase))]
public abstract class AnimalCharacterBase : NetworkBehaviour
{
    [SerializeField] protected RoleStats stats = new()
    {
        maxHealth = 100,
        walkSpeedMultiplier = 1f,
        sprintSpeedMultiplier = 1f,
        primaryCooldown = 5f
    };
    [SerializeField] protected float abilityRange = 2f;
    [SerializeField] protected float noiseRadius = 80f;

    protected NetworkCharacterBase Character { get; private set; }
    private float nextAbilityTime;

    protected abstract HuntRole AnimalRole { get; }
    protected virtual HuntTeam AnimalTeam => HuntTeam.WildAnimalTeam;

    protected virtual void Awake()
    {
        Character = GetComponent<NetworkCharacterBase>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            Character.ConfigureServer(AnimalTeam, AnimalRole, stats.maxHealth);
    }

    protected virtual void Update()
    {
        if (!IsOwner || !Character.IsAlive) return;
        if (Input.GetKeyDown(KeyCode.Q) && Time.time >= nextAbilityTime)
        {
            nextAbilityTime = Time.time + stats.primaryCooldown;
            UsePrimaryAbilityServerRpc(transform.position, transform.forward);
        }
    }

    public void EmitNoiseServer(float radius, NoiseKind kind)
    {
        if (IsServer) NoiseSystem.Instance?.EmitServer(transform.position, radius, kind, Character.Team.Value);
    }

    [ServerRpc]
    private void UsePrimaryAbilityServerRpc(Vector3 origin, Vector3 direction)
    {
        if (!Character.IsAlive) return;
        ExecutePrimaryAbilityServer(origin, direction.normalized);
    }

    protected abstract void ExecutePrimaryAbilityServer(Vector3 origin, Vector3 direction);

    protected NetworkCharacterBase FindNearestCharacter(Vector3 origin, float radius, bool enemyOnly)
    {
        NetworkCharacterBase nearest = null;
        float best = float.MaxValue;
        foreach (Collider hit in Physics.OverlapSphere(origin, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            NetworkCharacterBase candidate = hit.GetComponentInParent<NetworkCharacterBase>();
            if (candidate == null || candidate == Character || !candidate.IsAlive) continue;
            if (enemyOnly && !Character.IsEnemy(candidate)) continue;
            float distance = (candidate.transform.position - origin).sqrMagnitude;
            if (distance < best) { best = distance; nearest = candidate; }
        }
        return nearest;
    }
}
