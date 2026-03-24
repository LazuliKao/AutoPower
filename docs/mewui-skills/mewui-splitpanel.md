# SplitPanel

A two-pane layout panel with a draggable splitter.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`Panel` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Orientation` | `Orientation` | `Horizontal` | Split direction |
| `First` | `UIElement?` | `null` | First pane content |
| `Second` | `UIElement?` | `null` | Second pane content |
| `FirstLength` | `GridLength` | `Star` | First pane length |
| `SecondLength` | `GridLength` | `Star` | Second pane length |
| `SplitterThickness` | `double` | `8` | Splitter thickness in DIPs |
| `MinFirst` | `double` | `0` | Minimum first pane size |
| `MinSecond` | `double` | `0` | Minimum second pane size |
| `MaxFirst` | `double` | `∞` | Maximum first pane size |
| `MaxSecond` | `double` | `∞` | Maximum second pane size |

### Inherited from Panel
- `Padding` - Inner padding
- `Background` - Background color

## Usage Examples

### Basic Horizontal Split
```csharp
var splitPanel = new SplitPanel()
    .Orientation(Orientation.Horizontal)
    .First(new Label().Text("Left Pane"))
    .Second(new Label().Text("Right Pane"));
```

### Vertical Split
```csharp
var splitPanel = new SplitPanel()
    .Orientation(Orientation.Vertical)
    .First(new Label().Text("Top Pane"))
    .Second(new Label().Text("Bottom Pane"));
```

### Fixed First Pane
```csharp
var splitPanel = new SplitPanel()
    .FirstLength(GridLength.Pixels(200))
    .SecondLength(GridLength.Star)
    .First(sidebar)
    .Second(mainContent);
```

### With Min/Max Constraints
```csharp
var splitPanel = new SplitPanel()
    .FirstLength(GridLength.Pixels(250))
    .MinFirst(150)
    .MaxFirst(400)
    .First(sidebar)
    .Second(mainContent);
```

### Explorer-Style Layout
```csharp
var splitPanel = new SplitPanel()
    .Orientation(Orientation.Horizontal)
    .First(
        new TreeView()
            .ItemsSource(fileSystemNodes)
    )
    .Second(
        new ListBox()
            .ItemsSource(fileList)
    );
```

## GridLength Options
- `GridLength.Star` - Proportional sizing
- `GridLength.Pixels(n)` - Fixed size
- `GridLength.Auto` - Size to content

## Orientation Values
- `Orientation.Horizontal` - Left/Right split with vertical splitter
- `Orientation.Vertical` - Top/Bottom split with horizontal splitter

## Splitter Behavior
- Dragging splitter changes FirstLength to Pixels
- SecondLength remains Star
- Min/Max constraints are enforced during drag
- Cursor changes to SizeWE (horizontal) or SizeNS (vertical)

## Notes
- Only two panes are supported (First and Second)
- Splitter is automatically shown when both panes are visible
- Splitter has a visual grip line
- Supports keyboard focus for accessibility
