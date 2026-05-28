#nullable enable

using Aprillz.MewUI;
using AutoPower.Core.Core.Models;

namespace AutoPower.UI.ViewModels;

/// <summary>
/// ViewModel for editing a single condition group.
/// Manages the group's operator, conditions, and nested groups.
/// </summary>
public sealed class ConditionGroupEditorViewModel
{
    /// <summary>
    /// The condition group being edited.
    /// </summary>
    public ObservableValue<StrategyConditionGroup?> Group { get; } = new(null);

    /// <summary>
    /// The operator for the condition group (All, Any, None).
    /// </summary>
    public ObservableValue<StrategyConditionGroupOperator> Operator { get; } = new(StrategyConditionGroupOperator.All);

    /// <summary>
    /// The operator index for ComboBox binding (0=All, 1=Any, 2=None).
    /// </summary>
    public ObservableValue<int> OperatorIndex { get; } = new(0);

    /// <summary>
    /// Display label for the operator selection.
    /// </summary>
    public ObservableValue<string> OperatorLabel { get; } = new("All");

    /// <summary>
    /// Indicates whether the group has any conditions or nested groups.
    /// </summary>
    public ObservableValue<bool> HasContent { get; } = new(false);

    /// <summary>
    /// Count of conditions in the group.
    /// </summary>
    public ObservableValue<int> ConditionCount { get; } = new(0);

    /// <summary>
    /// Count of nested groups in the group.
    /// </summary>
    public ObservableValue<int> NestedGroupCount { get; } = new(0);

    /// <summary>
    /// Summary text describing the group's contents.
    /// </summary>
    public ObservableValue<string> SummaryText { get; } = new("Empty group");

    /// <summary>
    /// Whether this is the root group (cannot be deleted).
    /// </summary>
    public ObservableValue<bool> IsRoot { get; } = new(false);

    /// <summary>
    /// Unique identifier for this editor instance.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    public ConditionGroupEditorViewModel()
    {
        // Subscribe to state changes
        Group.Subscribe(OnGroupChanged);
        Operator.Subscribe(OnOperatorChanged);
        OperatorIndex.Subscribe(OnOperatorIndexChanged);
    }

    /// <summary>
    /// Loads a condition group into the editor.
    /// </summary>
    public void LoadGroup(StrategyConditionGroup? group, bool isRoot = false)
    {
        IsRoot.Value = isRoot;
        Group.Value = group;
        
        if (group != null)
        {
            Operator.Value = group.Operator;
        }
        else
        {
            Operator.Value = StrategyConditionGroupOperator.All;
        }
    }

    /// <summary>
    /// Creates a new empty condition group.
    /// </summary>
    public void CreateNewGroup()
    {
        Group.Value = new StrategyConditionGroup();
        Operator.Value = StrategyConditionGroupOperator.All;
    }

    /// <summary>
    /// Sets the operator for the condition group.
    /// </summary>
    public void SetOperator(StrategyConditionGroupOperator op)
    {
        Operator.Value = op;
        UpdateGroupFromState();
    }

    /// <summary>
    /// Sets the operator by index (for ComboBox binding).
    /// </summary>
    public void SetOperatorByIndex(int index)
    {
        var op = index switch
        {
            1 => StrategyConditionGroupOperator.Any,
            2 => StrategyConditionGroupOperator.None,
            _ => StrategyConditionGroupOperator.All,
        };
        SetOperator(op);
    }

    /// <summary>
    /// Adds a new condition to the group.
    /// </summary>
    public void AddCondition(StrategyConditionType type)
    {
        if (Group.Value == null)
        {
            CreateNewGroup();
        }

        var condition = new StrategyCondition { Type = type };
        var updatedConditions = new List<StrategyCondition>(Group.Value!.Conditions) { condition };
        
        Group.Value = Group.Value with { Conditions = updatedConditions };
        UpdateDerivedState();
    }

    /// <summary>
    /// Removes a condition from the group.
    /// </summary>
    public void RemoveCondition(Guid conditionId)
    {
        if (Group.Value == null) return;

        var updatedConditions = Group.Value.Conditions
            .Where(c => c.Id != conditionId)
            .ToList();

        Group.Value = Group.Value with { Conditions = updatedConditions };
        UpdateDerivedState();
    }

    /// <summary>
    /// Adds a nested group to this group.
    /// </summary>
    public void AddNestedGroup()
    {
        if (Group.Value == null)
        {
            CreateNewGroup();
        }

        var nestedGroup = new StrategyConditionGroup();
        var updatedGroups = new List<StrategyConditionGroup>(Group.Value!.Groups) { nestedGroup };
        
        Group.Value = Group.Value with { Groups = updatedGroups };
        UpdateDerivedState();
    }

    /// <summary>
    /// Removes a nested group from this group.
    /// </summary>
    public void RemoveNestedGroup(Guid groupId)
    {
        if (Group.Value == null) return;

        var updatedGroups = Group.Value.Groups
            .Where(g => g.Id != groupId)
            .ToList();

        Group.Value = Group.Value with { Groups = updatedGroups };
        UpdateDerivedState();
    }

    /// <summary>
    /// Gets the current condition group state.
    /// </summary>
    public StrategyConditionGroup? GetCurrentGroup()
    {
        return Group.Value;
    }

    /// <summary>
    /// Builds a condition group from the current editor state.
    /// </summary>
    public StrategyConditionGroup BuildConditionGroup()
    {
        if (Group.Value == null)
        {
            return new StrategyConditionGroup { Operator = Operator.Value };
        }

        return Group.Value with { Operator = Operator.Value };
    }

    private void OnGroupChanged()
    {
        UpdateDerivedState();
    }

    private void OnOperatorChanged()
    {
        OperatorLabel.Value = Operator.Value switch
        {
            StrategyConditionGroupOperator.Any => "Any",
            StrategyConditionGroupOperator.None => "None",
            _ => "All",
        };
        
        OperatorIndex.Value = Operator.Value switch
        {
            StrategyConditionGroupOperator.Any => 1,
            StrategyConditionGroupOperator.None => 2,
            _ => 0,
        };
    }

    private void OnOperatorIndexChanged()
    {
        Operator.Value = OperatorIndex.Value switch
        {
            1 => StrategyConditionGroupOperator.Any,
            2 => StrategyConditionGroupOperator.None,
            _ => StrategyConditionGroupOperator.All,
        };
    }

    private void UpdateGroupFromState()
    {
        if (Group.Value == null) return;
        Group.Value = Group.Value with { Operator = Operator.Value };
    }

    private void UpdateDerivedState()
    {
        var group = Group.Value;
        if (group == null)
        {
            HasContent.Value = false;
            ConditionCount.Value = 0;
            NestedGroupCount.Value = 0;
            SummaryText.Value = "Empty group";
            return;
        }

        ConditionCount.Value = group.Conditions.Count;
        NestedGroupCount.Value = group.Groups.Count;
        HasContent.Value = group.Conditions.Count > 0 || group.Groups.Count > 0;

        var parts = new List<string>();
        if (group.Conditions.Count > 0)
        {
            parts.Add($"{group.Conditions.Count} condition{(group.Conditions.Count != 1 ? "s" : "")}");
        }
        if (group.Groups.Count > 0)
        {
            parts.Add($"{group.Groups.Count} group{(group.Groups.Count != 1 ? "s" : "")}");
        }

        SummaryText.Value = parts.Count > 0 ? string.Join(", ", parts) : "Empty group";
    }
}
