#nullable enable

using AutoPower.Core.Core.Models;

namespace AutoPower.UI.Components;

internal static class DecisionTreeMutation
{
    public static StrategyDecisionNode AddThenBranch(
        StrategyDecisionNode root,
        Guid parentId,
        StrategyDecisionNode newThen,
        out bool changed)
    {
        var updated = UpdateNode(
            root,
            parentId,
            parent =>
            {
                if (parent.Then != null)
                {
                    return parent;
                }

                return parent with
                {
                    PlanGuid = null,
                    If = parent.If ?? StrategyConditionGroup.MatchAll(),
                    Then = newThen,
                };
            },
            out changed
        );

        if (changed)
        {
            ValidateTree(updated);
        }

        return updated;
    }

    public static StrategyDecisionNode AddElseBranch(
        StrategyDecisionNode root,
        Guid parentId,
        StrategyDecisionNode newElse,
        out bool changed)
    {
        var updated = UpdateNode(
            root,
            parentId,
            parent =>
            {
                if (parent.Then == null)
                {
                    return parent;
                }

                if (parent.Else != null)
                {
                    return parent;
                }

                return parent with
                {
                    PlanGuid = null,
                    Else = newElse,
                };
            },
            out changed
        );

        if (changed)
        {
            ValidateTree(updated);
        }

        return updated;
    }

    public static StrategyDecisionNode DeleteNode(
        StrategyDecisionNode root,
        Guid nodeId,
        out bool deleted)
    {
        deleted = false;

        if (root.Then?.Id == nodeId)
        {
            deleted = true;
            var updated = root with { Then = null };
            ValidateTree(updated);
            return updated;
        }

        if (root.Else?.Id == nodeId)
        {
            deleted = true;
            var updated = root with { Else = null };
            ValidateTree(updated);
            return updated;
        }

        var thenUpdated = root.Then;
        if (root.Then != null)
        {
            thenUpdated = DeleteNode(root.Then, nodeId, out var thenDeleted);
            deleted |= thenDeleted;
        }

        var elseUpdated = root.Else;
        if (root.Else != null)
        {
            elseUpdated = DeleteNode(root.Else, nodeId, out var elseDeleted);
            deleted |= elseDeleted;
        }

        var rebuilt = deleted ? root with { Then = thenUpdated, Else = elseUpdated } : root;

        if (deleted)
        {
            ValidateTree(rebuilt);
        }

        return rebuilt;
    }

    public static StrategyDecisionNode? FindNodeById(StrategyDecisionNode? root, Guid nodeId)
    {
        if (root == null)
        {
            return null;
        }

        if (root.Id == nodeId)
        {
            return root;
        }

        return FindNodeById(root.Then, nodeId) ?? FindNodeById(root.Else, nodeId);
    }

    private static StrategyDecisionNode UpdateNode(
        StrategyDecisionNode node,
        Guid targetId,
        Func<StrategyDecisionNode, StrategyDecisionNode> updater,
        out bool changed)
    {
        if (node.Id == targetId)
        {
            var updated = updater(node);
            changed = updated != node;
            return updated;
        }

        var thenChanged = false;
        var elseChanged = false;

        var updatedThen = node.Then;
        if (node.Then != null)
        {
            updatedThen = UpdateNode(node.Then, targetId, updater, out thenChanged);
        }

        var updatedElse = node.Else;
        if (node.Else != null)
        {
            updatedElse = UpdateNode(node.Else, targetId, updater, out elseChanged);
        }

        changed = thenChanged || elseChanged;
        return changed ? node with { Then = updatedThen, Else = updatedElse } : node;
    }

    private static void ValidateTree(StrategyDecisionNode node)
    {
        node.Validate();

        if (node.Then != null)
        {
            ValidateTree(node.Then);
        }

        if (node.Else != null)
        {
            ValidateTree(node.Else);
        }
    }
}
