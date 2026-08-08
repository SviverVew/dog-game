using Unity.Netcode;
using UnityEngine;

public class TrapperHunter : HunterCharacterBase
{
    [SerializeField] private NetworkObject trapPrefab;
    [SerializeField] private float placeDistance = 2f;
    protected override HuntRole HunterRole => HuntRole.Trapper;

    protected override void ExecuteClassAbilityServer(Vector3 position, Vector3 forward)
    {
        if (trapPrefab == null) return;
        NetworkObject trap = Instantiate(trapPrefab, position + forward * placeDistance, Quaternion.LookRotation(forward));
        trap.Spawn(true);
        NoiseSystem.Instance?.EmitServer(position, 55f, NoiseKind.Trap, HuntTeam.HunterTeam);
        HuntObjectiveManager.Instance?.AddProgressServer("traps", 1);
    }
}
