using Unity.Netcode;
using UnityEngine;

public class HuntObjective : NetworkBehaviour
{
    [SerializeField] private string objectiveId = "samples";
    [SerializeField] private int requiredAmount = 5;
    [SerializeField] private bool canBeSabotaged = true;

    public string Id => objectiveId;
    public int RequiredAmount => requiredAmount;
    public NetworkVariable<int> Progress { get; } = new();
    public NetworkVariable<bool> Sabotaged { get; } = new();
    public bool IsComplete => !Sabotaged.Value && Progress.Value >= requiredAmount;

    public void AddProgressServer(int amount)
    {
        if (!IsServer || Sabotaged.Value) return;
        Progress.Value = Mathf.Clamp(Progress.Value + amount, 0, requiredAmount);
    }

    public void SabotageServer()
    {
        if (!IsServer || !canBeSabotaged) return;
        Sabotaged.Value = true;
        Progress.Value = 0;
    }
}
