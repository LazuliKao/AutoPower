#nullable enable

using Aprillz.MewUI;
using AutoPower.Core.Core.Models;
using AutoPower.UI.Components;

namespace AutoPower.UI.ViewModels;

/// <summary>
/// ViewModel for the decision tree editor.
/// Manages the root node, selected node, and view mode state.
/// </summary>
public sealed class DecisionTreeViewModel
{
    /// <summary>
    /// The root node of the decision tree.
    /// </summary>
    public ObservableValue<StrategyDecisionNode?> Root { get; } = new(null);

    /// <summary>
    /// The currently selected node in the tree.
    /// </summary>
    public ObservableValue<StrategyDecisionNode?> SelectedNode { get; } = new(null);

    /// <summary>
    /// The current view mode (Card, Flowchart, or JSON preview).
    /// </summary>
    public ObservableValue<DecisionTreeViewMode> CurrentView { get; } = new(DecisionTreeViewMode.Card);

    /// <summary>
    /// Indicates whether the tree has any nodes.
    /// </summary>
    public ObservableValue<bool> HasNodes { get; } = new(false);

    /// <summary>
    /// Indicates whether a node is currently selected.
    /// </summary>
    public ObservableValue<bool> HasSelection { get; } = new(false);

    /// <summary>
    /// Count of total nodes in the tree.
    /// </summary>
    public ObservableValue<int> NodeCount { get; } = new(0);

    public DecisionTreeViewModel()
    {
        // Subscribe to Root changes to update derived state
        Root.Subscribe(UpdateDerivedState);
        SelectedNode.Subscribe(UpdateSelectionState);
    }

    /// <summary>
    /// Loads a decision tree from a root node.
    /// </summary>
    public void LoadTree(StrategyDecisionNode? root)
    {
        Root.Value = root;
        SelectedNode.Value = null;
    }

    /// <summary>
    /// Updates the tree root without resetting selection.
    /// If the selected node was mutated, updates the selection reference by matching ID.
    /// </summary>
    public void UpdateTree(StrategyDecisionNode? root)
    {
        var prevSelectedId = SelectedNode.Value?.Id;
        Root.Value = root;
        if (prevSelectedId.HasValue && root != null)
        {
            var newSelectedNode = DecisionTreeMutation.FindNodeById(root, prevSelectedId.Value);
            SelectedNode.Value = newSelectedNode;
        }
        else
        {
            SelectedNode.Value = null;
        }
    }

    /// <summary>
    /// Clears the decision tree.
    /// </summary>
    public void ClearTree()
    {
        Root.Value = null;
        SelectedNode.Value = null;
    }

    /// <summary>
    /// Selects a node in the tree.
    /// </summary>
    public void SelectNode(StrategyDecisionNode? node)
    {
        SelectedNode.Value = node;
    }



    /// <summary>
    /// Switches to JSON preview mode.
    /// </summary>
    public void SwitchToJsonView()
    {
        CurrentView.Value = DecisionTreeViewMode.Json;
    }

    private void UpdateDerivedState()
    {
        HasNodes.Value = Root.Value != null;
        NodeCount.Value = Root.Value != null ? CountNodes(Root.Value) : 0;
    }

    private void UpdateSelectionState()
    {
        HasSelection.Value = SelectedNode.Value != null;
    }

    private static int CountNodes(StrategyDecisionNode node)
    {
        var count = 1;
        if (node.Then != null)
        {
            count += CountNodes(node.Then);
        }
        if (node.Else != null)
        {
            count += CountNodes(node.Else);
        }
        return count;
    }
}

/// <summary>
/// View modes for the decision tree editor.
/// </summary>
public enum DecisionTreeViewMode
{
    Card,
    Flowchart,
    Json,
}
