using UnityEngine;

public class VeterinarianHunter : HunterCharacterBase
{
    [SerializeField] private int tranquilizerDamage = 15;
    [SerializeField] private float range = 45f;
    protected override HuntRole HunterRole => HuntRole.Veterinarian;

    protected override void ExecuteClassAbilityServer(Vector3 position, Vector3 forward)
    {
        if (!Physics.Raycast(position + Vector3.up * 1.4f, forward, out RaycastHit hit, range)) return;
        NetworkCharacterBase target = hit.collider.GetComponentInParent<NetworkCharacterBase>();
        if (target != null && target.Team.Value == HuntTeam.WildAnimalTeam)
            target.DamageServer(tranquilizerDamage, OwnerClientId);
        NoiseSystem.Instance?.EmitServer(position, 65f, NoiseKind.HunterWork, HuntTeam.HunterTeam);
    }
}
