using UnityEngine;

public class WolfAnimal : AnimalCharacterBase
{
    [SerializeField] private int biteDamage = 25;
    [SerializeField, Range(0.05f, 0.9f)] private float cannibalThreshold = 0.2f;
    [SerializeField] private int cannibalHeal = 40;
    protected override HuntRole AnimalRole => HuntRole.Wolf;

    protected override void ExecutePrimaryAbilityServer(Vector3 origin, Vector3 direction)
    {
        NetworkCharacterBase target = FindNearestCharacter(origin + direction, abilityRange, false);
        if (target == null) return;

        if (Character.IsEnemy(target))
            target.DamageServer(biteDamage, OwnerClientId);
        else if (target.Team.Value == HuntTeam.WildAnimalTeam &&
                 target.Health.Value <= Mathf.CeilToInt(target.MaxHealth.Value * cannibalThreshold))
        {
            target.DamageServer(target.Health.Value, OwnerClientId);
            Character.HealServer(cannibalHeal);
        }
        EmitNoiseServer(noiseRadius, NoiseKind.Howl);
    }
}
