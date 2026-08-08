using UnityEngine;

public class MonkeyAnimal : AnimalCharacterBase
{
    [SerializeField] private int rockDamage = 10;
    [SerializeField] private float rockRange = 25f;
    protected override HuntRole AnimalRole => HuntRole.Monkey;

    protected override void ExecutePrimaryAbilityServer(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin + Vector3.up, direction, out RaycastHit hit, rockRange,
                ~0, QueryTriggerInteraction.Ignore))
        {
            NetworkCharacterBase target = hit.collider.GetComponentInParent<NetworkCharacterBase>();
            if (target != null && Character.IsEnemy(target))
                target.DamageServer(rockDamage, OwnerClientId);
        }
        EmitNoiseServer(noiseRadius, NoiseKind.Impact);
    }
}
