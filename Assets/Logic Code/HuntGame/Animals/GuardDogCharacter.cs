using UnityEngine;

public class GuardDogCharacter : AnimalCharacterBase
{
    [SerializeField] private int biteDamage = 18;
    protected override HuntRole AnimalRole => HuntRole.GuardDog;
    protected override HuntTeam AnimalTeam => HuntTeam.HunterTeam;

    protected override void ExecutePrimaryAbilityServer(Vector3 origin, Vector3 direction)
    {
        NetworkCharacterBase target = FindNearestCharacter(origin + direction, abilityRange, true);
        if (target != null) target.DamageServer(biteDamage, OwnerClientId);
        EmitNoiseServer(noiseRadius, NoiseKind.Howl);
    }
}
