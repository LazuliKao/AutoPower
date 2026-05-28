#nullable enable

using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using AutoPower.Core.Core.Models;
using AutoPower.UI.ViewModels;

namespace AutoPower.UI.Components;

/// <summary>
/// UserControl for editing a decision tree using TreeView with DelegateTemplate.
/// Displays nested IF-THEN-ELSE structures and provides node manipulation controls.
/// </summary>
public sealed class TreeViewEditor : UserControl
{
    private readonly DecisionTreeViewModel _vm;

    // Colors matching SettingsWindow theme (using Theme.Palette via properties)
    private static readonly Color SurfaceCard = Color.FromHex("#1C2333");
    private static readonly Color SurfaceInput = Color.FromHex("#121723");
    private static readonly Color BorderColor = Color.FromHex("#2E374A");
    private static readonly Color TextPrimary = Color.FromHex("#EAF0FF");
    private static readonly Color TextMuted = Color.FromHex("#9AA7BF");
    private static readonly Color AccentColor = Color.FromHex("#FF4F9A");
    private static readonly Color ThenColor = Color.FromHex("#4CAF50");  // Green for THEN branch
    private static readonly Color ElseColor = Color.FromHex("#FF9800");  // Orange for ELSE branch
    private static readonly Color DangerColor = Color.FromHex("#D85A76");

    /// <summary>
    /// Event raised when the tree content changes.
    /// </summary>
    public event Action? TreeChanged;

    /// <summary>
    /// Event raised when a node is selected.
    /// </summary>
    public event Action<StrategyDecisionNode?>? NodeSelected;

    /// <summary>
    /// Gets the ViewModel for binding access.
    /// </summary>
    public DecisionTreeViewModel ViewModel => _vm;

    public TreeViewEditor(DecisionTreeViewModel vm)
    {
        _vm = vm;
        Build();
    }

    /// <summary>
    /// Loads a decision tree from a root node.
    /// </summary>
    public void LoadTree(StrategyDecisionNode? root)
    {
        _vm.LoadTree(root);
        Rebuild();
    }

    /// <summary>
    /// Clears the decision tree.
    /// </summary>
    public void ClearTree()
    {
        _vm.ClearTree();
        Rebuild();
    }

    protected override Element? OnBuild()
    {
        var root = _vm.Root.Value;

        // Empty state when no tree
        if (root == null)
        {
            return BuildEmptyState();
        }

        // TreeView with hierarchical data
        var treeView = new TreeView()
            .Items(
                new[] { root },
                childrenSelector: GetChildren,
                textSelector: GenerateNodeLabel,
                keySelector: n => n.Id)
            .ItemTemplate(CreateNodeTemplate());

        treeView.SelectionChanged += selectedItem =>
        {
            if (selectedItem is StrategyDecisionNode selectedNode)
            {
                SelectNode(selectedNode);
                return;
            }

            SelectNode(null);
        };

        // Wrap in scrollable container for deep trees
        return new Border()
            .Background(SurfaceCard)
            .BorderBrush(BorderColor)
            .BorderThickness(1)
            .CornerRadius(8)
            .Padding(12)
            .Child(
                new ScrollViewer()
                    .VerticalScroll(ScrollMode.Auto)
                    .HorizontalScroll(ScrollMode.Auto)
                    .Content(treeView)
            );
    }

    /// <summary>
    /// Gets children of a decision node (Then branch first, then Else branch).
    /// </summary>
    private static IReadOnlyList<StrategyDecisionNode> GetChildren(StrategyDecisionNode node)
    {
        var children = new List<StrategyDecisionNode>();
        if (node.Then is not null)
        {
            children.Add(node.Then);
        }
        if (node.Else is not null)
        {
            children.Add(node.Else);
        }
        return children;
    }

    /// <summary>
    /// Generates a display label for a node.
    /// </summary>
    private static string GenerateNodeLabel(StrategyDecisionNode node)
    {
        if (node.PlanGuid.HasValue)
        {
            // Leaf node with power plan
            return $"→ Plan: {node.PlanGuid.Value.ToString()[..8]}...";
        }

        if (node.If != null)
        {
            // Branching node with condition
            var conditionSummary = GetConditionSummary(node.If);
            return $"IF {conditionSummary}";
        }

        // Unknown/uninitialized node
        return "(empty node)";
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
            _ => "ALL"
        };

        return parts.Count > 0 ? $"[{op}] {string.Join(" + ", parts)}" : $"[{op}]";
    }

    /// <summary>
    /// Creates the template for rendering tree nodes.
    /// </summary>
    private DelegateTemplate<StrategyDecisionNode> CreateNodeTemplate()
    {
        return new DelegateTemplate<StrategyDecisionNode>(
            build: ctx =>
            {
                // Node container with label and action buttons
                var panel = new StackPanel()
                    .Horizontal()
                    .Spacing(8)
                    .Children(
                        // Selection indicator
                        new Border()
                            .Width(3)
                            .Height(16)
                            .CornerRadius(1)
                            .Background(TextMuted)
                            .Register(ctx, "SelectionIndicator"),

                        // Node label
                        new TextBlock()
                            .VerticalAlignment(VerticalAlignment.Center)
                            .FontFamily("Consolas")
                            .FontSize(11)
                            .Foreground(TextPrimary)
                            .Register(ctx, "NodeLabel"),

                        // Branch indicators
                        new TextBlock()
                            .VerticalAlignment(VerticalAlignment.Center)
                            .FontFamily("Consolas")
                            .FontSize(10)
                            .Foreground(ThenColor)
                            .Register(ctx, "ThenIndicator"),

                        new TextBlock()
                            .VerticalAlignment(VerticalAlignment.Center)
                            .FontFamily("Consolas")
                            .FontSize(10)
                            .Foreground(ElseColor)
                            .Register(ctx, "ElseIndicator"),

                        // Action buttons
                        CreateTemplateButton("+ THEN", ThenColor, ctx, "AddThen"),
                        CreateTemplateButton("+ ELSE", ElseColor, ctx, "AddElse"),
                        CreateTemplateDangerButton("DEL", ctx, "Delete")
                    );

                return panel;
            },
            bind: (view, node, index, ctx) =>
            {
                // Bind node label
                ctx.Get<TextBlock>("NodeLabel").Text = GenerateNodeLabel(node);

                // Bind selection indicator color
                var indicator = ctx.Get<Border>("SelectionIndicator");
                indicator.Background = _vm.SelectedNode.Value?.Id == node.Id
                    ? AccentColor
                    : TextMuted;

                // Bind branch indicators
                var thenIndicator = ctx.Get<TextBlock>("ThenIndicator");
                thenIndicator.Text = node.Then != null ? "✓ THEN" : "";
                thenIndicator.Foreground = ThenColor;

                var elseIndicator = ctx.Get<TextBlock>("ElseIndicator");
                elseIndicator.Text = node.Else != null ? "✓ ELSE" : "";
                elseIndicator.Foreground = ElseColor;

                // Wire button clicks
                var addThenBtn = ctx.Get<Button>("AddThen");
                var addElseBtn = ctx.Get<Button>("AddElse");
                var deleteBtn = ctx.Get<Button>("Delete");

                addThenBtn
                    .OnClick(() =>
                    {
                        if (node.Then != null)
                        {
                            return;
                        }

                        AddThenBranch(node);
                    });

                addElseBtn
                    .OnClick(() =>
                    {
                        if (node.Else != null)
                        {
                            return;
                        }

                        AddElseBranch(node);
                    });

                deleteBtn
                    .OnClick(() =>
                    {
                        var rootNode = _vm.Root.Value;
                        if (rootNode == null || rootNode.Id == node.Id)
                        {
                            return;
                        }

                        DeleteNode(node);
                    });
            });
    }

    /// <summary>
    /// Creates a template button with consistent styling.
    /// </summary>
    private static Button CreateTemplateButton(string text, Color accent, TemplateContext ctx, string name)
    {
        return new Button()
            .Content(text)
            .Height(22)
            .Padding(6, 2)
            .Background(SurfaceInput)
            .Foreground(accent)
            .BorderBrush(accent)
            .BorderThickness(1)
            .FontFamily("Bahnschrift")
            .FontSize(10)
            .SemiBold()
            .Register(ctx, name);
    }

    /// <summary>
    /// Creates a danger-styled template button.
    /// </summary>
    private static Button CreateTemplateDangerButton(string text, TemplateContext ctx, string name)
    {
        return new Button()
            .Content(text)
            .Height(22)
            .Padding(6, 2)
            .Background(SurfaceInput)
            .Foreground(DangerColor)
            .BorderBrush(DangerColor)
            .BorderThickness(1)
            .FontFamily("Bahnschrift")
            .FontSize(10)
            .SemiBold()
            .Register(ctx, name);
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
                            .Text("Create a root node to begin building the tree.")
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
    /// Adds a root node to the tree.
    /// </summary>
    private void AddRootNode()
    {
        var rootNode = new StrategyDecisionNode
        {
            If = StrategyConditionGroup.MatchAll(),
            IsEnabled = true
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

        _vm.LoadTree(updatedRoot);
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

        _vm.LoadTree(updatedRoot);
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

        _vm.LoadTree(updatedRoot);
        _vm.SelectNode(null);
        Rebuild();
        TreeChanged?.Invoke();
    }

    /// <summary>
    /// Handles node selection.
    /// </summary>
    private void SelectNode(StrategyDecisionNode? node)
    {
        _vm.SelectNode(node);
        Rebuild();
        NodeSelected?.Invoke(node);
    }

    private void Rebuild()
    {
        Build();
    }
}
