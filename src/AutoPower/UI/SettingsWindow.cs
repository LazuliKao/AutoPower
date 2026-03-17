using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using AutoPower.Core.Core.Models;
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
    private CheckBox? _autoStartCheckBox;

    private List<StrategyRule> _rules = new();
    private readonly List<RuleEditorControls> _ruleEditors = new();

    private Label? _rulesSummaryLabel;
    private ScrollViewer? _rulesScrollViewer;

    private Label? _overrideStatusLabel;
    private ComboBox? _overridePlanComboBox;
    private TextBox? _overrideTtlTextBox;

    private ScrollViewer? _previewScrollViewer;

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

        return new Border().Background(WindowBackground).Child(shell);
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
                    "Select how user activity should be detected before switching plans.",
                    _modeKeyboardMouse,
                    _modeMonitorSleep,
                    _modeBoth
                ),
                CreateSectionCard(
                    "Power Transition",
                    "Tune idle threshold and map plans for active and idle states.",
                    new StackPanel()
                        .Horizontal()
                        .Spacing(8)
                        .Children(
                            CreateFieldLabel("Idle timeout (minutes)").MinWidth(170),
                            _idleTimeoutTextBox
                        ),
                    CreateDivider(),
                    CreateFieldBlock("Active plan", _activePlanComboBox),
                    CreateFieldBlock("Idle plan", _idlePlanComboBox)
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

        _rulesScrollViewer = new ScrollViewer()
            .Height(320)
            .VerticalScroll(ScrollMode.Auto)
            .HorizontalScroll(ScrollMode.Disabled)
            .Background(SurfaceInput)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .Padding(10);

        _rulesScrollViewer.Content = BuildRulesPanel();

        return new StackPanel()
            .Vertical()
            .Spacing(12)
            .Padding(8)
            .Children(
                CreateSectionCard(
                    "Strategy Rules",
                    "Edit each rule inline: name, day type, schedule window, and target power plan.",
                    _rulesSummaryLabel,
                    _rulesScrollViewer,
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

            var nameTextBox = StyleInput(new TextBox().Text(rule.Name).Placeholder("Rule name"));
            var dayTypeComboBox = StyleInput(
                    new ComboBox()
                        .Items(new[] { "All", "Weekday", "Weekend" })
                        .SelectedIndex((int)rule.DayType)
                )
                .Width(130);
            var startTextBox = StyleInput(
                new TextBox().Text(rule.Start.ToString("HH:mm")).Width(88)
            );
            var endTextBox = StyleInput(new TextBox().Text(rule.End.ToString("HH:mm")).Width(88));
            var planComboBox = StyleInput(
                new ComboBox()
                    .Items(planNames.ToArray())
                    .SelectedIndex(GetPlanIndex(planGuids, rule.TargetPlanGuid))
                    .MinWidth(260)
            );

            _ruleEditors.Add(
                new RuleEditorControls(
                    rule,
                    enabledCheckBox,
                    nameTextBox,
                    dayTypeComboBox,
                    startTextBox,
                    endTextBox,
                    planComboBox
                )
            );

            var removeButton = CreateDangerButton("Remove", () => OnRemoveRuleClicked(rule.Id))
                .Width(92);

            var metaLabel = new Label()
                .Text($"Priority {rule.Priority}")
                .FontSize(11)
                .FontFamily("Consolas")
                .Foreground(TextMuted);

            var headerLeft = new StackPanel()
                .Horizontal()
                .Spacing(8)
                .Children(
                    new Label()
                        .Text($"Rule {i + 1}")
                        .Bold()
                        .FontFamily("Bahnschrift")
                        .Foreground(TextPrimary),
                    metaLabel
                );

            var headerRight = new StackPanel()
                .Horizontal()
                .Spacing(8)
                .Right()
                .Children(enabledCheckBox, removeButton);

            var headerRow = new StackPanel()
                .Horizontal()
                .Spacing(8)
                .Children(headerLeft, headerRight);

            var rulePanel = new Border()
                .Background(SurfaceCard)
                .BorderBrush(BorderColor)
                .BorderThickness(1)
                .CornerRadius(8)
                .Padding(10)
                .Child(
                    new StackPanel()
                        .Vertical()
                        .Spacing(8)
                        .Children(
                            headerRow,
                            CreateDivider(),
                            CreateFieldBlock("Name", nameTextBox),
                            new StackPanel()
                                .Horizontal()
                                .Spacing(8)
                                .Children(
                                    CreateFieldBlock("Day type", dayTypeComboBox),
                                    CreateFieldBlock("Start", startTextBox),
                                    CreateFieldBlock("End", endTextBox)
                                ),
                            CreateFieldBlock("Plan", planComboBox)
                        )
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
                                "No rules defined yet. Click 'Add Rule' to create your first schedule strategy."
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
            DayType = DayType.All,
            Start = new(9, 0),
            End = new(17, 0),
            TargetPlanGuid = _config.ActivePlanGuid,
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
        if (_rulesScrollViewer is null)
            return;

        _rulesScrollViewer.Content = BuildRulesPanel();
        _rulesSummaryLabel?.Text(FormatRulesSummary());
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

            var dayType = editor.DayTypeComboBox.SelectedIndex switch
            {
                1 => DayType.Weekday,
                2 => DayType.Weekend,
                _ => DayType.All,
            };

            var selectedPlanIndex = editor.PlanComboBox.SelectedIndex;
            var selectedPlanGuid = source.TargetPlanGuid;
            if (selectedPlanIndex >= 0 && selectedPlanIndex < _plans.Count)
            {
                selectedPlanGuid = _plans[selectedPlanIndex].Guid;
            }

            var ruleName = string.IsNullOrWhiteSpace(editor.NameTextBox.Text)
                ? source.Name
                : editor.NameTextBox.Text.Trim();

            updatedRules.Add(
                source with
                {
                    Name = ruleName,
                    DayType = dayType,
                    Start = ParseTimeOrFallback(editor.StartTextBox.Text, source.Start),
                    End = ParseTimeOrFallback(editor.EndTextBox.Text, source.End),
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
                _config = _config with
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
        _config = _config with { Override = new() };

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
        _previewScrollViewer = new ScrollViewer()
            .Height(420)
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
                    "Upcoming power plan transitions for the next 24 hours based on current configuration.",
                    _previewScrollViewer
                )
            );
    }

    private Element BuildTimelinePanel()
    {
        var timeline = PreviewEngine.GenerateTimeline(_config, _plans, DateTime.Now, hours: 24);

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

    private int GetPlanIndex(List<Guid> planGuids, Guid targetGuid)
    {
        for (var i = 0; i < planGuids.Count; i++)
        {
            if (planGuids[i] == targetGuid)
                return i;
        }
        return 0;
    }

    private void OnSaveClicked()
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

        var autoStart = _autoStartCheckBox?.IsChecked ?? false;

        var updatedConfig = _config with
        {
            Mode = mode,
            IdleTimeoutMinutes = idleTimeout,
            ActivePlanGuid = activePlanGuid,
            IdlePlanGuid = idlePlanGuid,
            Rules = new(_rules),
            AutoStartEnabled = autoStart,
        };

        OnConfigSaved?.Invoke(updatedConfig);
        _window?.Close();
    }

    private sealed class RuleEditorControls
    {
        internal RuleEditorControls(
            StrategyRule sourceRule,
            CheckBox enabledCheckBox,
            TextBox nameTextBox,
            ComboBox dayTypeComboBox,
            TextBox startTextBox,
            TextBox endTextBox,
            ComboBox planComboBox
        )
        {
            SourceRule = sourceRule;
            EnabledCheckBox = enabledCheckBox;
            NameTextBox = nameTextBox;
            DayTypeComboBox = dayTypeComboBox;
            StartTextBox = startTextBox;
            EndTextBox = endTextBox;
            PlanComboBox = planComboBox;
        }

        internal StrategyRule SourceRule { get; }
        internal CheckBox EnabledCheckBox { get; }
        internal TextBox NameTextBox { get; }
        internal ComboBox DayTypeComboBox { get; }
        internal TextBox StartTextBox { get; }
        internal TextBox EndTextBox { get; }
        internal ComboBox PlanComboBox { get; }
    }

    #endregion
}
