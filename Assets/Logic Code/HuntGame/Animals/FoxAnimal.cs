using UnityEngine;

public class FoxAnimal : AnimalCharacterBase
{
    [SerializeField] private float fakeNoiseDistance = 35f;
    protected override HuntRole AnimalRole => HuntRole.Fox;

    protected override void ExecutePrimaryAbilityServer(Vector3 origin, Vector3 direction)
    {
        Vector3 fakePosition = origin + direction * fakeNoiseDistance;
        NoiseSystem.Instance?.EmitServer(fakePosition, noiseRadius, NoiseKind.FakeNoise, HuntTeam.WildAnimalTeam);
    }
}
