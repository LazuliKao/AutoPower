# TreeView

A hierarchical tree view control with expand/collapse functionality.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ItemsSource` | `ITreeItemsView` | Empty | The tree items data source |
| `SelectedNode` | `TreeViewNode?` | `null` | Currently selected node |
| `SelectedItem` | `object?` | - | Selected item object (read-only) |
| `ItemHeight` | `double` | `NaN` | Height of each node row |
| `ItemPadding` | `Thickness` | Theme default | Padding around each node's text |
| `ItemTemplate` | `IDataTemplate` | Default template | Node template |
| `Indent` | `double` | `16` | Horizontal indentation per tree level |
| `ExpandTrigger` | `TreeViewExpandTrigger` | `ClickChevron` | What toggles node expansion |

### Inherited from Control
- `Background` - Background color
- `Foreground` - Foreground (text) color
- `BorderBrush` - Border color
- `CornerRadius` - Corner radius

## Events

| Event | Type | Description |
|-------|------|-------------|
| `SelectionChanged` | `Action<object?>` | Fired when selected item changes |
| `SelectedNodeChanged` | `Action<TreeViewNode?>` | Fired when selected node changes |

## Methods

| Method | Description |
|--------|-------------|
| `IsExpanded(TreeViewNode node)` | Checks if node is expanded |
| `Expand(TreeViewNode node)` | Expands a node |
| `Collapse(TreeViewNode node)` | Collapses a node |
| `Toggle(TreeViewNode node)` | Toggles expansion state |
| `ScrollIntoView(int index)` | Scrolls item into view |
| `ScrollIntoViewSelected()` | Scrolls selected node into view |

## Usage Examples

### Basic TreeView
```csharp
var root = new TreeViewNode("Root");
var child1 = new TreeViewNode("Child 1");
var child2 = new TreeViewNode("Child 2");
root.Children.Add(child1);
root.Children.Add(child2);

var treeView = new TreeView()
    .ItemsSource(new TreeViewNodeItemsView(root));
```

### With Expand Trigger
```csharp
var treeView = new TreeView()
    .ItemsSource(source)
    .ExpandTrigger(TreeViewExpandTrigger.ClickNode);
```

### Custom Indent
```csharp
var treeView = new TreeView()
    .ItemsSource(source)
    .Indent(24);
```

### Styled TreeView
```csharp
var treeView = new TreeView()
    .ItemsSource(source)
    .ItemHeight(28)
    .ItemPadding(new Thickness(8, 4))
    .CornerRadius(4);
```

## ExpandTrigger Values
- `TreeViewExpandTrigger.ClickChevron` - Expand/collapse only on chevron click
- `TreeViewExpandTrigger.DoubleClickNode` - Expand/collapse on chevron or double-click
- `TreeViewExpandTrigger.ClickNode` - Expand/collapse on chevron or single-click

## Keyboard Support
- **Up/Down** - Navigate nodes
- **Left** - Collapse node or move to parent
- **Right** - Expand node or move to first child
- **Space** - Toggle expansion
- **Home** - First visible item
- **End** - Last visible item

## Notes
- Supports virtualization for large trees
- Chevron glyph indicates expandable nodes
- Selection highlighting uses theme accent color
- Hover highlighting is supported
