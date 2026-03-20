using System.Linq;
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.Diagnostics;
using AutoPower.Core.Core.Models;
using AutoPower.Core.Infrastructure;
using AutoPower.Core.Strategy;

namespace AutoPower.UI;

internal sealed class SettingsWindow
{
    private static readonly Color WindowBackground = Color.FromHex("#0F1117");
    private static readonly Color SurfacePanel = Color.FromHex("#171C28");
    private static readonly Color SurfaceCard = Color.FromHex("#1C2333");
    private static readonly Color SurfaceInput = Color.FromHex("#121723");
    private static readonly Color BorderColor = Color.FromHex("#2E374A");
    private static readonly Color DividerColor = Color.FromHex("#293143");
    private static readonly Color TextPrimary = Color.FromHex("#EAF0FF");
    private static readonly Color TextMuted = Color.FromHex("#9AA7BF");
    private static readonly Color AccentColor = Color.FromHex("#FF4F9A");
    private static readonly Color DangerColor = Color.FromHex("#D85A76");

    private Window? _window;
    private bool _isOpen;
    private AppConfig _config = null!;
    private List<PowerPlanInfo> _plans = null!;

    private RadioButton? _modeKeyboardMouse;
    private RadioButton? _modeMonitorSleep;
    private RadioButton? _modeBoth;
    private TextBox? _idleTimeoutTextBox;
    private ComboBox? _activePlanComboBox;
    private ComboBox? _idlePlanComboBox;
    private ComboBox? _defaultPlanComboBox;
    private CheckBox? _autoStartCheckBox;

    private List<StrategyRule> _rules = new();
    private readonly List<RuleEditorControls> _ruleEditors = new();

    private Label? _rulesSummaryLabel;
    private Border? _rulesContainer;

    private Label? _overrideStatusLabel;
    private ComboBox? _overridePlanComboBox;
    private TextBox? _overrideTtlTextBox;

    private ScrollViewer? _previewScrollViewer;
    private CheckBox? _previewKeyboardMouseIdleCheckBox;
    private CheckBox? _previewMonitorOffCheckBox;

    internal event Action<string, string>? OnNotificationRequested;
    internal event Action<AppConfig>? OnConfigSaved;

    internal void Show(AppConfig config, List<PowerPlanInfo> plans)
    {
        if (_isOpen && _window != null)
        {
            _window.Activate();
            return;
        }

        _config = config;
        _plans = plans;
        _rules = new(config.Rules);

        _window = new Window()
            .Padding(0)
            .Title("AutoPower Settings")
            .Resizable(600, 660)
            .Content(CreateContent());

        _window.Closed += () => _isOpen = false;
        _isOpen = true;

        Application
            .Create()
            .UseAccent(Accent.Pink)
            .UseTheme(ThemeVariant.Dark)
            .UseWin32()
            .UseMewVGWin32()
            .Run(_window);
        Application.Quit();
    }

    private Element CreateContent()
    {
        var tabControl = new TabControl().TabItems(
            new TabItem().Header("General").Content(CreateGeneralTabContent()),
            new TabItem().Header("Schedule").Content(CreateScheduleTabContent()),
            new TabItem().Header("Override").Content(CreateOverrideTabContent()),
            new TabItem().Header("Preview").Content(CreatePreviewTabContent()),
            new TabItem().Header("About").Content(CreateAboutTabContent())
        );

        tabControl
            .Background(SurfacePanel)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .Foreground(TextPrimary);

        var saveButton = CreatePrimaryButton("Save", OnSaveClicked).Width(128);

        var shell = new DockPanel().Children(
            new Border()
                .DockTop()
                .Background(SurfacePanel)
                .BorderBrush(BorderColor)
                .BorderThickness(1)
                .CornerRadius(10)
                .Padding(14)
                .Child(
                    new DockPanel().Children(
                        saveButton.DockRight(),
                        new StackPanel()
                            .Vertical()
                            .Spacing(4)
                            .Children(
                                new Label()
                                    .Text("AutoPower Settings")
                                    .FontSize(21)
                                    .Bold()
                                    .FontFamily("Bahnschrift")
                                    .Foreground(TextPrimary),
                                new Label()
                                    .Text(
                                        "Industrial dark control surface for power strategy and overrides"
                                    )
                                    .FontSize(11)
                                    .FontFamily("Consolas")
                                    .Foreground(TextMuted)
                            )
                    )
                ),
            new ScrollViewer()
                .VerticalScroll(ScrollMode.Auto)
                .HorizontalScroll(ScrollMode.Disabled)
                .Content(tabControl)
                .DockTop()
        );

        return DelayLoadingContent(() => new Border().Background(WindowBackground).Child(shell));
    }

    private UIElement DelayLoadingContent(Func<UIElement> createContent)
    {
        var placeholder = new Border()
            .Child(
                new TextBlock()
                {
                    Text = "Loading...",
                    FontSize = 40,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                }
            )
            .WithTheme(
                (theme, self) =>
                {
                    self.Foreground = theme.Palette.Accent;
                }
            );
        Task.Delay(500)
            .ContinueWith(_ =>
            {
                Application.Current.Dispatcher?.BeginInvoke(() =>
                {
                    placeholder.Child = createContent();
                });
            })
            .ConfigureAwait(false);
        return placeholder;
    }

    #region General Tab

    private Element CreateGeneralTabContent()
    {
        _modeKeyboardMouse = new RadioButton()
            .Text("Keyboard/Mouse")
            .GroupName("DetectionMode")
            .IsChecked(_config.Mode == DetectionMode.KeyboardMouse)
            .Foreground(TextPrimary)
            .FontFamily("Consolas");

        _modeMonitorSleep = new RadioButton()
            .Text("Monitor Sleep")
            .GroupName("DetectionMode")
            .IsChecked(_config.Mode == DetectionMode.MonitorSleep)
            .Foreground(TextPrimary)
            .FontFamily("Consolas");

        _modeBoth = new RadioButton()
            .Text("Both")
            .GroupName("DetectionMode")
            .IsChecked(_config.Mode == DetectionMode.Both)
            .Foreground(TextPrimary)
            .FontFamily("Consolas");

        _idleTimeoutTextBox = StyleInput(
            new TextBox()
                .Text(_config.IdleTimeoutMinutes.ToString())
                .Width(120)
                .Placeholder("Minutes")
        );

        var planNames = new List<string>();
        var planGuids = new List<Guid>();
        foreach (var plan in _plans)
        {
            planNames.Add(plan.Name);
            planGuids.Add(plan.Guid);
        }

        _activePlanComboBox = StyleInput(
            new ComboBox()
                .Items(planNames.ToArray())
                .SelectedIndex(GetPlanIndex(planGuids, _config.ActivePlanGuid))
                .MinWidth(280)
        );

        _idlePlanComboBox = StyleInput(
            new ComboBox()
                .Items(planNames.ToArray())
                .SelectedIndex(GetPlanIndex(planGuids, _config.IdlePlanGuid))
                .MinWidth(280)
        );

        _defaultPlanComboBox = StyleInput(
            new ComboBox()
                .Items(new[] { "None" }.Concat(planNames).ToArray())
                .SelectedIndex(GetOptionalPlanIndex(planGuids, _config.DefaultPlanGuid))
                .MinWidth(280)
        );

        _autoStartCheckBox = new CheckBox()
            .Text("Start AutoPower when Windows starts")
            .IsChecked(_config.AutoStartEnabled)
            .Foreground(TextPrimary)
            .FontFamily("Consolas");

        return new StackPanel()
            .Vertical()
            .Spacing(12)
            .Padding(8)
            .Children(
                CreateSectionCard(
                    "Detection Strategy",
                    "Select which runtime detectors are available to rule conditions and fallback.",
                    _modeKeyboardMouse,
                    _modeMonitorSleep,
                    _modeBoth
                ),
                CreateSectionCard(
                    "Power Transition",
                    "Tune idle threshold and map plans for rule default and final fallback.",
                    new StackPanel()
                        .Horizontal()
                        .Spacing(8)
                        .Children(
                            CreateFieldLabel("Idle timeout (minutes)").MinWidth(170),
                            _idleTimeoutTextBox
                        ),
                    CreateDivider(),
                    CreateFieldBlock("Default plan", _defaultPlanComboBox),
                    CreateFieldBlock("Active fallback plan", _activePlanComboBox),
                    CreateFieldBlock("Idle fallback plan", _idlePlanComboBox)
                ),
                CreateSectionCard(
                    "Startup",
                    "Control integration with Windows startup.",
                    _autoStartCheckBox
                )
            );
    }

    #endregion

    #region Schedule Tab

    private Element CreateScheduleTabContent()
    {
        var addButton = CreatePrimaryButton("Add Rule", OnAddRuleClicked).Width(112);

        _rulesSummaryLabel = new Label()
            .Text(FormatRulesSummary())
            .FontFamily("Consolas")
            .FontSize(11)
            .Foreground(TextMuted);

        _rulesContainer = new Border()
            .Background(SurfaceInput)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .CornerRadius(8)
            .Padding(8)
            .Child(BuildRulesPanel());

        return new StackPanel()
            .Vertical()
            .Spacing(12)
            .Padding(8)
            .Children(
                CreateSectionCard(
                    "Strategy Rules",
                    "Each rule selects a plan and evaluates a condition group. UI currently edits one condition group per rule with time, day, keyboard/mouse idle, and monitor-off leaves.",
                    _rulesSummaryLabel,
                    _rulesContainer,
                    new StackPanel().Horizontal().Spacing(8).Children(addButton)
                )
            );
    }

    private StackPanel BuildRulesPanel()
    {
        _ruleEditors.Clear();
        var elements = new List<Element>();
        var planNames = new List<string>();
        var planGuids = new List<Guid>();

        foreach (var plan in _plans)
        {
            planNames.Add(plan.Name);
            planGuids.Add(plan.Guid);
        }

        for (var i = 0; i < _rules.Count; i++)
        {
            var rule = _rules[i];
            var enabledCheckBox = new CheckBox()
                .Text("Enabled")
                .IsChecked(rule.IsEnabled)
                .Foreground(TextPrimary)
                .FontFamily("Consolas");
            var nameTextBox = StyleCompactInput(
                new TextBox().Text(rule.Name).Placeholder("Rule name").MinWidth(220)
            );
            var priorityTextBox = StyleCompactInput(
                new TextBox().Text(rule.Priority.ToString()).Width(72).Placeholder("Priority")
            );
            var planComboBox = StyleCompactInput(
                new ComboBox()
                    .Items(planNames.ToArray())
                    .SelectedIndex(GetPlanIndex(planGuids, rule.TargetPlanGuid))
                    .MinWidth(220)
            );

            var editor = new RuleEditorControls(
                rule,
                enabledCheckBox,
                nameTextBox,
                priorityTextBox,
                planComboBox,
                CreateGroupStateFromModel(rule.Condition)
            );
            _ruleEditors.Add(editor);

            var removeButton = CreateCompactDangerButton("Del", () => OnRemoveRuleClicked(rule.Id))
                .Width(42);

            var headerLeft = new StackPanel()
                .Horizontal()
                .Spacing(6)
                .Children(
                    new Label()
                        .Text($"Rule {i + 1}")
                        .Bold()
                        .FontFamily("Bahnschrift")
                        .Foreground(TextPrimary),
                    new Label()
                        .Text($"ID {rule.Id.ToString()[..8]}")
                        .FontSize(11)
                        .FontFamily("Consolas")
                        .Foreground(TextMuted)
                );

            var headerRow = new StackPanel()
                .Vertical()
                .Spacing(4)
                .Children(
                    headerLeft,
                    new StackPanel().Horizontal().Spacing(6).Children(enabledCheckBox, removeButton)
                );

            var detailsRow = new StackPanel()
                .Horizontal()
                .Spacing(8)
                .Children(
                    CreateInlineField("Name", nameTextBox, 48),
                    CreateInlineField("Priority", priorityTextBox, 58)
                );

            var planRow = new StackPanel()
                .Horizontal()
                .Spacing(8)
                .Children(CreateInlineField("Target plan", planComboBox, 80));

            var treeEditor = BuildGroupPanel(editor, editor.RootGroupState, 0, isRoot: true);

            var rulePanel = new Border()
                .Background(SurfaceCard)
                .BorderBrush(BorderColor)
                .BorderThickness(1)
                .CornerRadius(8)
                .Padding(8)
                .Child(
                    new StackPanel()
                        .Vertical()
                        .Spacing(6)
                        .Children(headerRow, CreateDivider(), detailsRow, planRow, treeEditor)
                );

            elements.Add(rulePanel);
        }

        if (_rules.Count == 0)
        {
            elements.Add(
                new Border()
                    .Background(SurfaceCard)
                    .BorderBrush(BorderColor)
                    .BorderThickness(1)
                    .CornerRadius(8)
                    .Padding(14)
                    .Child(
                        new Label()
                            .Text(
                                "No rules defined yet. Click 'Add Rule' to create your first action rule."
                            )
                            .FontFamily("Consolas")
                            .Foreground(TextMuted)
                    )
            );
        }

        return new StackPanel().Vertical().Spacing(8).Children(elements.ToArray());
    }

    private void OnAddRuleClicked()
    {
        SyncRulesFromEditors();

        var newRule = new StrategyRule
        {
            Name = $"Rule {_rules.Count + 1}",
            Condition = StrategyConditionGroup.ForSchedule(DayType.All, new(9, 0), new(17, 0)),
            TargetPlanGuid = _config.DefaultPlanGuid ?? _config.ActivePlanGuid,
            Priority = _rules.Count + 1,
            IsEnabled = true,
        };

        _rules.Add(newRule);
        RebuildScheduleRulesPanel();
    }

    private void OnRemoveRuleClicked(Guid ruleId)
    {
        SyncRulesFromEditors();
        _rules.RemoveAll(rule => rule.Id == ruleId);
        RebuildScheduleRulesPanel();
    }

    private void RebuildScheduleRulesPanel()
    {
        if (_rulesContainer is null)
            return;

        _rulesContainer.Child = BuildRulesPanel();
        _rulesSummaryLabel?.Text(FormatRulesSummary());
        RefreshPreviewTab();
    }

    private string FormatRulesSummary()
    {
        return _rules.Count == 1 ? "1 rule configured" : $"{_rules.Count} rules configured";
    }

    private void SyncRulesFromEditors()
    {
        if (_ruleEditors.Count == 0)
            return;

        var updatedRules = new List<StrategyRule>(_ruleEditors.Count);
        foreach (var editor in _ruleEditors)
        {
            var source = editor.SourceRule;
            var selectedPlanIndex = editor.PlanComboBox.SelectedIndex;
            var selectedPlanGuid = source.TargetPlanGuid;
            if (selectedPlanIndex >= 0 && selectedPlanIndex < _plans.Count)
            {
                selectedPlanGuid = _plans[selectedPlanIndex].Guid;
            }

            var ruleName = string.IsNullOrWhiteSpace(editor.NameTextBox.Text)
                ? source.Name
                : editor.NameTextBox.Text.Trim();
            var priority = source.Priority;
            if (int.TryParse(editor.PriorityTextBox.Text, out var parsedPriority))
            {
                priority = Math.Max(0, parsedPriority);
            }

            var conditionGroup = BuildConditionGroup(editor.RootGroupState);

            updatedRules.Add(
                source with
                {
                    Name = ruleName,
                    Priority = priority,
                    Condition = conditionGroup,
                    TargetPlanGuid = selectedPlanGuid,
                    IsEnabled = editor.EnabledCheckBox.IsChecked ?? source.IsEnabled,
                }
            );
        }

        _rules = updatedRules;
    }

    private static TimeOnly ParseTimeOrFallback(string? value, TimeOnly fallback)
    {
        if (!string.IsNullOrWhiteSpace(value) && TimeOnly.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static StrategyGroupEditorState CreateGroupStateFromModel(StrategyConditionGroup group)
    {
        var state = new StrategyGroupEditorState { Id = group.Id, Operator = group.Operator };

        foreach (var condition in group.Conditions)
        {
            state.Conditions.Add(CreateConditionStateFromModel(condition));
        }

        foreach (var child in group.Groups)
        {
            state.Groups.Add(CreateGroupStateFromModel(child));
        }

        return state;
    }

    private static StrategyConditionEditorState CreateConditionStateFromModel(
        StrategyCondition condition
    )
    {
        var state = new StrategyConditionEditorState
        {
            Id = condition.Id,
            TypeIndex = (int)condition.Type,
        };

        if (condition.Type == StrategyConditionType.DayType)
        {
            state.DayTypeIndex = condition.DayType switch
            {
                DayType.Weekday => 1,
                DayType.Weekend => 2,
                _ => 0,
            };
        }
        else if (condition.Type == StrategyConditionType.TimeRange)
        {
            state.StartText = condition.Start.ToString("HH:mm");
            state.EndText = condition.End.ToString("HH:mm");
        }

        return state;
    }

    private static StrategyConditionGroup BuildConditionGroup(StrategyGroupEditorState state)
    {
        var operatorValue = GetOperatorFromCombo(state.OperatorComboBox, state.Operator);
        state.Operator = operatorValue;

        var conditions = new List<StrategyCondition>();
        foreach (var conditionState in state.Conditions)
        {
            conditions.Add(BuildCondition(conditionState));
        }

        var groups = new List<StrategyConditionGroup>();
        foreach (var child in state.Groups)
        {
            groups.Add(BuildConditionGroup(child));
        }

        return new StrategyConditionGroup
        {
            Id = state.Id,
            Operator = operatorValue,
            Conditions = conditions,
            Groups = groups,
        };
    }

    private static StrategyCondition BuildCondition(StrategyConditionEditorState state)
    {
        var typeIndex = state.TypeComboBox?.SelectedIndex ?? state.TypeIndex;
        var conditionType = GetConditionType(typeIndex);
        state.TypeIndex = (int)conditionType;

        return conditionType switch
        {
            StrategyConditionType.DayType => BuildDayTypeCondition(state),
            StrategyConditionType.TimeRange => BuildTimeRangeCondition(state),
            StrategyConditionType.KeyboardMouseIdle => new StrategyCondition
            {
                Id = state.Id,
                Type = StrategyConditionType.KeyboardMouseIdle,
            },
            StrategyConditionType.MonitorOff => new StrategyCondition
            {
                Id = state.Id,
                Type = StrategyConditionType.MonitorOff,
            },
            _ => new StrategyCondition
            {
                Id = state.Id,
                Type = StrategyConditionType.DayType,
                DayType = DayType.All,
            },
        };
    }

    private static StrategyConditionType GetConditionType(int index)
    {
        return index switch
        {
            1 => StrategyConditionType.TimeRange,
            2 => StrategyConditionType.KeyboardMouseIdle,
            3 => StrategyConditionType.MonitorOff,
            _ => StrategyConditionType.DayType,
        };
    }

    private static StrategyConditionGroupOperator GetOperatorFromCombo(
        ComboBox? comboBox,
        StrategyConditionGroupOperator fallback
    )
    {
        var index = comboBox?.SelectedIndex ?? (int)fallback;
        return index switch
        {
            1 => StrategyConditionGroupOperator.Any,
            2 => StrategyConditionGroupOperator.None,
            _ => StrategyConditionGroupOperator.All,
        };
    }

    private static DayType GetDayTypeFromCombo(ComboBox? comboBox, int fallbackIndex)
    {
        var index = comboBox?.SelectedIndex ?? fallbackIndex;
        return index switch
        {
            1 => DayType.Weekday,
            2 => DayType.Weekend,
            _ => DayType.All,
        };
    }

    private static StrategyCondition BuildDayTypeCondition(StrategyConditionEditorState state)
    {
        var dayTypeIndex = state.DayTypeComboBox?.SelectedIndex ?? state.DayTypeIndex;
        state.DayTypeIndex = dayTypeIndex;
        return new StrategyCondition
        {
            Id = state.Id,
            Type = StrategyConditionType.DayType,
            DayType = GetDayTypeFromCombo(state.DayTypeComboBox, dayTypeIndex),
        };
    }

    private static StrategyCondition BuildTimeRangeCondition(StrategyConditionEditorState state)
    {
        var startText = state.StartTextBox?.Text ?? state.StartText;
        var endText = state.EndTextBox?.Text ?? state.EndText;
        state.StartText = startText;
        state.EndText = endText;
        return new StrategyCondition
        {
            Id = state.Id,
            Type = StrategyConditionType.TimeRange,
            Start = ParseTimeOrFallback(startText, new TimeOnly(9, 0)),
            End = ParseTimeOrFallback(endText, new TimeOnly(17, 0)),
        };
    }

    private static bool RemoveGroup(StrategyGroupEditorState group, Guid groupId)
    {
        var removed = group.Groups.RemoveAll(child => child.Id == groupId) > 0;
        if (removed)
        {
            return true;
        }

        foreach (var child in group.Groups)
        {
            if (RemoveGroup(child, groupId))
            {
                return true;
            }
        }

        return false;
    }

    private Element BuildGroupPanel(
        RuleEditorControls editor,
        StrategyGroupEditorState group,
        int depth,
        bool isRoot
    )
    {
        var operatorComboBox = StyleCompactInput(
                new ComboBox()
                    .Items(new[] { "All", "Any", "None" })
                    .SelectedIndex((int)group.Operator)
            )
            .Width(92);
        group.OperatorComboBox = operatorComboBox;

        var addGroupButton = CreateCompactPrimaryButton(
                "+ Group",
                () =>
                {
                    SyncRulesFromEditors();
                    group.Groups.Add(new StrategyGroupEditorState());
                    RebuildScheduleRulesPanel();
                }
            )
            .Width(56);

        var addConditionButton = CreateCompactPrimaryButton(
                "+ Cond",
                () =>
                {
                    SyncRulesFromEditors();
                    group.Conditions.Add(new StrategyConditionEditorState { TypeIndex = 0 });
                    RebuildScheduleRulesPanel();
                }
            )
            .Width(56);

        var headerLeft = new StackPanel()
            .Horizontal()
            .Spacing(6)
            .Children(
                new Label()
                    .Text(isRoot ? "Root Group" : "Group")
                    .FontFamily("Bahnschrift")
                    .SemiBold()
                    .Foreground(TextPrimary),
                new Label()
                    .Text($"ID {group.Id.ToString()[..8]}")
                    .FontFamily("Consolas")
                    .FontSize(10)
                    .Foreground(TextMuted)
            );

        var headerActions = new StackPanel()
            .Horizontal()
            .Spacing(6)
            .Children(operatorComboBox, addConditionButton, addGroupButton);

        if (!isRoot)
        {
            headerActions.Children(
                CreateCompactDangerButton(
                        "Del",
                        () =>
                        {
                            SyncRulesFromEditors();
                            RemoveGroup(editor.RootGroupState, group.Id);
                            RebuildScheduleRulesPanel();
                        }
                    )
                    .Width(42)
            );
        }

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
                headerActions
            );

        var rows = new List<Element> { headerRow };

        foreach (var condition in group.Conditions)
        {
            rows.Add(BuildConditionRow(editor, group, condition, depth + 1));
        }

        foreach (var child in group.Groups)
        {
            rows.Add(BuildGroupPanel(editor, child, depth + 1, isRoot: false));
        }

        if (group.Conditions.Count == 0 && group.Groups.Count == 0)
        {
            rows.Add(
                new Label()
                    .Text("No conditions yet. Add a leaf or nested group.")
                    .FontFamily("Consolas")
                    .FontSize(10)
                    .Foreground(TextMuted)
            );
        }

        var indent = depth * 12;
        var groupPanel = new Border()
            .Background(depth == 0 ? SurfaceCard : SurfacePanel)
            .BorderBrush(depth == 0 ? BorderColor : DividerColor)
            .BorderThickness(1)
            .CornerRadius(depth == 0 ? 8 : 6)
            .Padding(depth == 0 ? 10 : 8)
            .Child(new StackPanel().Vertical().Spacing(8).Children(rows.ToArray()));

        if (depth > 0)
        {
            groupPanel.Margin(indent, 0, 0, 0);
        }

        return groupPanel;
    }

    private Element BuildConditionRow(
        RuleEditorControls editor,
        StrategyGroupEditorState group,
        StrategyConditionEditorState condition,
        int depth
    )
    {
        var typeComboBox = StyleCompactInput(
                new ComboBox()
                    .Items(new[] { "DayType", "TimeRange", "KeyboardMouseIdle", "MonitorOff" })
                    .SelectedIndex(condition.TypeIndex)
            )
            .Width(136);
        condition.TypeComboBox = typeComboBox;

        var inputs = BuildConditionInputs(condition);

        var applyTypeButton = CreateCompactPrimaryButton(
                "Apply",
                () =>
                {
                    SyncRulesFromEditors();
                    RebuildScheduleRulesPanel();
                }
            )
            .Width(52);

        var removeButton = CreateCompactDangerButton(
                "Del",
                () =>
                {
                    SyncRulesFromEditors();
                    group.Conditions.RemoveAll(item => item.Id == condition.Id);
                    RebuildScheduleRulesPanel();
                }
            )
            .Width(42);

        var row = new StackPanel()
            .Horizontal()
            .Spacing(6)
            .Children(typeComboBox, inputs, applyTypeButton, removeButton);

        var rowCard = new Border()
            .Background(SurfaceInput)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .CornerRadius(6)
            .Padding(6)
            .Child(row);

        if (depth > 0)
        {
            rowCard.Margin(depth * 12, 0, 0, 0);
        }

        return rowCard;
    }

    private Element BuildConditionInputs(StrategyConditionEditorState condition)
    {
        var conditionType = GetConditionType(
            condition.TypeComboBox?.SelectedIndex ?? condition.TypeIndex
        );
        condition.TypeIndex = (int)conditionType;

        switch (conditionType)
        {
            case StrategyConditionType.DayType:
            {
                var dayTypeComboBox = StyleCompactInput(
                        new ComboBox()
                            .Items(new[] { "All", "Weekday", "Weekend" })
                            .SelectedIndex(condition.DayTypeIndex)
                    )
                    .Width(120);
                condition.DayTypeComboBox = dayTypeComboBox;
                condition.StartTextBox = null;
                condition.EndTextBox = null;
                return dayTypeComboBox;
            }
            case StrategyConditionType.TimeRange:
            {
                var startTextBox = StyleCompactInput(
                    new TextBox().Text(condition.StartText).Width(84).Placeholder("Start")
                );
                var endTextBox = StyleCompactInput(
                    new TextBox().Text(condition.EndText).Width(84).Placeholder("End")
                );
                condition.StartTextBox = startTextBox;
                condition.EndTextBox = endTextBox;
                condition.DayTypeComboBox = null;
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
            case StrategyConditionType.KeyboardMouseIdle:
                condition.DayTypeComboBox = null;
                condition.StartTextBox = null;
                condition.EndTextBox = null;
                return new Label()
                    .Text("Keyboard/mouse idle")
                    .FontFamily("Consolas")
                    .FontSize(10)
                    .Foreground(TextMuted);
            case StrategyConditionType.MonitorOff:
                condition.DayTypeComboBox = null;
                condition.StartTextBox = null;
                condition.EndTextBox = null;
                return new Label()
                    .Text("Monitor off")
                    .FontFamily("Consolas")
                    .FontSize(10)
                    .Foreground(TextMuted);
            default:
                return new Label()
                    .Text("Unsupported condition")
                    .FontFamily("Consolas")
                    .FontSize(10)
                    .Foreground(TextMuted);
        }
    }

    #endregion

    #region Override Tab

    private Element CreateOverrideTabContent()
    {
        _overrideStatusLabel = new Label().Text(
            _config.Override.IsActive
                ? $"Override Active (Expires: {_config.Override.ExpiresAt:g})"
                : "No Override Active"
        );
        _overrideStatusLabel
            .FontFamily("Consolas")
            .Foreground(_config.Override.IsActive ? AccentColor : TextMuted)
            .FontSize(12);

        var planNames = new List<string>();
        foreach (var plan in _plans)
        {
            planNames.Add(plan.Name);
        }

        _overridePlanComboBox = StyleInput(
            new ComboBox().Items(planNames.ToArray()).SelectedIndex(0)
        );

        _overrideTtlTextBox = StyleInput(
            new TextBox().Text("60").Width(120).Placeholder("Minutes")
        );

        var setOverrideButton = CreatePrimaryButton("Set Override", OnSetOverrideClicked)
            .Width(120);
        var clearOverrideButton = CreateDangerButton("Clear Override", OnClearOverrideClicked)
            .Width(120);

        return new StackPanel()
            .Vertical()
            .Spacing(12)
            .Padding(8)
            .Children(
                CreateSectionCard(
                    "Manual Override",
                    "Force a temporary plan regardless of automatic strategy.",
                    _overrideStatusLabel,
                    CreateDivider(),
                    CreateFieldBlock("Override plan", _overridePlanComboBox),
                    new StackPanel()
                        .Horizontal()
                        .Spacing(8)
                        .Children(
                            CreateFieldLabel("Duration (minutes)").MinWidth(170),
                            _overrideTtlTextBox
                        ),
                    new StackPanel()
                        .Horizontal()
                        .Spacing(8)
                        .Children(setOverrideButton, clearOverrideButton)
                )
            );
    }

    private void OnSetOverrideClicked()
    {
        var planIndex = _overridePlanComboBox?.SelectedIndex ?? 0;
        if (planIndex >= 0 && planIndex < _plans.Count)
        {
            var selectedPlan = _plans[planIndex];
            if (int.TryParse(_overrideTtlTextBox?.Text ?? "60", out var ttlMinutes))
            {
                _config = BuildConfigFromEditors() with
                {
                    Override = new()
                    {
                        IsActive = true,
                        PlanGuid = selectedPlan.Guid,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes),
                    },
                };

                _overrideStatusLabel?.Text(
                    $"Override Active (Expires: {_config.Override.ExpiresAt?.ToLocalTime():g})"
                );
                _overrideStatusLabel?.Foreground(AccentColor);
                OnConfigSaved?.Invoke(_config);
                OnNotificationRequested?.Invoke(
                    "Override Active",
                    $"Forced '{selectedPlan.Name}' for {ttlMinutes} minutes"
                );
                RefreshPreviewTab();
            }
            else
            {
                OnNotificationRequested?.Invoke(
                    "Failed to Set Override",
                    "Invalid duration entered."
                );
            }
        }
        else
        {
            OnNotificationRequested?.Invoke("Failed to Set Override", "No plan selected.");
        }
    }

    private void OnClearOverrideClicked()
    {
        _config = BuildConfigFromEditors() with { Override = new() };

        _overrideStatusLabel?.Text("No Override Active");
        _overrideStatusLabel?.Foreground(TextMuted);
        OnConfigSaved?.Invoke(_config);
        OnNotificationRequested?.Invoke("Override Cleared", "Resumed automatic plan management.");
        RefreshPreviewTab();
    }

    #endregion

    #region Preview Tab

    private Element CreatePreviewTabContent()
    {
        _previewKeyboardMouseIdleCheckBox = new CheckBox()
            .Text("Assume keyboard/mouse is idle")
            .IsChecked(false)
            .Foreground(TextPrimary)
            .FontFamily("Consolas");
        _previewMonitorOffCheckBox = new CheckBox()
            .Text("Assume monitor is off")
            .IsChecked(false)
            .Foreground(TextPrimary)
            .FontFamily("Consolas");

        _previewScrollViewer = new ScrollViewer()
            .Height(380)
            .VerticalScroll(ScrollMode.Auto)
            .HorizontalScroll(ScrollMode.Disabled)
            .Background(SurfaceInput)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .Padding(10)
            .Content(BuildTimelinePanel());

        return new StackPanel()
            .Vertical()
            .Spacing(12)
            .Padding(8)
            .Children(
                CreateSectionCard(
                    "Timeline Preview",
                    "Preview uses the selected detector snapshot below. Detector-based rules are runtime-dependent and not future-guaranteed.",
                    new StackPanel()
                        .Horizontal()
                        .Spacing(8)
                        .Children(_previewKeyboardMouseIdleCheckBox, _previewMonitorOffCheckBox),
                    _previewScrollViewer
                )
            );
    }

    private Element BuildTimelinePanel()
    {
        var draftConfig = BuildConfigFromEditors();
        var timeline = PreviewEngine.GenerateTimeline(
            draftConfig,
            _plans,
            DateTime.Now,
            hours: 24,
            snapshot: BuildPreviewSnapshot(draftConfig)
        );

        var rows = new List<Element>();

        if (timeline.Count == 0)
        {
            rows.Add(
                new Border()
                    .Background(SurfaceCard)
                    .BorderBrush(BorderColor)
                    .BorderThickness(1)
                    .CornerRadius(8)
                    .Padding(14)
                    .Child(
                        new Label()
                            .Text("No upcoming plan transitions in the next 24 hours.")
                            .FontFamily("Consolas")
                            .Foreground(TextMuted)
                    )
            );
        }
        else
        {
            for (var i = 0; i < timeline.Count; i++)
            {
                var entry = timeline[i];
                var isFirst = i == 0;

                // Determine duration until next transition
                string durationText;
                if (i + 1 < timeline.Count)
                {
                    var span = timeline[i + 1].Time - entry.Time;
                    durationText = FormatDuration(span);
                }
                else
                {
                    durationText = "until end of preview";
                }

                var timeText = entry.Time.ToString("ddd HH:mm");

                var dot = new Border()
                    .Width(10)
                    .Height(10)
                    .CornerRadius(5)
                    .Background(isFirst ? AccentColor : TextMuted);

                var line =
                    i < timeline.Count - 1
                        ? (Element)
                            new Border()
                                .Width(2)
                                .Height(24)
                                .Background(BorderColor)
                                .Margin(4, 0, 0, 0)
                        : new Border().Width(0).Height(0);

                var timeLabel = new Label()
                    .Text(timeText)
                    .FontFamily("Consolas")
                    .FontSize(12)
                    .Foreground(isFirst ? AccentColor : TextPrimary)
                    .MinWidth(100);

                var planLabel = new Label()
                    .Text(entry.PlanName)
                    .FontFamily("Bahnschrift")
                    .FontSize(13)
                    .SemiBold()
                    .Foreground(TextPrimary);

                var sourceLabel = new Label()
                    .Text(entry.Source)
                    .FontFamily("Consolas")
                    .FontSize(10)
                    .Foreground(TextMuted);

                var durationLabel = new Label()
                    .Text(durationText)
                    .FontFamily("Consolas")
                    .FontSize(10)
                    .Foreground(TextMuted);

                var infoBlock = new StackPanel()
                    .Vertical()
                    .Spacing(2)
                    .Children(planLabel, sourceLabel, durationLabel);

                var entryRow = new StackPanel()
                    .Horizontal()
                    .Spacing(12)
                    .Children(timeLabel, dot, infoBlock);

                var entryWithConnector = new StackPanel()
                    .Vertical()
                    .Spacing(0)
                    .Children(
                        new Border()
                            .Background(SurfaceCard)
                            .BorderBrush(BorderColor)
                            .BorderThickness(1)
                            .CornerRadius(8)
                            .Padding(10)
                            .Child(entryRow),
                        new StackPanel().Horizontal().Children(new Border().Width(100), line)
                    );

                rows.Add(entryWithConnector);
            }
        }

        return new StackPanel().Vertical().Spacing(0).Children(rows.ToArray());
    }

    private void RefreshPreviewTab()
    {
        if (_previewScrollViewer is null)
            return;

        _previewScrollViewer.Content = BuildTimelinePanel();
    }

    private static string FormatDuration(TimeSpan span)
    {
        if (span.TotalMinutes < 1)
            return "< 1 min";
        if (span.TotalHours < 1)
            return $"{(int)span.TotalMinutes} min";
        if (span.TotalHours < 24)
        {
            var h = (int)span.TotalHours;
            var m = span.Minutes;
            return m > 0 ? $"{h}h {m}m" : $"{h}h";
        }
        var d = (int)span.TotalDays;
        var hr = span.Hours;
        return hr > 0 ? $"{d}d {hr}h" : $"{d}d";
    }

    #endregion

    #region About Tab

    private Element CreateAboutTabContent()
    {
        var version = "1.0.0";

        return new Border()
            .Background(SurfaceCard)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .CornerRadius(12)
            .Padding(24)
            .Child(
                new StackPanel()
                    .Vertical()
                    .Spacing(12)
                    .Center()
                    .Children(
                        new Label()
                            .Text("AutoPower")
                            .FontSize(30)
                            .Bold()
                            .FontFamily("Bahnschrift")
                            .Foreground(TextPrimary),
                        new Border()
                            .Background(SurfaceInput)
                            .BorderBrush(BorderColor)
                            .BorderThickness(1)
                            .CornerRadius(18)
                            .Padding(12, 5)
                            .Child(
                                new Label()
                                    .Text($"Version {version}")
                                    .FontFamily("Consolas")
                                    .Foreground(AccentColor)
                            ),
                        CreateDivider(),
                        new Label()
                            .Text("Automatic power plan management for Windows")
                            .FontSize(13)
                            .Foreground(TextPrimary)
                            .FontFamily("Bahnschrift"),
                        new Label()
                            .Text(
                                "Smartly switches plans based on idle detection and schedule rules."
                            )
                            .FontFamily("Consolas")
                            .FontSize(11)
                            .Foreground(TextMuted),
                        new Label()
                            .Text("Built with Aprillz.MewUI")
                            .FontFamily("Consolas")
                            .FontSize(11)
                            .Foreground(TextMuted)
                    )
            );
    }

    #endregion

    #region Helpers

    private Border CreateSectionCard(string title, string subtitle, params Element[] content)
    {
        var items = new List<Element>
        {
            new Label()
                .Text(title)
                .FontSize(15)
                .SemiBold()
                .FontFamily("Bahnschrift")
                .Foreground(TextPrimary),
            new Label().Text(subtitle).FontSize(11).FontFamily("Consolas").Foreground(TextMuted),
            CreateDivider(),
        };

        foreach (var element in content)
        {
            items.Add(element);
        }

        return new Border()
            .Background(SurfaceCard)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .CornerRadius(10)
            .Padding(12)
            .Child(new StackPanel().Vertical().Spacing(8).Children(items.ToArray()));
    }

    private static Border CreateDivider()
    {
        return new Border().Height(1).Background(DividerColor);
    }

    private static StackPanel CreateFieldBlock(string label, Element control)
    {
        return new StackPanel().Vertical().Spacing(4).Children(CreateFieldLabel(label), control);
    }

    private static StackPanel CreateInlineField(string label, Element control, int labelWidth)
    {
        return new StackPanel()
            .Horizontal()
            .Spacing(6)
            .Children(CreateFieldLabel(label).MinWidth(labelWidth), control);
    }

    private static Label CreateFieldLabel(string text)
    {
        return new Label()
            .Text(text)
            .FontSize(11)
            .SemiBold()
            .FontFamily("Consolas")
            .Foreground(TextMuted);
    }

    private static ScrollViewer WrapInScrollViewer(Element content)
    {
        return new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)
            .HorizontalScroll(ScrollMode.Disabled)
            .Content(content);
    }

    private static T StyleInput<T>(T control)
        where T : Control
    {
        return control
            .Height(30)
            .Padding(8, 4)
            .Background(SurfaceInput)
            .Foreground(TextPrimary)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .FontFamily("Consolas");
    }

    private static T StyleCompactInput<T>(T control)
        where T : Control
    {
        return control
            .Height(26)
            .Padding(6, 3)
            .Background(SurfaceInput)
            .Foreground(TextPrimary)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .FontFamily("Consolas");
    }

    private static Button CreatePrimaryButton(string text, Action onClick)
    {
        return new Button()
            .Content(text)
            .OnClick(onClick)
            .Height(32)
            .Padding(12, 6)
            .Background(AccentColor)
            .Foreground(Color.White)
            .BorderBrush(AccentColor)
            .BorderThickness(1)
            .FontFamily("Bahnschrift")
            .SemiBold();
    }

    private static Button CreateCompactPrimaryButton(string text, Action onClick)
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

    private static Button CreateDangerButton(string text, Action onClick)
    {
        return new Button()
            .Content(text)
            .OnClick(onClick)
            .Height(32)
            .Padding(12, 6)
            .Background(SurfaceInput)
            .Foreground(DangerColor)
            .BorderBrush(DangerColor)
            .BorderThickness(1)
            .FontFamily("Bahnschrift")
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

    private int GetPlanIndex(List<Guid> planGuids, Guid targetGuid)
    {
        for (var i = 0; i < planGuids.Count; i++)
        {
            if (planGuids[i] == targetGuid)
                return i;
        }
        return 0;
    }

    private int GetOptionalPlanIndex(List<Guid> planGuids, Guid? targetGuid)
    {
        if (!targetGuid.HasValue || targetGuid.Value == Guid.Empty)
        {
            return 0;
        }

        return GetPlanIndex(planGuids, targetGuid.Value) + 1;
    }

    private Guid? GetOptionalPlanGuid(ComboBox? comboBox)
    {
        var index = comboBox?.SelectedIndex ?? 0;
        if (index <= 0)
        {
            return null;
        }

        var planIndex = index - 1;
        return planIndex >= 0 && planIndex < _plans.Count ? _plans[planIndex].Guid : null;
    }

    private AppConfig BuildConfigFromEditors()
    {
        SyncRulesFromEditors();

        var mode = DetectionMode.Both;
        if (_modeKeyboardMouse?.IsChecked == true)
            mode = DetectionMode.KeyboardMouse;
        else if (_modeMonitorSleep?.IsChecked == true)
            mode = DetectionMode.MonitorSleep;
        else if (_modeBoth?.IsChecked == true)
            mode = DetectionMode.Both;

        var idleTimeout = 5;
        if (int.TryParse(_idleTimeoutTextBox?.Text ?? "5", out var parsedTimeout))
        {
            idleTimeout = Math.Max(1, parsedTimeout);
        }

        var activeIndex = _activePlanComboBox?.SelectedIndex ?? 0;
        var idleIndex = _idlePlanComboBox?.SelectedIndex ?? 0;
        var activePlanGuid = Guid.Empty;
        var idlePlanGuid = Guid.Empty;

        if (activeIndex >= 0 && activeIndex < _plans.Count)
            activePlanGuid = _plans[activeIndex].Guid;

        if (idleIndex >= 0 && idleIndex < _plans.Count)
            idlePlanGuid = _plans[idleIndex].Guid;

        return _config with
        {
            Mode = mode,
            IdleTimeoutMinutes = idleTimeout,
            ActivePlanGuid = activePlanGuid,
            IdlePlanGuid = idlePlanGuid,
            DefaultPlanGuid = GetOptionalPlanGuid(_defaultPlanComboBox),
            Rules = new(_rules),
            AutoStartEnabled = _autoStartCheckBox?.IsChecked ?? false,
        };
    }

    private StrategyEvaluationContext BuildPreviewSnapshot(AppConfig config)
    {
        return new()
        {
            Now = DateTime.Now,
            IsKeyboardMouseDetectionEnabled =
                config.Mode is DetectionMode.KeyboardMouse or DetectionMode.Both,
            IsMonitorDetectionEnabled =
                config.Mode is DetectionMode.MonitorSleep or DetectionMode.Both,
            IsKeyboardMouseIdle = _previewKeyboardMouseIdleCheckBox?.IsChecked,
            IsMonitorOff = _previewMonitorOffCheckBox?.IsChecked,
        };
    }

    private void OnSaveClicked()
    {
        var updatedConfig = BuildConfigFromEditors();
        OnConfigSaved?.Invoke(updatedConfig);
        _window?.Close();
    }

    private sealed class RuleEditorControls
    {
        internal RuleEditorControls(
            StrategyRule sourceRule,
            CheckBox enabledCheckBox,
            TextBox nameTextBox,
            TextBox priorityTextBox,
            ComboBox planComboBox,
            StrategyGroupEditorState rootGroupState
        )
        {
            SourceRule = sourceRule;
            EnabledCheckBox = enabledCheckBox;
            NameTextBox = nameTextBox;
            PriorityTextBox = priorityTextBox;
            PlanComboBox = planComboBox;
            RootGroupState = rootGroupState;
        }

        internal StrategyRule SourceRule { get; }
        internal CheckBox EnabledCheckBox { get; }
        internal TextBox NameTextBox { get; }
        internal TextBox PriorityTextBox { get; }
        internal ComboBox PlanComboBox { get; }
        internal StrategyGroupEditorState RootGroupState { get; }
    }

    private sealed class StrategyGroupEditorState
    {
        internal Guid Id { get; init; } = Guid.NewGuid();
        internal StrategyConditionGroupOperator Operator { get; set; } =
            StrategyConditionGroupOperator.All;
        internal ComboBox? OperatorComboBox { get; set; }
        internal List<StrategyConditionEditorState> Conditions { get; } = new();
        internal List<StrategyGroupEditorState> Groups { get; } = new();
    }

    private sealed class StrategyConditionEditorState
    {
        internal Guid Id { get; init; } = Guid.NewGuid();
        internal int TypeIndex { get; set; }
        internal ComboBox? TypeComboBox { get; set; }
        internal int DayTypeIndex { get; set; }
        internal ComboBox? DayTypeComboBox { get; set; }
        internal TextBox? StartTextBox { get; set; }
        internal TextBox? EndTextBox { get; set; }
        internal string StartText { get; set; } = new TimeOnly(9, 0).ToString("HH:mm");
        internal string EndText { get; set; } = new TimeOnly(17, 0).ToString("HH:mm");
    }

    #endregion
}
