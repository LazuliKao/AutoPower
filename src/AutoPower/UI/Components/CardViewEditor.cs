#nullable enable

using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using AutoPower.Core.Core.Models;
using AutoPower.UI.ViewModels;

namespace AutoPower.UI.Components;

/// <summary>
/// UserControl for editing a decision tree using collapsible cards.
/// Displays nested IF-THEN-ELSE structures with visual hierarchy.
/// Maximum visual depth is 5 levels; beyond level 3, suggests collapsing.
/// </summary>
public sealed class CardViewEditor : UserControl
{
    private readonly DecisionTreeViewModel _vm;
    private List<PowerPlanInfo> _availablePlans = new();

    // Colors matching SettingsWindow theme
    private static readonly Color SurfaceCard = Color.FromHex("#1C2333");
    private static readonly Color SurfaceInput = Color.FromHex("#121723");
    private static readonly Color BorderColor = Color.FromHex("#2E374A");
    private static readonly Color TextPrimary = Color.FromHex("#EAF0FF");
    private static readonly Color TextMuted = Color.FromHex("#9AA7BF");
    private static readonly Color AccentColor = Color.FromHex("#FF4F9A");
    private static readonly Color ThenColor = Color.FromHex("#4CAF50"); // Green for THEN branch
    private static readonly Color ElseColor = Color.FromHex("#FF9800"); // Orange for ELSE branch
    private static readonly Color DangerColor = Color.FromHex("#D85A76");
    private static readonly Color DisabledBackground = Color.FromRgb(0x14, 0x1A, 0x28);

    // Theme-based metrics (avoiding hardcoded values)
    private const double CardSpacing = 8.0;
    private const double IndentPerLevel = 16.0;
    private const double MaxVisualDepth = 5;
    private const double SuggestCollapseDepth = 3;

    /// <summary>
    /// Event raised when the tree content changes.
    /// </summary>
    public event Action? TreeChanged;

    /// <summary>
    /// Event raised when a node is selected.
    /// </summary>
    public event Action<StrategyDecisionNode?>? NodeSelected;

    /// <summary>
    /// Event raised when breadcrumb path changes.
    /// </summary>
    public event Action<string>? BreadcrumbChanged;

    /// <summary>
    /// Gets the ViewModel for binding access.
    /// </summary>
    public DecisionTreeViewModel ViewModel => _vm;

    // Track expansion state for each node
    private readonly Dictionary<Guid, bool> _expandedStates = new();

    // Cache condition editors by node ID to preserve state across rebuilds
    private readonly Dictionary<Guid, ConditionGroupEditor> _conditionEditors = new();

    public CardViewEditor(DecisionTreeViewModel vm)
    {
        _vm = vm;
        _vm.Root.Subscribe(() =>
        {
            CleanupConditionEditors(_vm.Root.Value);
            Rebuild();
        });
        Build();
    }

    /// <summary>
    /// Loads a decision tree from a root node.
    /// </summary>
    public void LoadTree(StrategyDecisionNode? root)
    {
        // Rebuild is handled automatically by the Root subscription
    }

    /// <summary>
    /// Clears the decision tree.
    /// </summary>
    public void ClearTree()
    {
        _vm.ClearTree();
        _expandedStates.Clear();
        _conditionEditors.Clear();
        Rebuild();
    }

    public void SetAvailablePlans(List<PowerPlanInfo> plans)
    {
        _availablePlans = plans;
        Rebuild();
    }

    /// <summary>
    /// Cleans up cached condition editors for nodes no longer in the tree.
    /// </summary>
    private void CleanupConditionEditors(StrategyDecisionNode? root)
    {
        if (root == null)
        {
            _conditionEditors.Clear();
            return;
        }

        var validIfNodeIds = new HashSet<Guid>();
        CollectIfNodeIds(root, validIfNodeIds);

        foreach (var key in _conditionEditors.Keys.Where(k => !validIfNodeIds.Contains(k)).ToList())
        {
            _conditionEditors.Remove(key);
        }
    }

    private static void CollectIfNodeIds(StrategyDecisionNode node, HashSet<Guid> ids)
    {
        if (node.If != null)
        {
            ids.Add(node.Id);
        }
        if (node.Then != null) CollectIfNodeIds(node.Then, ids);
        if (node.Else != null) CollectIfNodeIds(node.Else, ids);
    }

    protected override Element? OnBuild()
    {
        var root = _vm.Root.Value;

        // Empty state when no tree
        if (root == null)
        {
            return BuildEmptyState();
        }

        // Build breadcrumb path
        var breadcrumb = BuildBreadcrumb(root);

        // Build the card tree
        var cardContent = BuildCard(root, 0, new List<string>());

        // Main container with breadcrumb and scrollable card area
        return new Border()
            .Background(SurfaceCard)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .CornerRadius(8)
            .Child(
                new DockPanel().Children(
                    // Breadcrumb bar at top
                    new Border()
                        .DockTop()
                        .Background(SurfaceInput)
                        .BorderBrush(BorderColor)
                        .BorderThickness(1)
                        .Padding(12, 8)
                        .Child(breadcrumb),
                    // Scrollable card area
                    new ScrollViewer()
                        .VerticalScroll(ScrollMode.Auto)
                        .HorizontalScroll(ScrollMode.Auto)
                        .Padding(12)
                        .Content(cardContent)
                )
            );
    }

    /// <summary>
    /// Builds the breadcrumb navigation bar.
    /// </summary>
    private UIElement BuildBreadcrumb(StrategyDecisionNode root)
    {
        var pathSegments = GetBreadcrumbPath(root, _vm.SelectedNode.Value);

        var panel = new StackPanel()
            .Horizontal()
            .Spacing(4)
            .Children(
                new Label().Text("Path: ").FontFamily("Consolas").FontSize(10).Foreground(TextMuted)
            );

        for (int i = 0; i < pathSegments.Count; i++)
        {
            var isLast = i == pathSegments.Count - 1;

            panel.Children(
                new Label()
                    .Text(pathSegments[i])
                    .FontFamily("Consolas")
                    .FontSize(10)
                    .Foreground(isLast ? AccentColor : TextPrimary)
            );

            if (!isLast)
            {
                panel.Children(
                    new Label()
                        .Text(" > ")
                        .FontFamily("Consolas")
                        .FontSize(10)
                        .Foreground(TextMuted)
                );
            }
        }

        return panel;
    }

    /// <summary>
    /// Gets the breadcrumb path to a selected node.
    /// </summary>
    private static List<string> GetBreadcrumbPath(
        StrategyDecisionNode root,
        StrategyDecisionNode? selected
    )
    {
        var path = new List<string> { "Root" };

        if (selected == null || selected.Id == root.Id)
        {
            return path;
        }

        FindPath(root, selected, path);
        return path;
    }

    /// <summary>
    /// Recursively finds the path to a node.
    /// </summary>
    private static bool FindPath(
        StrategyDecisionNode current,
        StrategyDecisionNode target,
        List<string> path
    )
    {
        if (current.Then != null)
        {
            path.Add("THEN");
            if (current.Then.Id == target.Id || FindPath(current.Then, target, path))
            {
                return true;
            }
            path.RemoveAt(path.Count - 1);
        }

        if (current.Else != null)
        {
            path.Add("ELSE");
            if (current.Else.Id == target.Id || FindPath(current.Else, target, path))
            {
                return true;
            }
            path.RemoveAt(path.Count - 1);
        }

        return false;
    }

    /// <summary>
    /// Builds a card for a decision node at the given depth.
    /// </summary>
    private Element BuildCard(StrategyDecisionNode node, int depth, List<string> pathLabels)
    {
        // Check if we've exceeded max visual depth
        if (depth >= MaxVisualDepth)
        {
            return BuildDepthLimitIndicator(depth);
        }

        // Determine if this node should be expanded
        var isExpanded = _expandedStates.GetValueOrDefault(node.Id, depth < SuggestCollapseDepth);

        // Build card header
        var header = BuildCardHeader(node, depth, isExpanded);

        // Build card content (conditionally based on expansion)
        var content = isExpanded ? BuildCardContent(node, depth, pathLabels) : null;

        // Create collapsible card container
        var card = new Border()
            .Background(node.IsEnabled ? (depth == 0 ? SurfaceCard : SurfaceInput) : DisabledBackground)
            .BorderBrush(GetDepthBorderColor(depth))
            .BorderThickness(1)
            .CornerRadius(8)
            .Margin(depth > 0 ? new Thickness(0, CardSpacing, 0, 0) : new Thickness(0))
            .Child(
                new StackPanel()
                    .Vertical()
                    .Spacing(0)
                    .Children(BuildCardChildren(header, content, node, depth))
            );

        if (depth == 0)
        {
            return card;
        }

        return new Border()
            .Padding(IndentPerLevel, 0, 0, 0)
            .Child(card);
    }

    /// <summary>
    /// Builds the card header with toggle indicator.
    /// </summary>
    private Element BuildCardHeader(StrategyDecisionNode node, int depth, bool isExpanded)
    {
        var toggleIcon = isExpanded ? "▼" : "▶";
        var nodeType = GetNodeTypeLabel(node);
        var conditionSummary = node.If != null ? GetConditionSummary(node.If) : "(no condition)";

        var headerLeft = new StackPanel()
            .Horizontal()
            .Spacing(8)
            .Children(
                // Expand/collapse toggle
                new Label()
                    .Text(toggleIcon)
                    .FontFamily("Consolas")
                    .FontSize(10)
                    .Foreground(TextMuted)
                    .VerticalAlignment(VerticalAlignment.Center),
                // Enable/disable toggle
                new CheckBox()
                    .IsChecked(node.IsEnabled)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .OnCheckedChanged(isChecked =>
                    {
                        var root = _vm.Root.Value;
                        if (root == null) return;
                        var updated = DecisionTreeMutation.SetIsEnabled(root, node.Id, isChecked, out var changed);
                        if (!changed) return;
                        _vm.UpdateTree(updated);
                        TreeChanged?.Invoke();
                    }),
                // Node type indicator
                new Border()
                    .Background(GetNodeTypeColor(node))
                    .CornerRadius(4)
                    .Padding(6, 2)
                    .Child(
                        new Label()
                            .Text(nodeType)
                            .FontFamily("Bahnschrift")
                            .FontSize(10)
                            .SemiBold()
                            .Foreground(Color.White)
                    ),
                // Condition summary (for IF nodes)
                node.If != null
                    ? new Label()
                        .Text($"IF {conditionSummary}")
                        .FontFamily("Consolas")
                        .FontSize(11)
                        .Foreground(node.IsEnabled ? TextPrimary : TextMuted)
                        .VerticalAlignment(VerticalAlignment.Center)
                    : new Label().Text("")
            );

        // Action buttons row
        var actionsRow = new StackPanel()
            .Horizontal()
            .Spacing(4)
            .Children(
                CreateCompactButton(toggleIcon, TextMuted, () =>
                {
                    ToggleExpansion(node);
                    SelectNode(node);
                }),
                CreateCompactButton("SEL", AccentColor, () => SelectNode(node)),
                CreateCompactButton("+ THEN", ThenColor, () => AddThenBranch(node)),
                CreateCompactButton("+ ELSE", ElseColor, () => AddElseBranch(node)),
                CreateCompactDangerButton("DEL", () => DeleteNode(node))
            );

        return new Border()
            .Background(SurfaceInput)
            .Padding(10, 8)
            .Child(new DockPanel().Children(actionsRow.DockRight(), headerLeft));
    }

    /// <summary>
    /// Builds the card content (THEN/ELSE branches and condition editor).
    /// </summary>
    private Element BuildCardContent(StrategyDecisionNode node, int depth, List<string> pathLabels)
    {
        var children = new List<Element>();
        var isLeaf = node.Then == null && node.Else == null;

        // Condition editor if this is an IF node
        if (node.If != null)
        {
            if (!_conditionEditors.TryGetValue(node.Id, out var conditionEditor))
            {
                conditionEditor = new ConditionGroupEditor();
                conditionEditor.GroupChanged += () =>
                {
                    var root = _vm.Root.Value;
                    if (root == null) return;
                    var updatedGroup = conditionEditor.GetGroup();
                    var updated = DecisionTreeMutation.SetIf(root, node.Id, updatedGroup, out var changed);
                    if (changed)
                    {
                        _vm.UpdateTree(updated);
                        TreeChanged?.Invoke();
                    }
                };
                _conditionEditors[node.Id] = conditionEditor;
            }
            conditionEditor.LoadGroup(node.If, isRoot: true);

            children.Add(new Border().Margin(8, 8, 8, 0).Child(conditionEditor));
        }

        // THEN branch
        if (node.Then != null)
        {
            children.Add(BuildBranchCard(node.Then, "THEN", ThenColor, depth + 1, pathLabels));
        }

        // ELSE branch
        if (node.Else != null)
        {
            children.Add(BuildBranchCard(node.Else, "ELSE", ElseColor, depth + 1, pathLabels));
        }

        if (isLeaf)
        {
            var details = new StackPanel().Vertical().Spacing(8);
            var planRow = new StackPanel().Horizontal().Spacing(8);
            var planLabel = new Label()
                .Text("Power Plan")
                .FontSize(11)
                .Foreground(TextMuted);
            var planComboBox = new ComboBox();
            var planNames = _availablePlans.Select(p => p.Name).ToArray();
            planComboBox.Items(planNames);
            var selectedIndex = _availablePlans.FindIndex(p => p.Guid == node.PlanGuid);
            if (selectedIndex >= 0)
            {
                planComboBox.SelectedIndex(selectedIndex);
            }

            planComboBox.SelectionChanged += _ =>
            {
                var idx = planComboBox.SelectedIndex;
                if (idx >= 0 && idx < _availablePlans.Count)
                {
                    bool changed;
                    var updatedRoot = DecisionTreeMutation.SetPlanGuid(
                        _vm.Root.Value!, node.Id, _availablePlans[idx].Guid, out changed);
                    if (changed)
                    {
                        _vm.UpdateTree(updatedRoot);
                        TreeChanged?.Invoke();
                    }
                }
            };

            planRow.Children(planLabel, planComboBox);
            details.Children(planRow);

            children.Add(
                new Border()
                    .Margin(8)
                    .Padding(12, 8)
                    .Background(SurfaceInput)
                    .BorderBrush(BorderColor)
                    .BorderThickness(1)
                    .CornerRadius(6)
                    .Child(details)
            );
        }

        return new StackPanel().Vertical().Spacing(0).Children(children.ToArray());
    }

    /// <summary>
    /// Builds a branch card with label.
    /// </summary>
    private Element BuildBranchCard(
        StrategyDecisionNode node,
        string branchLabel,
        Color branchColor,
        int depth,
        List<string> pathLabels
    )
    {
        var newPathLabels = new List<string>(pathLabels) { branchLabel };

        return new StackPanel()
            .Vertical()
            .Spacing(0)
            .Children(
                // Branch label
                new Border()
                    .Margin(8, 8, 8, 0)
                    .Background(branchColor)
                    .CornerRadius(4)
                    .Padding(8, 4)
                    .Child(
                        new Label()
                            .Text(branchLabel)
                            .FontFamily("Bahnschrift")
                            .FontSize(11)
                            .SemiBold()
                            .Foreground(Color.White)
                    ),
                // Nested card
                BuildCard(node, depth, newPathLabels)
            );
    }

    /// <summary>
    /// Builds children array for the card stack panel.
    /// </summary>
    private Element[] BuildCardChildren(
        Element header,
        Element? content,
        StrategyDecisionNode node,
        int depth
    )
    {
        var children = new List<Element> { header };

        if (content != null)
        {
            children.Add(content);
        }

        return children.ToArray();
    }

    /// <summary>
    /// Builds an indicator when depth limit is reached.
    /// </summary>
    private Element BuildDepthLimitIndicator(int depth)
    {
        return new Border()
            .Background(SurfaceInput)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .CornerRadius(6)
            .Margin(0, CardSpacing, 0, 0)
            .Padding(12, 8)
            .Child(
                new Label()
                    .Text(
                        $"Maximum depth ({MaxVisualDepth}) reached. Collapse upper levels to navigate."
                    )
                    .FontFamily("Consolas")
                    .FontSize(10)
                    .Foreground(TextMuted)
            );
    }

    /// <summary>
    /// Gets the node type label for display.
    /// </summary>
    private string GetNodeTypeLabel(StrategyDecisionNode node)
    {
        if (node.PlanGuid.HasValue)
        {
            var plan = _availablePlans.FirstOrDefault(p => p.Guid == node.PlanGuid);
            return plan != null ? $"PLAN: {plan.Name}" : $"PLAN: {node.PlanGuid.Value.ToString()[..8]}...";
        }

        if (node.If != null)
        {
            return "IF";
        }

        return "NODE";
    }

    /// <summary>
    /// Gets the color for a node type.
    /// </summary>
    private static Color GetNodeTypeColor(StrategyDecisionNode node)
    {
        if (node.PlanGuid.HasValue)
        {
            return AccentColor;
        }

        if (node.If != null)
        {
            return Color.FromHex("#2196F3"); // Blue for IF
        }

        return TextMuted;
    }

    /// <summary>
    /// Gets a border color based on depth for visual hierarchy.
    /// </summary>
    private static Color GetDepthBorderColor(int depth)
    {
        return depth switch
        {
            0 => AccentColor,
            1 => ThenColor,
            2 => ElseColor,
            _ => BorderColor,
        };
    }

    /// <summary>
    /// Gets a short summary of a condition group.
    /// </summary>
    private static string GetConditionSummary(StrategyConditionGroup group)
    {
        var parts = new List<string>();

        if (group.Conditions.Count > 0)
        {
            parts.Add($"{group.Conditions.Count} cond(s)");
        }

        if (group.Groups.Count > 0)
        {
            parts.Add($"{group.Groups.Count} group(s)");
        }

        var op = group.Operator switch
        {
            StrategyConditionGroupOperator.All => "ALL",
            StrategyConditionGroupOperator.Any => "ANY",
            StrategyConditionGroupOperator.None => "NONE",
            _ => "ALL",
        };

        return parts.Count > 0 ? $"[{op}] {string.Join(" + ", parts)}" : $"[{op}]";
    }

    /// <summary>
    /// Builds the empty state UI.
    /// </summary>
    private Element BuildEmptyState()
    {
        return new Border()
            .Background(SurfaceCard)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .CornerRadius(8)
            .Padding(24)
            .Child(
                new StackPanel()
                    .Vertical()
                    .Spacing(12)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Children(
                        new Label()
                            .Text("No Decision Tree")
                            .FontFamily("Bahnschrift")
                            .FontSize(14)
                            .SemiBold()
                            .Foreground(TextPrimary)
                            .HorizontalAlignment(HorizontalAlignment.Center),
                        new Label()
                            .Text("Create a root node to begin building the card view.")
                            .FontFamily("Consolas")
                            .FontSize(11)
                            .Foreground(TextMuted)
                            .HorizontalAlignment(HorizontalAlignment.Center),
                        new Button()
                            .Content("+ Add Root Node")
                            .OnClick(AddRootNode)
                            .Height(28)
                            .Padding(12, 4)
                            .Background(AccentColor)
                            .Foreground(Color.White)
                            .BorderBrush(AccentColor)
                            .BorderThickness(1)
                            .FontFamily("Bahnschrift")
                            .FontSize(11)
                            .SemiBold()
                            .HorizontalAlignment(HorizontalAlignment.Center)
                    )
            );
    }

    /// <summary>
    /// Toggles the expansion state of a node.
    /// </summary>
    private void ToggleExpansion(StrategyDecisionNode node)
    {
        _expandedStates[node.Id] = !_expandedStates.GetValueOrDefault(node.Id, true);
        Rebuild();
    }

    /// <summary>
    /// Adds a root node to the tree.
    /// </summary>
    private void AddRootNode()
    {
        var rootNode = new StrategyDecisionNode
        {
            If = StrategyConditionGroup.MatchAll(),
            IsEnabled = true,
        };

        _vm.LoadTree(rootNode);
        Rebuild();
        TreeChanged?.Invoke();
    }

    /// <summary>
    /// Adds a THEN branch to a node.
    /// </summary>
    private void AddThenBranch(StrategyDecisionNode parent)
    {
        var root = _vm.Root.Value;
        if (root == null)
        {
            return;
        }

        var updatedRoot = DecisionTreeMutation.AddThenBranch(
            root,
            parent.Id,
            new StrategyDecisionNode
            {
                If = StrategyConditionGroup.MatchAll(),
                IsEnabled = true,
            },
            out var changed);

        if (!changed)
        {
            return;
        }

        _vm.UpdateTree(updatedRoot);
        var selected = DecisionTreeMutation.FindNodeById(updatedRoot, parent.Id)?.Then;
        _vm.SelectNode(selected);
        Rebuild();
        TreeChanged?.Invoke();
    }

    /// <summary>
    /// Adds an ELSE branch to a node.
    /// </summary>
    private void AddElseBranch(StrategyDecisionNode parent)
    {
        var root = _vm.Root.Value;
        if (root == null)
        {
            return;
        }

        var updatedRoot = DecisionTreeMutation.AddElseBranch(
            root,
            parent.Id,
            new StrategyDecisionNode
            {
                If = StrategyConditionGroup.MatchAll(),
                IsEnabled = true,
            },
            out var changed);

        if (!changed)
        {
            return;
        }

        _vm.UpdateTree(updatedRoot);
        var selected = DecisionTreeMutation.FindNodeById(updatedRoot, parent.Id)?.Else;
        _vm.SelectNode(selected);
        Rebuild();
        TreeChanged?.Invoke();
    }

    /// <summary>
    /// Deletes a node from the tree.
    /// </summary>
    private void DeleteNode(StrategyDecisionNode node)
    {
        var root = _vm.Root.Value;
        if (root == null || root.Id == node.Id)
        {
            return;
        }

        var updatedRoot = DecisionTreeMutation.DeleteNode(root, node.Id, out var deleted);
        if (!deleted)
        {
            return;
        }

        _vm.UpdateTree(updatedRoot);
        _vm.SelectNode(null);
        CleanupConditionEditors(updatedRoot);
        Rebuild();
        TreeChanged?.Invoke();
    }

    /// <summary>
    /// Handles node selection.
    /// </summary>
    private void SelectNode(StrategyDecisionNode node)
    {
        _vm.SelectNode(node);
        NodeSelected?.Invoke(node);

        // Update breadcrumb
        if (_vm.Root.Value != null)
        {
            var path = GetBreadcrumbPath(_vm.Root.Value, node);
            BreadcrumbChanged?.Invoke(string.Join(" > ", path));
        }
    }

    private void Rebuild()
    {
        Build();
    }

    private static Button CreateCompactButton(string text, Color accent, Action onClick)
    {
        return new Button()
            .Content(text)
            .OnClick(onClick)
            .Height(22)
            .Padding(6, 2)
            .Background(SurfaceInput)
            .Foreground(accent)
            .BorderBrush(accent)
            .BorderThickness(1)
            .FontFamily("Bahnschrift")
            .FontSize(10)
            .SemiBold();
    }

    private static Button CreateCompactDangerButton(string text, Action onClick)
    {
        return new Button()
            .Content(text)
            .OnClick(onClick)
            .Height(22)
            .Padding(6, 2)
            .Background(SurfaceInput)
            .Foreground(DangerColor)
            .BorderBrush(DangerColor)
            .BorderThickness(1)
            .FontFamily("Bahnschrift")
            .FontSize(10)
            .SemiBold();
    }
}
