using UnityEngine;

public class BoarAnimal : AnimalCharacterBase
{
    [SerializeField] private int chargeDamage = 30;
    [SerializeField] private float chargeRange = 4f;
    protected override HuntRole AnimalRole => HuntRole.Boar;

    protected override void ExecutePrimaryAbilityServer(Vector3 origin, Vector3 direction)
    {
        NetworkCharacterBase target = FindNearestCharacter(origin + direction * 1.5f, chargeRange, true);
        if (target != null) target.DamageServer(chargeDamage, OwnerClientId);
        EmitNoiseServer(noiseRadius, NoiseKind.Impact);
    }
}
