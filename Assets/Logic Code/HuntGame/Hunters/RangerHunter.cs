using UnityEngine;

public class RangerHunter : HunterCharacterBase
{
    [SerializeField] private float trackingRadius = 120f;
    protected override HuntRole HunterRole => HuntRole.Ranger;

    protected override void ExecuteClassAbilityServer(Vector3 position, Vector3 forward)
    {
        float best = trackingRadius * trackingRadius;
        Vector3 found = position;
        foreach (NetworkCharacterBase character in FindObjectsByType<NetworkCharacterBase>(FindObjectsSortMode.None))
        {
            if (character.Team.Value != HuntTeam.WildAnimalTeam || !character.IsAlive) continue;
            float distance = (character.transform.position - position).sqrMagnitude;
            if (distance < best) { best = distance; found = character.transform.position; }
        }
        NoiseSystem.Instance?.EmitServer(found, 20f, NoiseKind.Footstep, HuntTeam.WildAnimalTeam);
    }
}
