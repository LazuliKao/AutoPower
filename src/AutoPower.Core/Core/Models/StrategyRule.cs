namespace AutoPower.Core.Core.Models;

public sealed record StrategyRule
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public DayType DayType { get; init; } = DayType.All;
    public TimeOnly Start { get; init; }
    public TimeOnly End { get; init; }
    public Guid TargetPlanGuid { get; init; }
    public int Priority { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public bool IsEnabled { get; init; } = true;
}
