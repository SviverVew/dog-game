using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>Server-authoritative state shared by Hunter, Guard Dog and animals.</summary>
public class NetworkCharacterBase : NetworkBehaviour
{
    [SerializeField] private int defaultMaxHealth = 100;

    public NetworkVariable<HuntTeam> Team { get; } = new();
    public NetworkVariable<HuntRole> Role { get; } = new();
    public NetworkVariable<int> MaxHealth { get; } = new(100);
    public NetworkVariable<int> Health { get; } = new(100);
    public NetworkVariable<bool> IsDown { get; } = new(false);

    public event Action<int, int> HealthChanged;
    public bool IsAlive => !IsDown.Value;

    public override void OnNetworkSpawn()
    {
        Health.OnValueChanged += OnHealthChanged;
        if (IsServer && MaxHealth.Value <= 0)
        {
            MaxHealth.Value = defaultMaxHealth;
            Health.Value = defaultMaxHealth;
        }
    }

    public override void OnNetworkDespawn()
    {
        Health.OnValueChanged -= OnHealthChanged;
    }

    public void ConfigureServer(HuntTeam team, HuntRole role, int maxHealth)
    {
        if (!IsServer) return;
        Team.Value = team;
        Role.Value = role;
        MaxHealth.Value = Mathf.Max(1, maxHealth);
        Health.Value = MaxHealth.Value;
        IsDown.Value = false;
    }

    public void DamageServer(int amount, ulong attackerClientId)
    {
        if (!IsServer || IsDown.Value || amount <= 0) return;
        Health.Value = Mathf.Max(0, Health.Value - amount);
        if (Health.Value == 0)
        {
            IsDown.Value = true;
            HuntMatchManager.Instance?.NotifyCharacterDownServer(this, attackerClientId);
        }
    }

    public void HealServer(int amount)
    {
        if (!IsServer || IsDown.Value || amount <= 0) return;
        Health.Value = Mathf.Min(MaxHealth.Value, Health.Value + amount);
    }

    public bool IsEnemy(NetworkCharacterBase other)
    {
        return other != null && Team.Value != other.Team.Value;
    }

    private void OnHealthChanged(int _, int value) => HealthChanged?.Invoke(value, MaxHealth.Value);
}
