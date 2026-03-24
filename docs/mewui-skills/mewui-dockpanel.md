# DockPanel

A panel that docks children to the edges (left, top, right, bottom).

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`Panel` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `LastChildFill` | `bool` | `true` | Whether the last child fills remaining space |
| `Spacing` | `double` | `0` | Spacing between docked children |

### Inherited from Panel
- `Children` - Child elements collection
- `Padding` - Inner padding

## Attached Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DockPanel.Dock` | `Dock` | `Left` | Dock position for element |

## Dock Values
- `Dock.Left` - Dock to left edge
- `Dock.Top` - Dock to top edge
- `Dock.Right` - Dock to right edge
- `Dock.Bottom` - Dock to bottom edge

## Usage Examples

### Basic DockPanel
```csharp
var dockPanel = new DockPanel()
    .Children(
        new Label().Text("Header").DockTop(),
        new Label().Text("Footer").DockBottom(),
        new Label().Text("Left Sidebar").DockLeft(),
        new Label().Text("Right Sidebar").DockRight(),
        new Label().Text("Main Content").DockFill()  // Last child fills
    );
```

### Window Layout
```csharp
var window = new Window()
    .Content(
        new DockPanel()
            .Children(
                menuBar.DockTop(),
                toolbar.DockTop(),
                statusBar.DockBottom(),
                sidebar.DockLeft(),
                mainContent.DockFill()
            )
    );
```

### With LastChildFill Disabled
```csharp
var dockPanel = new DockPanel()
    .LastChildFill(false)
    .Children(
        new Button().Content("Left").DockLeft(),
        new Button().Content("Right").DockRight()
        // No fill - both buttons dock to edges
    );
```

### With Spacing
```csharp
var dockPanel = new DockPanel()
    .Spacing(4)
    .Children(
        header.DockTop(),
        content.DockFill()
    );
```

## Dock Order
Children are docked in order:
1. First child docks to its edge
2. Second child docks to its edge (in remaining space)
3. Last child (if LastChildFill=true) fills remaining space

## Notes
- LastChildFill=true by default (common pattern for main content)
- Docked children are measured twice (WPF-style) for proper sizing
- Spacing is added between docked children
- Multiple children can dock to the same edge
