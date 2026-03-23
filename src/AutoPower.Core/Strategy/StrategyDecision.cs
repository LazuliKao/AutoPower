using AutoPower.Core.Core.Models;

namespace AutoPower.Core.Strategy;

public sealed record StrategyDecision
{
    public Guid PlanGuid { get; init; }
    public AppState State { get; init; } = AppState.Active;
    public string Source { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public bool IsFallback { get; init; }
    public bool IsRuntimeDependent { get; init; }
}

