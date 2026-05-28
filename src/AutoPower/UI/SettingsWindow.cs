using System.Linq;
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.Diagnostics;
using AutoPower.Core.Core.Models;
using AutoPower.Core.Infrastructure;
using AutoPower.Core.Strategy;
using AutoPower.UI.Components;

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

    private DecisionTreeEditor? _decisionTreeEditor;

    private Label? _rulesSummaryLabel;

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
            .Content("Keyboard/Mouse")
            .GroupName("DetectionMode")
            .IsChecked(_config.Mode == DetectionMode.KeyboardMouse)
            .Foreground(TextPrimary)
            .FontFamily("Consolas");

        _modeMonitorSleep = new RadioButton()
            .Content("Monitor Sleep")
            .GroupName("DetectionMode")
            .IsChecked(_config.Mode == DetectionMode.MonitorSleep)
            .Foreground(TextPrimary)
            .FontFamily("Consolas");

        _modeBoth = new RadioButton()
            .Content("Both")
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
            .Content("Start AutoPower when Windows starts")
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
        _decisionTreeEditor = new DecisionTreeEditor();
        _decisionTreeEditor.LoadTree(_config.DecisionTree);
        _decisionTreeEditor.TreeChanged += RefreshPreviewTab;

        _rulesSummaryLabel = new Label()
            .Text(FormatDecisionTreeSummary())
            .FontFamily("Consolas")
            .FontSize(11)
            .Foreground(TextMuted);

        return new StackPanel()
            .Vertical()
            .Spacing(12)
            .Padding(8)
            .Children(
                CreateSectionCard(
                    "Decision Tree Strategy",
                    "IF-THEN-ELSE decision tree for power plan selection. Each node evaluates a condition group and branches accordingly.",
                    _rulesSummaryLabel,
                    _decisionTreeEditor
                )
            );
    }

    private string FormatDecisionTreeSummary()
    {
        var nodeCount = CountNodes(_config.DecisionTree);
        return nodeCount == 1 ? "1 node configured" : $"{nodeCount} nodes configured";
    }

    private static int CountNodes(StrategyDecisionNode? node)
    {
        if (node is null)
            return 0;

        var count = 1;
        if (node.Then is not null)
            count += CountNodes(node.Then);
        if (node.Else is not null)
            count += CountNodes(node.Else);

        return count;
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
            .Content("Assume keyboard/mouse is idle")
            .IsChecked(false)
            .Foreground(TextPrimary)
            .FontFamily("Consolas");
        _previewMonitorOffCheckBox = new CheckBox()
            .Content("Assume monitor is off")
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
            DecisionTree = _decisionTreeEditor?.ViewModel.Root.Value,
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



    #endregion
}
