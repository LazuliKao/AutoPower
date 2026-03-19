namespace AutoPower.Core.Core.Models;

public sealed record StrategyCondition
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public StrategyConditionType Type { get; init; } = StrategyConditionType.DayType;
    public DayType DayType { get; init; } = DayType.All;
    public TimeOnly Start { get; init; }
    public TimeOnly End { get; init; }
}
