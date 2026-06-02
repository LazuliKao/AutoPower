#nullable enable

using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using AutoPower.Core.Core.Models;
using AutoPower.UI.ViewModels;

namespace AutoPower.UI.Components;

/// <summary>
/// Container component for editing decision trees with multiple view modes.
/// Supports Tree, Card, and Flowchart views with state preservation during switches.
/// </summary>
public sealed class DecisionTreeEditor : UserControl
{
    private readonly DecisionTreeViewModel _vm;
    private List<PowerPlanInfo> _availablePlans = new();

    // View components - initialized once, visibility toggled
    private CardViewEditor _cardEditor = null!;
    private FlowchartView _flowchartView = null!;

    // Right-side Details Panel Container
    private readonly Border _detailsPanelContainer = new Border();

    // Details panel in-place updates tracking
    private Guid? _detailsNodeId;
    private bool _detailsNodeIsLeaf;
    private bool _isUpdatingDetails;
    private CheckBox? _detailsEnabledCheckBox;
    private ComboBox? _detailsPlanComboBox;
    private ConditionGroupEditor? _detailsConditionEditor;

    // Colors matching SettingsWindow theme
    private static readonly Color SurfaceCard = Color.FromHex("#1C2333");
    private static readonly Color SurfaceInput = Color.FromHex("#121723");
    private static readonly Color BorderColor = Color.FromHex("#2E374A");
    private static readonly Color TextPrimary = Color.FromHex("#EAF0FF");
    private static readonly Color TextMuted = Color.FromHex("#9AA7BF");
    private static readonly Color AccentColor = Color.FromHex("#FF4F9A");
    private static readonly Color ThenColor = Color.FromHex("#4CAF50");
    private static readonly Color ElseColor = Color.FromHex("#FF9800");
    private static readonly Color DangerColor = Color.FromHex("#D85A76");

    // Observable state for view mode selection
    private readonly ObservableValue<bool> _cardViewSelected = new(true);
    private readonly ObservableValue<bool> _flowchartSelected = new(false);
    private readonly ObservableValue<bool> _jsonViewSelected = new(false);
    
    // Guard to prevent recursive subscription calls
    private bool _isUpdatingViewSelection;

    // Observable state for view visibility
    private readonly ObservableValue<bool> _cardViewVisible = new(true);
    private readonly ObservableValue<bool> _flowchartVisible = new(false);
    private readonly ObservableValue<bool> _jsonViewVisible = new(false);
    private readonly ObservableValue<string> _jsonPreviewText = new("No tree loaded");

    /// <summary>
    /// Event raised when the tree content changes.
    /// </summary>
    public event Action? TreeChanged;

    /// <summary>
    /// Event raised when a node is selected.
    /// </summary>
    public event Action<StrategyDecisionNode?>? NodeSelected;

    /// <summary>
    /// Gets the ViewModel for external binding access.
    /// </summary>
    public DecisionTreeViewModel ViewModel => _vm;

    public DecisionTreeEditor()
    {
        _vm = new DecisionTreeViewModel();

        // Initialize view components with shared ViewModel
        _cardEditor = new CardViewEditor(_vm);
        _flowchartView = new FlowchartView(_vm);

        // Wire up event forwarding from child editors
        _cardEditor.TreeChanged += () => TreeChanged?.Invoke();
        _cardEditor.NodeSelected += node => NodeSelected?.Invoke(node);

        _cardEditor.TreeChanged += OnTreeContentChanged;

        // Subscribe to view selection changes
        _cardViewSelected.Subscribe(() => OnViewSelectionChanged(DecisionTreeViewMode.Card));
        _flowchartSelected.Subscribe(() => OnViewSelectionChanged(DecisionTreeViewMode.Flowchart));
        _jsonViewSelected.Subscribe(() => OnViewSelectionChanged(DecisionTreeViewMode.Json));

        // Subscribe to SelectedNode changes to update details panel
        _vm.SelectedNode.Subscribe(OnSelectedNodeChanged);

        // Subscribe to Root changes to update JSON preview automatically
        _vm.Root.Subscribe(UpdateJsonPreview);

        // Initialize details panel to default state
        OnSelectedNodeChanged();

        Build();
    }

    /// <summary>
    /// Creates an editor with an existing ViewModel (for sharing state).
    /// </summary>
    public DecisionTreeEditor(DecisionTreeViewModel vm)
    {
        _vm = vm;

        // Initialize view components with shared ViewModel
        _cardEditor = new CardViewEditor(_vm);
        _flowchartView = new FlowchartView(_vm);

        // Wire up event forwarding
        _cardEditor.TreeChanged += () => TreeChanged?.Invoke();
        _cardEditor.NodeSelected += node => NodeSelected?.Invoke(node);

        _cardEditor.TreeChanged += OnTreeContentChanged;

        // Subscribe to view selection changes
        _cardViewSelected.Subscribe(() => OnViewSelectionChanged(DecisionTreeViewMode.Card));
        _flowchartSelected.Subscribe(() => OnViewSelectionChanged(DecisionTreeViewMode.Flowchart));
        _jsonViewSelected.Subscribe(() => OnViewSelectionChanged(DecisionTreeViewMode.Json));

        // Subscribe to SelectedNode changes to update details panel
        _vm.SelectedNode.Subscribe(OnSelectedNodeChanged);

        // Subscribe to Root changes to update JSON preview automatically
        _vm.Root.Subscribe(UpdateJsonPreview);

        // Sync initial view mode from ViewModel
        SyncViewModeFromViewModel();

        // Initialize details panel to default state
        OnSelectedNodeChanged();

        Build();
    }

    /// <summary>
    /// Loads a decision tree from a root node.
    /// </summary>
    public void LoadTree(StrategyDecisionNode? root)
    {
        _vm.LoadTree(root);
        // Child editors rebuild themselves via subscription; rebuild container for view switch bar only
        Rebuild();
    }

    /// <summary>
    /// Updates the tree, preserving the selected node's reference by ID.
    /// </summary>
    public void UpdateTree(StrategyDecisionNode? root)
    {
        _vm.UpdateTree(root);
    }

    public void SetAvailablePlans(List<PowerPlanInfo> plans)
    {
        _availablePlans = plans;
        _cardEditor.SetAvailablePlans(plans);
        OnSelectedNodeChanged();
    }

    /// <summary>
    /// Clears the decision tree.
    /// </summary>
    public void ClearTree()
    {
        _vm.ClearTree();
        // Child editors rebuild themselves via subscription; rebuild container for view switch bar only
        Rebuild();
    }

    protected override Element? OnBuild()
    {
        // View switch bar at top
        var viewSwitchBar = BuildViewSwitchBar();

        // View containers with visibility binding
        var cardContainer = new Border()
            .Child(_cardEditor)
            .BindIsVisible(_cardViewVisible);

        var flowchartContainer = new Border()
            .Background(SurfaceCard)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .CornerRadius(8)
            .Child(
                new ScrollViewer()
                    .VerticalScroll(ScrollMode.Auto)
                    .HorizontalScroll(ScrollMode.Auto)
                    .Content(_flowchartView)
            )
            .BindIsVisible(_flowchartVisible);

        var jsonContainer = new Border()
            .Background(SurfaceCard)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .CornerRadius(8)
            .Padding(12)
            .Child(BuildJsonPreview())
            .BindIsVisible(_jsonViewVisible);

        // Grid for left-side views and right-side Details Panel
        var workspaceGrid = new Grid()
            .Columns("*,300")
            .Spacing(12)
            .Children(
                // Left view: Stack of views (only one visible at a time)
                new StackPanel()
                    .Vertical()
                    .Spacing(8)
                    .Children(
                        cardContainer,
                        flowchartContainer,
                        jsonContainer
                    )
                    .Column(0),

                // Right view: Details panel
                _detailsPanelContainer
                    .Column(1)
            );

        return new DockPanel()
            .Children(
                // View switch bar docked at top
                new Border()
                    .DockTop()
                    .Background(SurfaceInput)
                    .BorderBrush(BorderColor)
                    .BorderThickness(1)
                    .Padding(8, 6)
                    .Margin(0, 0, 0, 8)
                    .Child(viewSwitchBar),

                // Workspace Grid occupying the rest of space
                workspaceGrid
            );
    }

    /// <summary>
    /// Builds the view mode switch bar with RadioButtons.
    /// </summary>
    private UIElement BuildViewSwitchBar()
    {
        return new StackPanel()
            .Horizontal()
            .Spacing(12)
            .Children(
                new Label()
                    .Text("View Mode: ")
                    .FontFamily("Consolas")
                    .FontSize(11)
                    .Foreground(TextMuted)
                    .VerticalAlignment(VerticalAlignment.Center),

                CreateViewRadioButton("Card", _cardViewSelected),
                CreateViewRadioButton("Flowchart", _flowchartSelected),
                CreateViewRadioButton("JSON", _jsonViewSelected)
            );
    }

    /// <summary>
    /// Creates a RadioButton for view mode selection.
    /// </summary>
    private static RadioButton CreateViewRadioButton(string text, ObservableValue<bool> isChecked)
    {
        return new RadioButton()
            .Content(text)
            .GroupName("DecisionTreeViewMode")
            .BindIsChecked(isChecked)
            .Foreground(TextPrimary)
            .FontFamily("Consolas")
            .FontSize(11);
    }

    /// <summary>
    /// Builds the JSON preview panel.
    /// </summary>
    private UIElement BuildJsonPreview()
    {
        return new ScrollViewer()
            .VerticalScroll(ScrollMode.Auto)
            .HorizontalScroll(ScrollMode.Auto)
            .MaxHeight(400)
            .Content(
                new TextBlock()
                    .BindText(_jsonPreviewText)
                    .FontFamily("Consolas")
                    .FontSize(11)
                    .Foreground(TextPrimary)
                    .TextWrapping(TextWrapping.Wrap)
            );
    }

    /// <summary>
    /// Generates a JSON preview string for the decision tree.
    /// </summary>
    private static string GenerateJsonPreview(StrategyDecisionNode node, int indent = 0)
    {
        var indentStr = new string(' ', indent * 2);
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"{indentStr}{{");

        if (!node.IsEnabled)
        {
            sb.AppendLine($"{indentStr}  \"enabled\": false,");
        }

        if (node.If != null)
        {
            sb.AppendLine($"{indentStr}  \"if\": {{");
            sb.AppendLine($"{indentStr}    \"operator\": \"{node.If.Operator}\",");
            sb.AppendLine($"{indentStr}    \"conditions\": {node.If.Conditions.Count},");
            sb.AppendLine($"{indentStr}    \"groups\": {node.If.Groups.Count}");
            sb.AppendLine($"{indentStr}  }},");
        }

        if (node.Then != null)
        {
            sb.AppendLine($"{indentStr}  \"then\": ");
            sb.Append(GenerateJsonPreview(node.Then, indent + 1));
            sb.AppendLine(",");
        }

        if (node.Else != null)
        {
            sb.AppendLine($"{indentStr}  \"else\": ");
            sb.Append(GenerateJsonPreview(node.Else, indent + 1));
            sb.AppendLine(",");
        }

        if (node.PlanGuid.HasValue)
        {
            sb.AppendLine($"{indentStr}  \"planGuid\": \"{node.PlanGuid.Value}\"");
        }

        sb.Append($"{indentStr}}}");
        return sb.ToString();
    }

    /// <summary>
    /// Handles view mode selection changes.
    /// </summary>
    private void OnViewSelectionChanged(DecisionTreeViewMode newMode)
    {
        // Prevent recursive calls from subscription
        if (_isUpdatingViewSelection)
        {
            return;
        }

        // Only process if this is a new selection (not just re-setting the same value)
        if (_vm.CurrentView.Value == newMode)
        {
            return;
        }

        // Sync edit state before switching views (preservation)
        SyncEditState();

        // Update ViewModel
        _vm.CurrentView.Value = newMode;

        // Update visibility observables (only one visible)
        _cardViewVisible.Value = newMode == DecisionTreeViewMode.Card;
        _flowchartVisible.Value = newMode == DecisionTreeViewMode.Flowchart;
        _jsonViewVisible.Value = newMode == DecisionTreeViewMode.Json;

        if (newMode == DecisionTreeViewMode.Json)
        {
            UpdateJsonPreview();
        }

        // Ensure only one RadioButton is checked (with guard to prevent recursion)
        _isUpdatingViewSelection = true;
        UpdateRadioButtonStates(newMode);
        _isUpdatingViewSelection = false;

        // Visibility is bound to observables; no full rebuild needed.
    }

    /// <summary>
    /// Syncs edit state before view switch (preservation placeholder).
    /// </summary>
    private void SyncEditState()
    {
        // Edit state is preserved in the shared ViewModel (_vm)
        // and individual editor components maintain their own state
        // No additional sync needed - ObservableValue handles this
    }

    /// <summary>
    /// Updates RadioButton states to ensure mutual exclusivity.
    /// </summary>
    private void UpdateRadioButtonStates(DecisionTreeViewMode mode)
    {
        _cardViewSelected.Value = mode == DecisionTreeViewMode.Card;
        _flowchartSelected.Value = mode == DecisionTreeViewMode.Flowchart;
        _jsonViewSelected.Value = mode == DecisionTreeViewMode.Json;
    }

    /// <summary>
    /// Syncs view mode from ViewModel (for initialization).
    /// </summary>
    private void SyncViewModeFromViewModel()
    {
        var mode = _vm.CurrentView.Value;

        _cardViewSelected.Value = mode == DecisionTreeViewMode.Card;
        _flowchartSelected.Value = mode == DecisionTreeViewMode.Flowchart;
        _jsonViewSelected.Value = mode == DecisionTreeViewMode.Json;

        _cardViewVisible.Value = mode == DecisionTreeViewMode.Card;
        _flowchartVisible.Value = mode == DecisionTreeViewMode.Flowchart;
        _jsonViewVisible.Value = mode == DecisionTreeViewMode.Json;
    }

    private void OnTreeContentChanged()
    {
        UpdateJsonPreview();
    }

    private void UpdateJsonPreview()
    {
        var root = _vm.Root.Value;
        _jsonPreviewText.Value = root == null ? "No tree loaded" : GenerateJsonPreview(root);
    }

    /// <summary>
    /// Rebuilds the Details Panel content when the selected node changes.
    /// </summary>
    private void OnSelectedNodeChanged()
    {
        var node = _vm.SelectedNode.Value;
        if (node == null)
        {
            _detailsNodeId = null;
            _detailsEnabledCheckBox = null;
            _detailsPlanComboBox = null;
            _detailsConditionEditor = null;
            _detailsPanelContainer.Child = BuildDetailsPanelContent(null);
            return;
        }

        var isLeaf = node.Then == null && node.Else == null;
        if (_detailsNodeId == node.Id && _detailsNodeIsLeaf == isLeaf)
        {
            // Update in-place to avoid rebuilding and losing focus
            _isUpdatingDetails = true;
            try
            {
                if (_detailsEnabledCheckBox != null)
                {
                    _detailsEnabledCheckBox.IsChecked(node.IsEnabled);
                }

                if (isLeaf)
                {
                    if (_detailsPlanComboBox != null)
                    {
                        var selectedIndex = _availablePlans.FindIndex(p => p.Guid == node.PlanGuid);
                        if (selectedIndex >= 0 && _detailsPlanComboBox.SelectedIndex != selectedIndex)
                        {
                            _detailsPlanComboBox.SelectedIndex(selectedIndex);
                        }
                    }
                }
                else
                {
                    if (_detailsConditionEditor != null)
                    {
                        _detailsConditionEditor.LoadGroup(node.If, isRoot: true);
                    }
                }
            }
            finally
            {
                _isUpdatingDetails = false;
            }
        }
        else
        {
            _detailsPanelContainer.Child = BuildDetailsPanelContent(node);
        }
    }

    /// <summary>
    /// Constructs the Visual Element for the Node Details panel.
    /// </summary>
    private UIElement BuildDetailsPanelContent(StrategyDecisionNode? node)
    {
        if (node == null)
        {
            _detailsNodeId = null;
            _detailsEnabledCheckBox = null;
            _detailsPlanComboBox = null;
            _detailsConditionEditor = null;

            return new Border()
                .Background(SurfaceCard)
                .BorderBrush(BorderColor)
                .BorderThickness(1)
                .CornerRadius(8)
                .Padding(16)
                .Child(
                    new StackPanel()
                        .Vertical()
                        .Spacing(12)
                        .Center()
                        .Children(
                            new Label()
                                .Text("No Selection")
                                .FontFamily("Bahnschrift")
                                .FontSize(13)
                                .SemiBold()
                                .Foreground(TextMuted)
                                .HorizontalAlignment(HorizontalAlignment.Center),
                            new Label()
                                .Text("Select a node in the tree or flowchart to edit its rules and actions.")
                                .FontFamily("Consolas")
                                .FontSize(10)
                                .Foreground(TextMuted)
                                .TextWrapping(TextWrapping.Wrap)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                        )
                );
        }

        _detailsNodeId = node.Id;
        _detailsNodeIsLeaf = node.Then == null && node.Else == null;

        var isRoot = _vm.Root.Value?.Id == node.Id;
        var isLeaf = _detailsNodeIsLeaf;

        var headerRow = new StackPanel()
            .Horizontal()
            .Spacing(8)
            .Children(
                new Border()
                    .Background(node.PlanGuid.HasValue ? AccentColor : Color.FromHex("#2196F3"))
                    .CornerRadius(4)
                    .Padding(6, 2)
                    .Child(
                        new Label()
                            .Text(node.PlanGuid.HasValue ? "Plan Node" : "IF Branch")
                            .FontFamily("Bahnschrift")
                            .FontSize(10)
                            .SemiBold()
                            .Foreground(Color.White)
                    ),
                new Label()
                    .Text($"ID: {node.Id.ToString()[..8]}")
                    .FontFamily("Consolas")
                    .FontSize(10)
                    .Foreground(TextMuted)
                    .VerticalAlignment(VerticalAlignment.Center)
            );

        _detailsEnabledCheckBox = new CheckBox()
            .Content("Enabled")
            .IsChecked(node.IsEnabled)
            .Foreground(TextPrimary)
            .FontFamily("Consolas")
            .FontSize(11)
            .OnCheckedChanged(isChecked =>
            {
                if (_isUpdatingDetails) return;
                var root = _vm.Root.Value;
                if (root == null) return;
                var updated = DecisionTreeMutation.SetIsEnabled(root, node.Id, isChecked, out var changed);
                if (!changed) return;
                UpdateTree(updated);
                TreeChanged?.Invoke();
            });

        var children = new List<Element> {
            headerRow,
            new Border().Height(1).Background(BorderColor).Margin(0, 4),
            _detailsEnabledCheckBox
        };

        if (!isLeaf)
        {
            // Branch node: show condition group editor
            _detailsConditionEditor = new ConditionGroupEditor();
            _detailsConditionEditor.LoadGroup(node.If, isRoot: true);
            _detailsConditionEditor.GroupChanged += () =>
            {
                if (_isUpdatingDetails) return;
                var root = _vm.Root.Value;
                if (root == null) return;
                var updatedGroup = _detailsConditionEditor.GetGroup();
                var updated = DecisionTreeMutation.SetIf(root, node.Id, updatedGroup, out var changed);
                if (changed)
                {
                    UpdateTree(updated);
                    TreeChanged?.Invoke();
                }
            };

            children.Add(new Label().Text("Conditions:").FontFamily("Bahnschrift").FontSize(11).Foreground(TextMuted).Margin(0, 8, 0, 4));
            children.Add(_detailsConditionEditor);

            // Add branch buttons
            var addThenBtn = new Button()
                .Content("+ Add THEN Branch")
                .Height(26)
                .Background(SurfaceInput)
                .Foreground(ThenColor)
                .BorderBrush(ThenColor)
                .BorderThickness(1)
                .FontFamily("Bahnschrift")
                .FontSize(10)
                .SemiBold()
                .OnCanClick(() => node.Then == null)
                .OnClick(() => {
                    var root = _vm.Root.Value;
                    if (root == null) return;
                    var updated = DecisionTreeMutation.AddThenBranch(root, node.Id, new StrategyDecisionNode { If = StrategyConditionGroup.MatchAll(), IsEnabled = true }, out var changed);
                    if (changed) {
                        var selected = DecisionTreeMutation.FindNodeById(updated, node.Id)?.Then;
                        _vm.SelectNode(selected);
                        UpdateTree(updated);
                        TreeChanged?.Invoke();
                    }
                });

            var addElseBtn = new Button()
                .Content("+ Add ELSE Branch")
                .Height(26)
                .Background(SurfaceInput)
                .Foreground(ElseColor)
                .BorderBrush(ElseColor)
                .BorderThickness(1)
                .FontFamily("Bahnschrift")
                .FontSize(10)
                .SemiBold()
                .OnCanClick(() => node.Then != null && node.Else == null)
                .OnClick(() => {
                    var root = _vm.Root.Value;
                    if (root == null) return;
                    var updated = DecisionTreeMutation.AddElseBranch(root, node.Id, new StrategyDecisionNode { If = StrategyConditionGroup.MatchAll(), IsEnabled = true }, out var changed);
                    if (changed) {
                        var selected = DecisionTreeMutation.FindNodeById(updated, node.Id)?.Else;
                        _vm.SelectNode(selected);
                        UpdateTree(updated);
                        TreeChanged?.Invoke();
                    }
                });

            children.Add(new Border().Height(1).Background(BorderColor).Margin(0, 8));
            children.Add(new Label().Text("Branch Operations:").FontFamily("Bahnschrift").FontSize(11).Foreground(TextMuted).Margin(0, 4));
            children.Add(new StackPanel().Horizontal().Spacing(8).Children(addThenBtn, addElseBtn));
        }
        else
        {
            // Leaf node: show plan picker ComboBox and convert to branch button
            var planLabel = new Label()
                .Text("Target Power Plan:")
                .FontFamily("Bahnschrift")
                .FontSize(11)
                .Foreground(TextMuted)
                .Margin(0, 8, 0, 4);

            _detailsPlanComboBox = new ComboBox()
                .Height(30)
                .Width(260)
                .Padding(8, 4)
                .Background(SurfaceInput)
                .Foreground(TextPrimary)
                .BorderBrush(BorderColor)
                .BorderThickness(1)
                .FontFamily("Consolas");

            var planNames = _availablePlans.Select(p => p.Name).ToArray();
            _detailsPlanComboBox.Items(planNames);
            var selectedIndex = _availablePlans.FindIndex(p => p.Guid == node.PlanGuid);
            if (selectedIndex >= 0)
            {
                _detailsPlanComboBox.SelectedIndex(selectedIndex);
            }

            _detailsPlanComboBox.SelectionChanged += _ =>
            {
                if (_isUpdatingDetails) return;
                var idx = _detailsPlanComboBox.SelectedIndex;
                if (idx >= 0 && idx < _availablePlans.Count)
                {
                    var updated = DecisionTreeMutation.SetPlanGuid(_vm.Root.Value!, node.Id, _availablePlans[idx].Guid, out var changed);
                    if (changed)
                    {
                        UpdateTree(updated);
                        TreeChanged?.Invoke();
                    }
                }
            };

            var convertBtn = new Button()
                .Content("+ Convert to IF-THEN Branch")
                .Height(28)
                .Background(SurfaceInput)
                .Foreground(Color.FromHex("#2196F3"))
                .BorderBrush(Color.FromHex("#2196F3"))
                .BorderThickness(1)
                .FontFamily("Bahnschrift")
                .FontSize(11)
                .SemiBold()
                .OnClick(() => {
                    var root = _vm.Root.Value;
                    if (root == null) return;
                    var updated = DecisionTreeMutation.AddThenBranch(
                        root,
                        node.Id,
                        new StrategyDecisionNode { PlanGuid = node.PlanGuid, IsEnabled = true },
                        out var changed);

                    if (changed) {
                        var selected = DecisionTreeMutation.FindNodeById(updated, node.Id);
                        _vm.SelectNode(selected);
                        UpdateTree(updated);
                        TreeChanged?.Invoke();
                    }
                });

            children.Add(planLabel);
            children.Add(_detailsPlanComboBox);
            children.Add(new Border().Height(1).Background(BorderColor).Margin(0, 12));
            children.Add(convertBtn);
        }

        if (!isRoot)
        {
            var deleteBtn = new Button()
                .Content("Delete Node")
                .Height(30)
                .Background(SurfaceInput)
                .Foreground(DangerColor)
                .BorderBrush(DangerColor)
                .BorderThickness(1)
                .FontFamily("Bahnschrift")
                .FontSize(11)
                .SemiBold()
                .OnClick(() => {
                    var root = _vm.Root.Value;
                    if (root == null) return;
                    var updated = DecisionTreeMutation.DeleteNode(root, node.Id, out var deleted);
                    if (deleted)
                    {
                        _vm.SelectNode(null);
                        UpdateTree(updated);
                        TreeChanged?.Invoke();
                    }
                });

            children.Add(new Border().Height(1).Background(BorderColor).Margin(0, 12));
            children.Add(deleteBtn);
        }

        return new Border()
            .Background(SurfaceCard)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .CornerRadius(8)
            .Padding(12)
            .Child(
                new ScrollViewer()
                    .VerticalScroll(ScrollMode.Auto)
                    .HorizontalScroll(ScrollMode.Disabled)
                    .Content(
                        new StackPanel()
                            .Vertical()
                            .Spacing(8)
                            .Children(children.ToArray())
                    )
            );
    }

    private void Rebuild()
    {
        Build();
    }
}
