namespace AutoPower.Core.Core.Models;

public sealed record StrategyRule
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public StrategyConditionGroup Condition { get; init; } = StrategyConditionGroup.MatchAll();
    public Guid TargetPlanGuid { get; init; }
    public int Priority { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public bool IsEnabled { get; init; } = true;
}
