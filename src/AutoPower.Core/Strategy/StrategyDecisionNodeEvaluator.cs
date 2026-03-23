#nullable enable

using AutoPower.Core.Core.Models;

namespace AutoPower.Core.Strategy;

/// <summary>
/// Evaluates a decision tree represented by StrategyDecisionNode.
/// Supports recursive evaluation of if-then-else branches.
/// </summary>
public static class StrategyDecisionNodeEvaluator
{
    /// <summary>
    /// Evaluates a decision node and returns the appropriate strategy decision.
    /// </summary>
    /// <param name="node">The root node of the decision tree to evaluate.</param>
    /// <param name="context">The evaluation context containing runtime state.</param>
    /// <returns>
    /// A StrategyDecision if a matching plan is found, or null if:
    /// - The node is null or disabled
    /// - The condition evaluates to Unknown
    /// - No valid path through the tree yields a decision
    /// </returns>
    public static StrategyDecision? Evaluate(
        StrategyDecisionNode? node,
        StrategyEvaluationContext context)
    {
        if (node is null || !node.IsEnabled)
        {
            return null;
        }

        // Leaf node: return plan directly
        if (node.PlanGuid.HasValue)
        {
            return new StrategyDecision
            {
                PlanGuid = node.PlanGuid.Value,
                Source = "Decision Tree",
                IsRuntimeDependent = false
            };
        }

        // No condition: evaluate Then branch directly
        if (node.If is null)
        {
            return Evaluate(node.Then, context);
        }

        // Evaluate condition
        var (result, isRuntimeDependent) = StrategyEvaluator.EvaluateGroup(node.If, context);

        return result switch
        {
            StrategyEvaluator.ConditionMatchResult.True => Evaluate(node.Then, context),
            StrategyEvaluator.ConditionMatchResult.False => Evaluate(node.Else, context),
            StrategyEvaluator.ConditionMatchResult.Unknown => null, // Skip branch
            _ => null
        };
    }
}
