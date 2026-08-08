using Unity.Netcode;
using UnityEngine;

public class DestructibleEquipment : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private GameObject intactVisual;
    [SerializeField] private GameObject destroyedVisual;
    public NetworkVariable<int> Health { get; } = new();
    public NetworkVariable<bool> IsDestroyed { get; } = new();

    public override void OnNetworkSpawn()
    {
        IsDestroyed.OnValueChanged += OnDestroyedChanged;
        if (IsServer) Health.Value = maxHealth;
        ApplyVisual(IsDestroyed.Value);
    }

    public void DamageServer(int damage)
    {
        if (!IsServer || IsDestroyed.Value || damage <= 0) return;
        Health.Value = Mathf.Max(0, Health.Value - damage);
        if (Health.Value == 0)
        {
            IsDestroyed.Value = true;
            if (FindObjectsByType<DestructibleEquipment>(FindObjectsSortMode.None).Length > 0 &&
                System.Array.TrueForAll(FindObjectsByType<DestructibleEquipment>(FindObjectsSortMode.None), x => x.IsDestroyed.Value))
                HuntMatchManager.Instance?.EndMatchServer(HuntTeam.WildAnimalTeam, HuntWinReason.EquipmentDestroyed);
        }
    }

    private void OnDestroyedChanged(bool _, bool value) => ApplyVisual(value);
    private void ApplyVisual(bool destroyed)
    {
        if (intactVisual != null) intactVisual.SetActive(!destroyed);
        if (destroyedVisual != null) destroyedVisual.SetActive(destroyed);
    }
}
