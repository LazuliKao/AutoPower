namespace AutoPower.Core.Core.Models;

public sealed record AppConfig
{
    public int SchemaVersion { get; init; } = 2;
    public DetectionMode Mode { get; init; } = DetectionMode.Both;
    public int IdleTimeoutMinutes { get; init; } = 5;
    public Guid ActivePlanGuid { get; init; }
    public Guid IdlePlanGuid { get; init; }
    public Guid? DefaultPlanGuid { get; init; }
    public List<StrategyRule> Rules { get; init; } = new();
    public bool AutoStartEnabled { get; init; }
    public OverrideState Override { get; init; } = new();
}
