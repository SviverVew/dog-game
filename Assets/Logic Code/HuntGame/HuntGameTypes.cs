using System;

public enum HuntTeam : byte
{
    HunterTeam,
    WildAnimalTeam
}

public enum HuntRole : byte
{
    Trapper,
    Ranger,
    Veterinarian,
    Photographer,
    GuardDog,
    Wolf,
    Fox,
    Monkey,
    Boar
}

public enum NoiseKind : byte
{
    Footstep,
    HunterWork,
    Trap,
    Howl,
    FakeNoise,
    Impact
}

public enum HuntWinReason : byte
{
    None,
    HunterEscapedAfterObjectives,
    HunterDown,
    TimeExpired,
    EquipmentDestroyed,
    ObjectivesSabotaged
}

[Serializable]
public struct RoleStats
{
    public int maxHealth;
    public float walkSpeedMultiplier;
    public float sprintSpeedMultiplier;
    public float primaryCooldown;
}
