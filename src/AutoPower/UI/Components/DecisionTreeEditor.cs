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

    // View components - initialized once, visibility toggled
    private TreeViewEditor _treeEditor = null!;
    private CardViewEditor _cardEditor = null!;
    private FlowchartView _flowchartView = null!;

    // Colors matching SettingsWindow theme
    private static readonly Color SurfaceCard = Color.FromHex("#1C2333");
    private static readonly Color SurfaceInput = Color.FromHex("#121723");
    private static readonly Color BorderColor = Color.FromHex("#2E374A");
    private static readonly Color TextPrimary = Color.FromHex("#EAF0FF");
    private static readonly Color TextMuted = Color.FromHex("#9AA7BF");
    private static readonly Color AccentColor = Color.FromHex("#FF4F9A");

    // Observable state for view mode selection
    private readonly ObservableValue<bool> _treeViewSelected = new(true);
    private readonly ObservableValue<bool> _cardViewSelected = new(false);
    private readonly ObservableValue<bool> _flowchartSelected = new(false);
    private readonly ObservableValue<bool> _jsonViewSelected = new(false);
    
    // Guard to prevent recursive subscription calls
    private bool _isUpdatingViewSelection;

    // Observable state for view visibility
    private readonly ObservableValue<bool> _treeViewVisible = new(true);
    private readonly ObservableValue<bool> _cardViewVisible = new(false);
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
        _treeEditor = new TreeViewEditor(_vm);
        _cardEditor = new CardViewEditor(_vm);
        _flowchartView = new FlowchartView();

        // Wire up event forwarding from child editors
        _treeEditor.TreeChanged += () => TreeChanged?.Invoke();
        _treeEditor.NodeSelected += node => NodeSelected?.Invoke(node);
        _cardEditor.TreeChanged += () => TreeChanged?.Invoke();
        _cardEditor.NodeSelected += node => NodeSelected?.Invoke(node);

        _treeEditor.TreeChanged += OnTreeContentChanged;
        _cardEditor.TreeChanged += OnTreeContentChanged;

        // Subscribe to view selection changes
        _treeViewSelected.Subscribe(() => OnViewSelectionChanged(DecisionTreeViewMode.Tree));
        _cardViewSelected.Subscribe(() => OnViewSelectionChanged(DecisionTreeViewMode.Card));
        _flowchartSelected.Subscribe(() => OnViewSelectionChanged(DecisionTreeViewMode.Flowchart));
        _jsonViewSelected.Subscribe(() => OnViewSelectionChanged(DecisionTreeViewMode.Json));

        Build();
    }

    /// <summary>
    /// Creates an editor with an existing ViewModel (for sharing state).
    /// </summary>
    public DecisionTreeEditor(DecisionTreeViewModel vm)
    {
        _vm = vm;

        // Initialize view components with shared ViewModel
        _treeEditor = new TreeViewEditor(_vm);
        _cardEditor = new CardViewEditor(_vm);
        _flowchartView = new FlowchartView();

        // Wire up event forwarding
        _treeEditor.TreeChanged += () => TreeChanged?.Invoke();
        _treeEditor.NodeSelected += node => NodeSelected?.Invoke(node);
        _cardEditor.TreeChanged += () => TreeChanged?.Invoke();
        _cardEditor.NodeSelected += node => NodeSelected?.Invoke(node);

        _treeEditor.TreeChanged += OnTreeContentChanged;
        _cardEditor.TreeChanged += OnTreeContentChanged;

        // Subscribe to view selection changes
        _treeViewSelected.Subscribe(() => OnViewSelectionChanged(DecisionTreeViewMode.Tree));
        _cardViewSelected.Subscribe(() => OnViewSelectionChanged(DecisionTreeViewMode.Card));
        _flowchartSelected.Subscribe(() => OnViewSelectionChanged(DecisionTreeViewMode.Flowchart));
        _jsonViewSelected.Subscribe(() => OnViewSelectionChanged(DecisionTreeViewMode.Json));

        // Sync initial view mode from ViewModel
        SyncViewModeFromViewModel();

        Build();
    }

    /// <summary>
    /// Loads a decision tree from a root node.
    /// </summary>
    public void LoadTree(StrategyDecisionNode? root)
    {
        _vm.LoadTree(root);
        _treeEditor.LoadTree(root);
        _cardEditor.LoadTree(root);
        _flowchartView.SetTree(root);
        UpdateJsonPreview();
        Rebuild();
    }

    /// <summary>
    /// Clears the decision tree.
    /// </summary>
    public void ClearTree()
    {
        _vm.ClearTree();
        _treeEditor.ClearTree();
        _cardEditor.ClearTree();
        _flowchartView.ClearTree();
        UpdateJsonPreview();
        Rebuild();
    }

    protected override Element? OnBuild()
    {
        // View switch bar at top
        var viewSwitchBar = BuildViewSwitchBar();

        // View containers with visibility binding
        var treeContainer = new Border()
            .Child(_treeEditor)
            .BindIsVisible(_treeViewVisible);

        var cardContainer = new Border()
            .Child(_cardEditor)
            .BindIsVisible(_cardViewVisible);

        var flowchartContainer = new Border()
            .Child(_flowchartView)
            .BindIsVisible(_flowchartVisible);

        var jsonContainer = new Border()
            .Background(SurfaceCard)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .CornerRadius(8)
            .Padding(12)
            .Child(BuildJsonPreview())
            .BindIsVisible(_jsonViewVisible);

        return new DockPanel()
            .Children(
                // View switch bar docked at top
                new Border()
                    .DockTop()
                    .Background(SurfaceInput)
                    .BorderBrush(BorderColor)
                    .BorderThickness(1)
                    .Padding(8, 6)
                    .Child(viewSwitchBar),

                // View containers stacked (only one visible at a time)
                new StackPanel()
                    .Vertical()
                    .Spacing(8)
                    .Children(
                        treeContainer,
                        cardContainer,
                        flowchartContainer,
                        jsonContainer
                    )
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

                CreateViewRadioButton("Tree", _treeViewSelected),
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
            .Text(text)
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
        var root = _vm.Root.Value;

        if (root == null)
        {
            return new Label()
                .Text("No tree loaded")
                .FontFamily("Consolas")
                .FontSize(11)
                .Foreground(TextMuted);
        }

        // Generate JSON representation
        var jsonText = GenerateJsonPreview(root);

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
        _treeViewVisible.Value = newMode == DecisionTreeViewMode.Tree;
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
        _treeViewSelected.Value = mode == DecisionTreeViewMode.Tree;
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

        _treeViewSelected.Value = mode == DecisionTreeViewMode.Tree;
        _cardViewSelected.Value = mode == DecisionTreeViewMode.Card;
        _flowchartSelected.Value = mode == DecisionTreeViewMode.Flowchart;
        _jsonViewSelected.Value = mode == DecisionTreeViewMode.Json;

        _treeViewVisible.Value = mode == DecisionTreeViewMode.Tree;
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

    private void Rebuild()
    {
        Build();
    }
}
