using UnityEngine;

public class PhotographerHunter : HunterCharacterBase
{
    [SerializeField] private float photoRange = 60f;
    [SerializeField, Range(1f, 60f)] private float photoAngle = 18f;
    protected override HuntRole HunterRole => HuntRole.Photographer;

    protected override void ExecuteClassAbilityServer(Vector3 position, Vector3 forward)
    {
        NetworkCharacterBase best = null;
        float bestDistance = float.MaxValue;
        foreach (NetworkCharacterBase animal in FindObjectsByType<NetworkCharacterBase>(FindObjectsSortMode.None))
        {
            if (animal.Team.Value != HuntTeam.WildAnimalTeam || !animal.IsAlive) continue;
            Vector3 delta = animal.transform.position - position;
            if (delta.sqrMagnitude > photoRange * photoRange || Vector3.Angle(forward, delta) > photoAngle) continue;
            if (delta.sqrMagnitude < bestDistance) { best = animal; bestDistance = delta.sqrMagnitude; }
        }
        if (best != null) HuntObjectiveManager.Instance?.AddProgressServer("photos", 1);
        NoiseSystem.Instance?.EmitServer(position, 20f, NoiseKind.HunterWork, HuntTeam.HunterTeam);
    }
}
