namespace AutoPower.Core.Core.Models;

public sealed record StrategyConditionGroup
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public StrategyConditionGroupOperator Operator { get; init; } = StrategyConditionGroupOperator.All;
    public List<StrategyCondition> Conditions { get; init; } = new();
    public List<StrategyConditionGroup> Groups { get; init; } = new();

    /// <summary>
    /// Creates a condition group that matches all conditions (default empty group).
    /// </summary>
    public static StrategyConditionGroup MatchAll() => new()
    {
        Operator = StrategyConditionGroupOperator.All,
        Conditions = new(),
        Groups = new()
    };
}
