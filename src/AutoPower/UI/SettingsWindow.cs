using System.Linq;
using System.Globalization;
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.Diagnostics;
using AutoPower.Core.Core.Models;
using AutoPower.Core.Localization;
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
    private ComboBox? _languageComboBox;
    private ComboBox? _themeComboBox;
    private Slider? _scaleSlider;
    private Label? _scaleLabel;

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
            .Title(Strings.SettingsTitle)
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
            new TabItem().Header(Strings.TabsGeneral).Content(CreateGeneralTabContent()),
            new TabItem().Header(Strings.TabsInterface).Content(CreateInterfaceTabContent()),
            new TabItem().Header(Strings.TabsSchedule).Content(CreateScheduleTabContent()),
            new TabItem().Header(Strings.TabsOverride).Content(CreateOverrideTabContent()),
            new TabItem().Header(Strings.TabsPreview).Content(CreatePreviewTabContent()),
            new TabItem().Header(Strings.TabsAbout).Content(CreateAboutTabContent())
        );

        tabControl
            .Background(SurfacePanel)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .Foreground(TextPrimary);

        var saveButton = CreatePrimaryButton(Strings.SettingsSave, OnSaveClicked).Width(128);

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
                                    .Text(Strings.SettingsTitle)
                                    .FontSize(21)
                                    .Bold()
                                    .FontFamily("Microsoft YaHei")
                                    .Foreground(TextPrimary),
                                new Label()
                                    .Text(
                                        Strings.SettingsSubtitle
                                    )
                                    .FontSize(11)
                                    .FontFamily("Microsoft YaHei")
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
                    Text = Strings.SettingsLoading,
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
            .Content(Strings.GeneralModeKeyboardMouse)
            .GroupName("DetectionMode")
            .IsChecked(_config.Mode == DetectionMode.KeyboardMouse)
            .Foreground(TextPrimary)
            .FontFamily("Microsoft YaHei");

        _modeMonitorSleep = new RadioButton()
            .Content(Strings.GeneralModeMonitorSleep)
            .GroupName("DetectionMode")
            .IsChecked(_config.Mode == DetectionMode.MonitorSleep)
            .Foreground(TextPrimary)
            .FontFamily("Microsoft YaHei");

        _modeBoth = new RadioButton()
            .Content(Strings.GeneralModeBoth)
            .GroupName("DetectionMode")
            .IsChecked(_config.Mode == DetectionMode.Both)
            .Foreground(TextPrimary)
            .FontFamily("Microsoft YaHei");

        _idleTimeoutTextBox = StyleInput(
            new TextBox()
                .Text(_config.IdleTimeoutMinutes.ToString())
                .Width(120)
                .Placeholder(Strings.GeneralIdleTimeoutPlaceholder)
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
                .Items(new[] { Strings.GeneralNoneOption }.Concat(planNames).ToArray())
                .SelectedIndex(GetOptionalPlanIndex(planGuids, _config.DefaultPlanGuid))
                .MinWidth(280)
        );

        _autoStartCheckBox = new CheckBox()
            .Content(Strings.GeneralAutoStart)
            .IsChecked(_config.AutoStartEnabled)
            .Foreground(TextPrimary)
            .FontFamily("Microsoft YaHei");
        return new StackPanel()
            .Vertical()
            .Spacing(12)
            .Padding(8)
            .Children(
                CreateSectionCard(
                    Strings.GeneralDetectionStrategy,
                    Strings.GeneralDetectionStrategyDesc,
                    _modeKeyboardMouse,
                    _modeMonitorSleep,
                    _modeBoth
                ),
                CreateSectionCard(
                    Strings.GeneralPowerTransition,
                    Strings.GeneralPowerTransitionDesc,
                    new StackPanel()
                        .Horizontal()
                        .Spacing(8)
                        .Children(
                            CreateFieldLabel(Strings.GeneralIdleTimeoutLabel).MinWidth(170),
                            _idleTimeoutTextBox
                        ),
                    CreateDivider(),
                    CreateFieldBlock(Strings.GeneralDefaultPlan, _defaultPlanComboBox),
                    CreateFieldBlock(Strings.GeneralActiveFallbackPlan, _activePlanComboBox),
                    CreateFieldBlock(Strings.GeneralIdleFallbackPlan, _idlePlanComboBox)
                ),
                CreateSectionCard(
                    Strings.GeneralStartup,
                    Strings.GeneralStartupDesc,
                    _autoStartCheckBox
                )
            );
    }

    #endregion

    #region Interface Tab

    private static readonly string[] LanguageCodes = { "", "en", "zh-Hans", "zh-Hant" };
    private static readonly string[] ThemeCodes = { "", "Light", "Dark" };

    private Element CreateInterfaceTabContent()
    {
        // Language
        var langIndex = Array.IndexOf(LanguageCodes, _config.Language ?? "");
        _languageComboBox = StyleInput(
            new ComboBox()
                .Items(new[]
                {
                    Strings.LanguageSystemDefault,
                    Strings.LanguageEnglish,
                    Strings.LanguageChineseSimplified,
                    Strings.LanguageChineseTraditional,
                })
                .SelectedIndex(langIndex >= 0 ? langIndex : 0)
                .MinWidth(200)
        );

        // Theme
        var themeIndex = Array.IndexOf(ThemeCodes, _config.Theme ?? "");
        _themeComboBox = StyleInput(
            new ComboBox()
                .Items(new[]
                {
                    Strings.InterfaceThemeSystem,
                    Strings.InterfaceThemeLight,
                    Strings.InterfaceThemeDark,
                })
                .SelectedIndex(themeIndex >= 0 ? themeIndex : 0)
                .MinWidth(200)
        );

        // Scale
        var scale = _config.ScalePercent ?? 100;
        _scaleLabel = new Label()
            .Text(Strings.InterfaceScalePercent(scale))
            .Foreground(TextPrimary)
            .FontFamily("Microsoft YaHei")
            .MinWidth(48);

        _scaleSlider = new Slider()
            .Minimum(75)
            .Maximum(200)
            .SmallChange(5)
            .Value(scale)
            .OnValueChanged(v =>
            {
                _scaleLabel?.Text(Strings.InterfaceScalePercent((int)v));
            });

        return new StackPanel()
            .Vertical()
            .Spacing(12)
            .Padding(8)
            .Children(
                CreateSectionCard(
                    Strings.LanguageLabel,
                    Strings.LanguageDesc,
                    _languageComboBox
                ),
                CreateSectionCard(
                    Strings.InterfaceThemeLabel,
                    Strings.InterfaceThemeDesc,
                    _themeComboBox
                ),
                CreateSectionCard(
                    Strings.InterfaceScaleLabel,
                    Strings.InterfaceScaleDesc,
                    new StackPanel()
                        .Horizontal()
                        .Spacing(12)
                        .Children(_scaleSlider, _scaleLabel)
                )
            );
    }

    #endregion


    #region Schedule Tab

    private Element CreateScheduleTabContent()
    {
        _decisionTreeEditor = new DecisionTreeEditor();
        _decisionTreeEditor.SetAvailablePlans(_plans);
        _decisionTreeEditor.LoadTree(_config.DecisionTree);
        _decisionTreeEditor.TreeChanged += () =>
        {
            RefreshPreviewTab();
            if (_rulesSummaryLabel != null)
            {
                var count = CountNodes(_decisionTreeEditor.ViewModel.Root.Value);
                _rulesSummaryLabel.Text(count == 1 ? Strings.ScheduleNodeConfiguredSingular : Strings.ScheduleNodeConfigured(count));
            }
        };

        _rulesSummaryLabel = new Label()
            .Text(FormatDecisionTreeSummary())
            .FontFamily("Microsoft YaHei")
            .FontSize(11)
            .Foreground(TextMuted);

        return new StackPanel()
            .Vertical()
            .Spacing(12)
            .Padding(8)
            .Children(
                CreateSectionCard(
                    Strings.ScheduleDecisionTreeStrategy,
                    Strings.ScheduleDecisionTreeDesc,
                    _rulesSummaryLabel,
                    _decisionTreeEditor
                )
            );
    }

    private string FormatDecisionTreeSummary()
    {
        var nodeCount = CountNodes(_config.DecisionTree);
        return nodeCount == 1 ? Strings.ScheduleNodeConfiguredSingular : Strings.ScheduleNodeConfigured(nodeCount);
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
                ? Strings.OverrideOverrideActive(_config.Override.ExpiresAt?.ToLocalTime().ToString("g") ?? "")
                : Strings.OverrideNoOverrideActive
        );
        _overrideStatusLabel
            .FontFamily("Microsoft YaHei")
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
            new TextBox().Text("60").Width(120).Placeholder(Strings.OverrideDurationPlaceholder)
        );

        var setOverrideButton = CreatePrimaryButton(Strings.OverrideSetOverride, OnSetOverrideClicked)
            .Width(120);
        var clearOverrideButton = CreateDangerButton(Strings.OverrideClearOverride, OnClearOverrideClicked)
            .Width(120);

        return new StackPanel()
            .Vertical()
            .Spacing(12)
            .Padding(8)
            .Children(
                CreateSectionCard(
                    Strings.OverrideManualOverride,
                    Strings.OverrideManualOverrideDesc,
                    _overrideStatusLabel,
                    CreateDivider(),
                    CreateFieldBlock(Strings.OverrideOverridePlan, _overridePlanComboBox),
                    new StackPanel()
                        .Horizontal()
                        .Spacing(8)
                        .Children(
                            CreateFieldLabel(Strings.OverrideDurationLabel).MinWidth(170),
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
                    Strings.OverrideOverrideActive(_config.Override.ExpiresAt?.ToLocalTime().ToString("g") ?? "")
                );
                _overrideStatusLabel?.Foreground(AccentColor);
                OnConfigSaved?.Invoke(_config);
                OnNotificationRequested?.Invoke(
                    Strings.OverrideNotificationOverrideActive,
                    Strings.OverrideNotificationForced(selectedPlan.Name, ttlMinutes)
                );
                RefreshPreviewTab();
            }
            else
            {
                OnNotificationRequested?.Invoke(
                    Strings.OverrideNotificationFailed,
                    Strings.OverrideNotificationInvalidDuration
                );
            }
        }
        else
        {
            OnNotificationRequested?.Invoke(Strings.OverrideNotificationFailed, Strings.OverrideNotificationNoPlan);
        }
    }

    private void OnClearOverrideClicked()
    {
        _config = BuildConfigFromEditors() with { Override = new() };

        _overrideStatusLabel?.Text(Strings.OverrideNoOverrideActive);
        _overrideStatusLabel?.Foreground(TextMuted);
        OnConfigSaved?.Invoke(_config);
        OnNotificationRequested?.Invoke(Strings.OverrideNotificationCleared, Strings.OverrideNotificationResumed);
        RefreshPreviewTab();
    }

    #endregion

    #region Preview Tab

    private Element CreatePreviewTabContent()
    {
        _previewKeyboardMouseIdleCheckBox = new CheckBox()
            .Content(Strings.PreviewAssumeKeyboardMouseIdle)
            .IsChecked(false)
            .Foreground(TextPrimary)
            .FontFamily("Microsoft YaHei");
        _previewMonitorOffCheckBox = new CheckBox()
            .Content(Strings.PreviewAssumeMonitorOff)
            .IsChecked(false)
            .Foreground(TextPrimary)
            .FontFamily("Microsoft YaHei");

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
                    Strings.PreviewTimelinePreview,
                    Strings.PreviewTimelinePreviewDesc,
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
                            .Text(Strings.PreviewNoTransitions)
                            .FontFamily("Microsoft YaHei")
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
                    durationText = Strings.PreviewUntilEndOfPreview;
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
                    .FontFamily("Microsoft YaHei")
                    .FontSize(12)
                    .Foreground(isFirst ? AccentColor : TextPrimary)
                    .MinWidth(100);

                var planLabel = new Label()
                    .Text(entry.PlanName)
                    .FontFamily("Microsoft YaHei")
                    .FontSize(13)
                    .SemiBold()
                    .Foreground(TextPrimary);

                var sourceLabel = new Label()
                    .Text(entry.Source)
                    .FontFamily("Microsoft YaHei")
                    .FontSize(10)
                    .Foreground(TextMuted);

                var durationLabel = new Label()
                    .Text(durationText)
                    .FontFamily("Microsoft YaHei")
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
            return Strings.DurationLessThanOneMin;
        if (span.TotalHours < 1)
            return Strings.DurationMinutes((int)span.TotalMinutes);
        if (span.TotalHours < 24)
        {
            var h = (int)span.TotalHours;
            var m = span.Minutes;
            return m > 0 ? Strings.DurationHoursMinutes(h, m) : Strings.DurationHours(h);
        }
        var d = (int)span.TotalDays;
        var hr = span.Hours;
        return hr > 0 ? Strings.DurationDaysHours(d, hr) : Strings.DurationDays(d);
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
                            .Text(Strings.AppTitle)
                            .FontSize(30)
                            .Bold()
                            .FontFamily("Microsoft YaHei")
                            .Foreground(TextPrimary),
                        new Border()
                            .Background(SurfaceInput)
                            .BorderBrush(BorderColor)
                            .BorderThickness(1)
                            .CornerRadius(18)
                            .Padding(12, 5)
                            .Child(
                                new Label()
                                    .Text(Strings.AboutVersion(version))
                                    .FontFamily("Microsoft YaHei")
                                    .Foreground(AccentColor)
                            ),
                        CreateDivider(),
                        new Label()
                            .Text(Strings.AboutDescription)
                            .FontSize(13)
                            .Foreground(TextPrimary)
                            .FontFamily("Microsoft YaHei"),
                        new Label()
                            .Text(
                                Strings.AboutTagline
                            )
                            .FontFamily("Microsoft YaHei")
                            .FontSize(11)
                            .Foreground(TextMuted),
                        new Label()
                            .Text(Strings.AboutBuiltWith)
                            .FontFamily("Microsoft YaHei")
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
                .FontFamily("Microsoft YaHei")
                .Foreground(TextPrimary),
            new Label().Text(subtitle).FontSize(11).FontFamily("Microsoft YaHei").Foreground(TextMuted),
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
            .FontFamily("Microsoft YaHei")
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
            .FontFamily("Microsoft YaHei");
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
            .FontFamily("Microsoft YaHei");
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
            .FontFamily("Microsoft YaHei")
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
            .FontFamily("Microsoft YaHei")
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
            .FontFamily("Microsoft YaHei")
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
            .FontFamily("Microsoft YaHei")
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

        var langIdx = _languageComboBox?.SelectedIndex ?? 0;
        var lang = langIdx >= 0 && langIdx < LanguageCodes.Length ? LanguageCodes[langIdx] : "";

        var themeIdx = _themeComboBox?.SelectedIndex ?? 0;
        var theme = themeIdx >= 0 && themeIdx < ThemeCodes.Length ? ThemeCodes[themeIdx] : "";

        var scale = (int)(_scaleSlider?.Value ?? 100);

        return _config with
        {
            Language = string.IsNullOrEmpty(lang) ? null : lang,
            Theme = string.IsNullOrEmpty(theme) ? null : theme,
            ScalePercent = scale == 100 ? null : scale,
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
        var oldScale = _config.ScalePercent ?? 100;
        var newScale = updatedConfig.ScalePercent ?? 100;

        OnConfigSaved?.Invoke(updatedConfig);
        _window?.Close();

        if (oldScale != newScale)
        {
            OnNotificationRequested?.Invoke(
                Strings.InterfaceScaleLabel,
                Strings.InterfaceScaleRestartNotice
            );
        }
    }



    #endregion
}
