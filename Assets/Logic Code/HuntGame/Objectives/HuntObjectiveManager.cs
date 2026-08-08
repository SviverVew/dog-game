using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class HuntObjectiveManager : NetworkBehaviour
{
    public static HuntObjectiveManager Instance { get; private set; }
    [SerializeField] private HuntObjective[] objectives;
    public bool AllRequiredObjectivesComplete => objectives != null && objectives.Length > 0 && objectives.All(x => x != null && x.IsComplete);

    private void Awake() => Instance = this;

    public void AddProgressServer(string objectiveId, int amount)
    {
        if (!IsServer || objectives == null) return;
        HuntObjective target = objectives.FirstOrDefault(x => x != null && x.Id == objectiveId);
        target?.AddProgressServer(amount);
    }

    public void SabotageServer(string objectiveId)
    {
        if (!IsServer || objectives == null) return;
        HuntObjective target = objectives.FirstOrDefault(x => x != null && x.Id == objectiveId);
        target?.SabotageServer();
        if (objectives.Length > 0 && objectives.All(x => x == null || x.Sabotaged.Value))
            HuntMatchManager.Instance?.EndMatchServer(HuntTeam.WildAnimalTeam, HuntWinReason.ObjectivesSabotaged);
    }

    public void TryHunterEscapeServer(NetworkCharacterBase hunter)
    {
        if (!IsServer || hunter == null || hunter.Team.Value != HuntTeam.HunterTeam) return;
        if (AllRequiredObjectivesComplete)
            HuntMatchManager.Instance?.EndMatchServer(HuntTeam.HunterTeam, HuntWinReason.HunterEscapedAfterObjectives);
    }
}
