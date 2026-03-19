namespace AutoPower.Core.Core.Models;

public sealed record StrategyConditionGroup
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public StrategyConditionGroupOperator Operator { get; init; } = StrategyConditionGroupOperator.All;
    public List<StrategyCondition> Conditions { get; init; } = new();
    public List<StrategyConditionGroup> Groups { get; init; } = new();

    public static StrategyConditionGroup MatchAll() => new();

    public static StrategyConditionGroup ForSchedule(DayType dayType, TimeOnly start, TimeOnly end)
    {
        return new()
        {
            Operator = StrategyConditionGroupOperator.All,
            Conditions = new()
            {
                new() { Type = StrategyConditionType.DayType, DayType = dayType },
                new() { Type = StrategyConditionType.TimeRange, Start = start, End = end },
            },
        };
    }
}
