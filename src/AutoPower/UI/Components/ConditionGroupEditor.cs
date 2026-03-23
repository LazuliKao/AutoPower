#nullable enable

using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using AutoPower.Core.Core.Models;
using AutoPower.UI.ViewModels;

namespace AutoPower.UI.Components;

/// <summary>
/// UserControl for editing a condition group with operator selection,
/// condition list, and nested group support.
/// </summary>
public sealed class ConditionGroupEditor : UserControl
{
    private readonly ConditionGroupEditorViewModel _vm;

    // Colors matching SettingsWindow theme
    private static readonly Color SurfaceCard = Color.FromHex("#1C2333");
    private static readonly Color SurfaceInput = Color.FromHex("#121723");
    private static readonly Color BorderColor = Color.FromHex("#2E374A");
    private static readonly Color TextPrimary = Color.FromHex("#EAF0FF");
    private static readonly Color TextMuted = Color.FromHex("#9AA7BF");
    private static readonly Color AccentColor = Color.FromHex("#FF4F9A");
    private static readonly Color DangerColor = Color.FromHex("#D85A76");

    /// <summary>
    /// Event raised when the group content changes.
    /// </summary>
    public event Action? GroupChanged;

    /// <summary>
    /// Event raised when a delete is requested (for non-root groups).
    /// </summary>
    public event Action? DeleteRequested;

    /// <summary>
    /// Gets the ViewModel for binding access.
    /// </summary>
    public ConditionGroupEditorViewModel ViewModel => _vm;

    public ConditionGroupEditor()
    {
        _vm = new ConditionGroupEditorViewModel();
        Build();
    }

    /// <summary>
    /// Loads a condition group into the editor.
    /// </summary>
    public void LoadGroup(StrategyConditionGroup? group, bool isRoot = false)
    {
        _vm.LoadGroup(group, isRoot);
    }

    /// <summary>
    /// Gets the current condition group.
    /// </summary>
    public StrategyConditionGroup? GetGroup()
    {
        return _vm.BuildConditionGroup();
    }

    protected override Element? OnBuild()
    {
        // Operator ComboBox
        var operatorComboBox = new ComboBox()
            .Items(new[] { "All", "Any", "None" })
            .BindSelectedIndex(_vm.Operator, o => (int)o)
            .Width(92)
            .Height(26)
            .Padding(6, 3)
            .Background(SurfaceInput)
            .Foreground(TextPrimary)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .FontFamily("Consolas");

        // Add Condition button
        var addConditionButton = CreateCompactButton(
            "+ Cond",
            () =>
            {
                _vm.AddCondition(StrategyConditionType.DayType);
                Rebuild();
                GroupChanged?.Invoke();
            })
            .Width(56);

        // Add Group button
        var addGroupButton = CreateCompactButton(
            "+ Group",
            () =>
            {
                _vm.AddNestedGroup();
                Rebuild();
                GroupChanged?.Invoke();
            })
            .Width(56);

        // Header row
        var headerLeft = new StackPanel()
            .Horizontal()
            .Spacing(6)
            .Children(
                new Label()
                    .BindText(_vm.IsRoot, isRoot => isRoot ? "Root Group" : "Group")
                    .FontFamily("Bahnschrift")
                    .SemiBold()
                    .Foreground(TextPrimary),
                new Label()
                    .BindText(_vm.Group, g => g != null ? $"ID {g.Id.ToString()[..8]}" : "")
                    .FontFamily("Consolas")
                    .FontSize(10)
                    .Foreground(TextMuted)
            );

        var headerActions = new StackPanel()
            .Horizontal()
            .Spacing(6)
            .Children(operatorComboBox, addConditionButton, addGroupButton);

        // Delete button (only for non-root groups)
        var deleteButton = CreateCompactDangerButton(
            "Del",
            () =>
            {
                DeleteRequested?.Invoke();
            })
            .Width(42);

        // Build header with conditional delete button
        var headerElements = new List<Element>
        {
            headerLeft,
            new Label()
                .Text("Operator")
                .FontFamily("Consolas")
                .FontSize(10)
                .Foreground(TextMuted),
        };

        // Conditionally add delete button to actions
        var headerRow = new StackPanel()
            .Vertical()
            .Spacing(4)
            .Children(
                headerLeft,
                new Label()
                    .Text("Operator")
                    .FontFamily("Consolas")
                    .FontSize(10)
                    .Foreground(TextMuted),
                new StackPanel()
                    .Horizontal()
                    .Spacing(6)
                    .Children(BuildHeaderActions())
            );

        // Build content rows (conditions and nested groups)
        var rows = new List<Element> { headerRow };
        rows.AddRange(BuildConditionRows());
        rows.AddRange(BuildNestedGroupRows());

        // Empty state
        if (!_vm.HasContent.Value)
        {
            rows.Add(
                new Label()
                    .Text("No conditions yet. Add a leaf or nested group.")
                    .FontFamily("Consolas")
                    .FontSize(10)
                    .Foreground(TextMuted)
            );
        }

        return new Border()
            .Background(SurfaceCard)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .CornerRadius(8)
            .Padding(10)
            .Child(
                new StackPanel()
                    .Vertical()
                    .Spacing(8)
                    .Children(rows.ToArray())
            );
    }

    private Element[] BuildHeaderActions()
    {
        var operatorComboBox = new ComboBox()
            .Items(new[] { "All", "Any", "None" })
            .BindSelectedIndex(_vm.Operator, o => (int)o)
            .Width(92)
            .Height(26)
            .Padding(6, 3)
            .Background(SurfaceInput)
            .Foreground(TextPrimary)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .FontFamily("Consolas");

        var addConditionButton = CreateCompactButton(
            "+ Cond",
            () =>
            {
                _vm.AddCondition(StrategyConditionType.DayType);
                Rebuild();
                GroupChanged?.Invoke();
            })
            .Width(56);

        var addGroupButton = CreateCompactButton(
            "+ Group",
            () =>
            {
                _vm.AddNestedGroup();
                Rebuild();
                GroupChanged?.Invoke();
            })
            .Width(56);

        var actions = new List<Element> { operatorComboBox, addConditionButton, addGroupButton };

        // Only add delete button for non-root groups
        if (!_vm.IsRoot.Value)
        {
            actions.Add(
                CreateCompactDangerButton(
                    "Del",
                    () => DeleteRequested?.Invoke())
                    .Width(42)
            );
        }

        return actions.ToArray();
    }

    private Element[] BuildConditionRows()
    {
        var group = _vm.Group.Value;
        if (group == null || group.Conditions.Count == 0)
        {
            return Array.Empty<Element>();
        }

        var rows = new List<Element>();
        foreach (var condition in group.Conditions)
        {
            rows.Add(BuildConditionRow(condition));
        }

        return rows.ToArray();
    }

    private Element BuildConditionRow(StrategyCondition condition)
    {
        var typeComboBox = new ComboBox()
            .Items(new[] { "DayType", "TimeRange", "KeyboardMouseIdle", "MonitorOff" })
            .SelectedIndex((int)condition.Type)
            .Width(136)
            .Height(26)
            .Padding(6, 3)
            .Background(SurfaceInput)
            .Foreground(TextPrimary)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .FontFamily("Consolas");

        var inputs = BuildConditionInputs(condition);

        var applyButton = CreateCompactButton(
            "Apply",
            () =>
            {
                Rebuild();
                GroupChanged?.Invoke();
            })
            .Width(52);

        var removeButton = CreateCompactDangerButton(
            "Del",
            () =>
            {
                _vm.RemoveCondition(condition.Id);
                Rebuild();
                GroupChanged?.Invoke();
            })
            .Width(42);

        return new Border()
            .Background(SurfaceInput)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .CornerRadius(6)
            .Padding(6)
            .Child(
                new StackPanel()
                    .Horizontal()
                    .Spacing(6)
                    .Children(typeComboBox, inputs, applyButton, removeButton)
            );
    }

    private Element BuildConditionInputs(StrategyCondition condition)
    {
        return condition.Type switch
        {
            StrategyConditionType.DayType => BuildDayTypeInput(condition),
            StrategyConditionType.TimeRange => BuildTimeRangeInput(condition),
            StrategyConditionType.KeyboardMouseIdle => new Label()
                .Text("Keyboard/mouse idle")
                .FontFamily("Consolas")
                .FontSize(10)
                .Foreground(TextMuted),
            StrategyConditionType.MonitorOff => new Label()
                .Text("Monitor off")
                .FontFamily("Consolas")
                .FontSize(10)
                .Foreground(TextMuted),
            _ => new Label()
                .Text("Unsupported condition")
                .FontFamily("Consolas")
                .FontSize(10)
                .Foreground(TextMuted),
        };
    }

    private Element BuildDayTypeInput(StrategyCondition condition)
    {
        return new ComboBox()
            .Items(new[] { "All", "Weekday", "Weekend" })
            .SelectedIndex((int)condition.DayType)
            .Width(120)
            .Height(26)
            .Padding(6, 3)
            .Background(SurfaceInput)
            .Foreground(TextPrimary)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .FontFamily("Consolas");
    }

    private Element BuildTimeRangeInput(StrategyCondition condition)
    {
        var startTextBox = new TextBox()
            .Text(condition.Start.ToString("HH:mm"))
            .Width(84)
            .Height(26)
            .Padding(6, 3)
            .Background(SurfaceInput)
            .Foreground(TextPrimary)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .FontFamily("Consolas")
            .Placeholder("Start");

        var endTextBox = new TextBox()
            .Text(condition.End.ToString("HH:mm"))
            .Width(84)
            .Height(26)
            .Padding(6, 3)
            .Background(SurfaceInput)
            .Foreground(TextPrimary)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .FontFamily("Consolas")
            .Placeholder("End");

        return new StackPanel()
            .Horizontal()
            .Spacing(6)
            .Children(
                startTextBox,
                new Label()
                    .Text("to")
                    .FontFamily("Consolas")
                    .FontSize(10)
                    .Foreground(TextMuted),
                endTextBox
            );
    }

    private Element[] BuildNestedGroupRows()
    {
        var group = _vm.Group.Value;
        if (group == null || group.Groups.Count == 0)
        {
            return Array.Empty<Element>();
        }

        var rows = new List<Element>();
        foreach (var nestedGroup in group.Groups)
        {
            rows.Add(BuildNestedGroupEditor(nestedGroup));
        }

        return rows.ToArray();
    }

    private Element BuildNestedGroupEditor(StrategyConditionGroup nestedGroup)
    {
        var nestedEditor = new ConditionGroupEditor();
        nestedEditor.LoadGroup(nestedGroup, isRoot: false);
        nestedEditor.GroupChanged += () =>
        {
            GroupChanged?.Invoke();
        };
        nestedEditor.DeleteRequested += () =>
        {
            _vm.RemoveNestedGroup(nestedGroup.Id);
            Rebuild();
            GroupChanged?.Invoke();
        };

        return new Border()
            .Margin(12, 0, 0, 0)
            .Child(nestedEditor);
    }

    private void Rebuild()
    {
        // Rebuild the control to reflect state changes
        Build();
    }

    private static Button CreateCompactButton(string text, Action onClick)
    {
        return new Button()
            .Content(text)
            .OnClick(onClick)
            .Height(26)
            .Padding(6, 3)
            .Background(AccentColor)
            .Foreground(Color.White)
            .BorderBrush(AccentColor)
            .BorderThickness(1)
            .FontFamily("Bahnschrift")
            .FontSize(11)
            .SemiBold();
    }

    private static Button CreateCompactDangerButton(string text, Action onClick)
    {
        return new Button()
            .Content(text)
            .OnClick(onClick)
            .Height(26)
            .Padding(6, 3)
            .Background(SurfaceInput)
            .Foreground(DangerColor)
            .BorderBrush(DangerColor)
            .BorderThickness(1)
            .FontFamily("Bahnschrift")
            .FontSize(11)
            .SemiBold();
    }
}
