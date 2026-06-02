#nullable enable

using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Aprillz.MewUI.Rendering;
using AutoPower.Core.Core.Models;
using AutoPower.UI.ViewModels;

namespace AutoPower.UI.Components;

/// <summary>
/// Flowchart visualization for decision trees.
/// Renders nodes as rectangles connected by lines with THEN/ELSE labels.
/// Supports clicking nodes to select them.
/// </summary>
public sealed class FlowchartView : Control
{
    // Colors matching existing UI components
    private static readonly Color SurfaceCard = Color.FromHex("#1C2333");
    private static readonly Color BorderColor = Color.FromHex("#2E374A");
    private static readonly Color TextPrimary = Color.FromHex("#EAF0FF");
    private static readonly Color TextMuted = Color.FromHex("#9AA7BF");
    private static readonly Color ThenColor = Color.FromHex("#4CAF50");   // Green for THEN branch
    private static readonly Color ElseColor = Color.FromHex("#FF9800");   // Orange for ELSE branch
    private static readonly Color LeafColor = Color.FromHex("#5C6BC0");   // Indigo for leaf nodes
    private static readonly Color AccentColor = Color.FromHex("#FF4F9A"); // Pink highlight color

    // Layout constants
    private const double NodeWidth = 160.0;
    private const double NodeHeight = 60.0;
    private const double HorizontalSpacing = 40.0;
    private const double VerticalSpacing = 60.0;
    private const double EdgeLabelOffset = 8.0;
    private const double NodeCornerRadius = 6.0;
    private const double LayoutPadding = 20.0;

    // View model for selection tracking
    private readonly DecisionTreeViewModel _vm;

    // Tree data
    private StrategyDecisionNode? _root;
    private readonly Dictionary<Guid, Rect> _nodePositions = new();
    private readonly List<(Point From, Point To, string Label, Color Color)> _edges = new();
    private double _treeWidth;
    private double _treeHeight;

    public FlowchartView(DecisionTreeViewModel vm)
    {
        _vm = vm;
        // Redraw flowchart when root or selected node changes
        _vm.Root.Subscribe(() => SetTree(_vm.Root.Value));
        _vm.SelectedNode.Subscribe(InvalidateVisual);
    }

    /// <summary>
    /// Sets the decision tree root to visualize.
    /// </summary>
    public void SetTree(StrategyDecisionNode? root)
    {
        _root = root;
        ComputeLayout();
        InvalidateMeasure();
        InvalidateVisual();
    }

    /// <summary>
    /// Clears the flowchart.
    /// </summary>
    public void ClearTree()
    {
        _root = null;
        _nodePositions.Clear();
        _edges.Clear();
        _treeWidth = 0;
        _treeHeight = 0;
        InvalidateMeasure();
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override Size MeasureContent(Size available)
    {
        if (_root == null)
        {
            return new Size(LayoutPadding * 2, LayoutPadding * 2);
        }

        // Return computed tree size with padding
        return new Size(_treeWidth + LayoutPadding * 2, _treeHeight + LayoutPadding * 2);
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (!IsEffectivelyEnabled || e.Button != MouseButton.Left)
        {
            return;
        }

        if (_root == null || _nodePositions.Count == 0)
        {
            return;
        }

        // Perform hit testing on node positions (which are in control-relative coordinates)
        foreach (var (id, rect) in _nodePositions)
        {
            if (rect.Contains(e.GetPosition(this)))
            {
                var node = FindNode(_root, id);
                if (node != null)
                {
                    _vm.SelectNode(node);
                    Focus();
                    InvalidateVisual();
                    e.Handled = true;
                    return;
                }
            }
        }
    }

    /// <inheritdoc />
    protected override void OnRender(IGraphicsContext context)
    {
        base.OnRender(context);

        if (_root == null || _nodePositions.Count == 0)
        {
            return;
        }

        var dpiScale = GetDpi() / 96.0;
        var bounds = LayoutRounding.SnapBoundsRectToPixels(Bounds, dpiScale);

        // Draw edges first (below nodes)
        foreach (var (from, to, label, color) in _edges)
        {
            // Offset coordinates by Bounds top-left for parent-relative drawing
            var offsetFrom = new Point(from.X + bounds.X, from.Y + bounds.Y);
            var offsetTo = new Point(to.X + bounds.X, to.Y + bounds.Y);
            DrawEdge(context, offsetFrom, offsetTo, label, color, dpiScale);
        }

        // Draw nodes on top
        foreach (var (id, rect) in _nodePositions)
        {
            var node = FindNode(_root, id);
            if (node != null)
            {
                // Offset rect coordinates by Bounds top-left for parent-relative drawing
                var offsetRect = new Rect(rect.X + bounds.X, rect.Y + bounds.Y, rect.Width, rect.Height);
                DrawNode(context, offsetRect, node, dpiScale);
            }
        }
    }

    /// <inheritdoc />
    protected override void OnThemeChanged(Theme oldTheme, Theme newTheme)
    {
        base.OnThemeChanged(oldTheme, newTheme);
        InvalidateVisual();
    }

    private void ComputeLayout()
    {
        _nodePositions.Clear();
        _edges.Clear();
        _treeWidth = 0;
        _treeHeight = 0;

        if (_root == null)
        {
            return;
        }

        // Compute subtree widths for balanced layout
        var subtreeWidths = new Dictionary<Guid, double>();
        ComputeSubtreeWidth(_root, subtreeWidths);

        // Layout tree starting at (LayoutPadding, LayoutPadding)
        LayoutNode(_root, LayoutPadding, LayoutPadding, subtreeWidths);
    }

    private double ComputeSubtreeWidth(StrategyDecisionNode node, Dictionary<Guid, double> widths)
    {
        // Leaf node width
        if (node.Then == null && node.Else == null)
        {
            widths[node.Id] = NodeWidth;
            return NodeWidth;
        }

        double totalWidth = 0;
        double branchCount = 0;

        if (node.Then != null)
        {
            var thenWidth = ComputeSubtreeWidth(node.Then, widths);
            totalWidth += thenWidth;
            branchCount++;
        }

        if (node.Else != null)
        {
            var elseWidth = ComputeSubtreeWidth(node.Else, widths);
            totalWidth += elseWidth;
            branchCount++;
        }

        // Add spacing between branches
        if (branchCount > 1)
        {
            totalWidth += HorizontalSpacing;
        }

        // Node width is the max of its own width and subtree width
        var width = Math.Max(NodeWidth, totalWidth);
        widths[node.Id] = width;
        return width;
    }

    private double LayoutNode(
        StrategyDecisionNode node,
        double x,
        double y,
        Dictionary<Guid, double> subtreeWidths)
    {
        // Center this node within its subtree width
        var nodeWidth = subtreeWidths.GetValueOrDefault(node.Id, NodeWidth);
        var nodeX = x + (nodeWidth - NodeWidth) / 2;

        _nodePositions[node.Id] = new Rect(nodeX, y, NodeWidth, NodeHeight);

        double maxY = y + NodeHeight;

        if (node.Then != null || node.Else != null)
        {
            var childY = y + NodeHeight + VerticalSpacing;
            var subtreeX = x;

            // Layout THEN branch (left side)
            if (node.Then != null)
            {
                var thenWidth = subtreeWidths.GetValueOrDefault(node.Then.Id, NodeWidth);
                var thenMaxY = LayoutNode(node.Then, subtreeX, childY, subtreeWidths);

                // Add edge with THEN label
                var fromPoint = new Point(nodeX + NodeWidth / 2, y + NodeHeight);
                var toPoint = new Point(subtreeX + thenWidth / 2, childY);
                _edges.Add((fromPoint, toPoint, "THEN", ThenColor));

                subtreeX += thenWidth + HorizontalSpacing;
                maxY = Math.Max(maxY, thenMaxY);
            }

            // Layout ELSE branch (right side)
            if (node.Else != null)
            {
                var elseWidth = subtreeWidths.GetValueOrDefault(node.Else.Id, NodeWidth);
                var elseMaxY = LayoutNode(node.Else, subtreeX, childY, subtreeWidths);

                // Add edge with ELSE label
                var fromPoint = new Point(nodeX + NodeWidth / 2, y + NodeHeight);
                var toPoint = new Point(subtreeX + elseWidth / 2, childY);
                _edges.Add((fromPoint, toPoint, "ELSE", ElseColor));

                maxY = Math.Max(maxY, elseMaxY);
            }
        }

        _treeWidth = Math.Max(_treeWidth, x + nodeWidth);
        _treeHeight = Math.Max(_treeHeight, maxY);

        return maxY;
    }

    private void DrawNode(
        IGraphicsContext context,
        Rect rect,
        StrategyDecisionNode node,
        double dpiScale)
    {
        // Snap rect to pixels
        var snappedRect = LayoutRounding.SnapBoundsRectToPixels(rect, dpiScale);

        // Determine node color based on type
        var isLeaf = node.Then == null && node.Else == null;
        var bgColor = isLeaf ? LeafColor : SurfaceCard;

        // Highlight selected node
        var isSelected = _vm.SelectedNode.Value?.Id == node.Id;
        var borderThickness = isSelected ? 2.0 : 1.0;
        var borderColor = isSelected ? AccentColor : (isLeaf ? LeafColor : BorderColor);

        // Draw background
        context.FillRectangle(snappedRect, bgColor);
        // Draw border
        context.DrawRectangle(snappedRect, borderColor, borderThickness);

        // Draw node label
        var font = GetFont();
        var label = GetNodeLabel(node);
        var labelRect = snappedRect.Deflate(new Thickness(8, 4));

        context.DrawText(
            label,
            labelRect,
            font,
            TextPrimary,
            TextAlignment.Center,
            TextAlignment.Center,
            TextWrapping.Wrap);
    }

    private void DrawEdge(
        IGraphicsContext context,
        Point from,
        Point to,
        string label,
        Color color,
        double dpiScale)
    {
        // Snap points to pixels
        var snappedFrom = new Point(
            Math.Round(from.X * dpiScale) / dpiScale,
            Math.Round(from.Y * dpiScale) / dpiScale);
        var snappedTo = new Point(
            Math.Round(to.X * dpiScale) / dpiScale,
            Math.Round(to.Y * dpiScale) / dpiScale);

        // Draw line
        context.DrawLine(snappedFrom, snappedTo, color, 2);

        // Draw label at midpoint
        var midX = (snappedFrom.X + snappedTo.X) / 2;
        var midY = (snappedFrom.Y + snappedTo.Y) / 2;

        // Offset label to the side of the line
        var labelOffsetX = label == "THEN" ? -EdgeLabelOffset : EdgeLabelOffset;
        var labelPoint = new Point(midX + labelOffsetX, midY - 8);

        var font = GetFont();
        var labelRect = new Rect(
            labelPoint.X - 20,
            labelPoint.Y - 8,
            40,
            16);

        context.DrawText(
            label,
            labelRect,
            font,
            color,
            TextAlignment.Center,
            TextAlignment.Center,
            TextWrapping.NoWrap);
    }

    private static string GetNodeLabel(StrategyDecisionNode node)
    {
        // Leaf node shows the plan
        if (node.PlanGuid.HasValue)
        {
            return $"→ Plan\n{node.PlanGuid.Value:N}[..8]";
        }

        // Branch node shows condition summary
        if (node.If != null)
        {
            var opText = node.If.Operator switch
            {
                StrategyConditionGroupOperator.All => "ALL",
                StrategyConditionGroupOperator.Any => "ANY",
                StrategyConditionGroupOperator.None => "NONE",
                _ => "?"
            };
            var conditionCount = node.If.Conditions.Count;
            var groupCount = node.If.Groups.Count;
            return $"IF [{opText}]\n{conditionCount} cond, {groupCount} grp";
        }

        // No condition - unconditional branch
        return "IF [always]\n→ Then";
    }

    private static StrategyDecisionNode? FindNode(StrategyDecisionNode? root, Guid id)
    {
        if (root == null)
        {
            return null;
        }

        if (root.Id == id)
        {
            return root;
        }

        var thenResult = FindNode(root.Then, id);
        if (thenResult != null)
        {
            return thenResult;
        }

        return FindNode(root.Else, id);
    }
}
