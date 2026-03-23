#nullable enable

namespace AutoPower.Core.Core.Models;

/// <summary>
/// Represents a node in a decision tree for strategy evaluation.
/// Each node can either be a leaf (with PlanGuid) or a branching node (with Then/Else children).
/// </summary>
public sealed record StrategyDecisionNode
{
    /// <summary>
    /// Unique identifier for this decision node.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Optional condition group to evaluate. If null, the node is treated as always matching.
    /// </summary>
    public StrategyConditionGroup? If { get; init; }

    /// <summary>
    /// Child node to evaluate when the condition is true. Mutually exclusive with PlanGuid.
    /// </summary>
    public StrategyDecisionNode? Then { get; init; }

    /// <summary>
    /// Child node to evaluate when the condition is false. Only valid when Then is not null.
    /// </summary>
    public StrategyDecisionNode? Else { get; init; }

    /// <summary>
    /// Power plan GUID to apply when this is a leaf node. Mutually exclusive with Then/Else.
    /// </summary>
    public Guid? PlanGuid { get; init; }

    /// <summary>
    /// Whether this decision node is enabled in the strategy evaluation.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Validates the decision node structure.
    /// Throws if both PlanGuid and Then are set (they are mutually exclusive).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when PlanGuid and Then branch are both specified.
    /// </exception>
    public void Validate()
    {
        if (PlanGuid.HasValue && Then != null)
        {
            throw new InvalidOperationException(
                "StrategyDecisionNode cannot have both PlanGuid and Then branch. " +
                "Use PlanGuid for leaf nodes, or Then/Else for branching nodes.");
        }
    }
}
