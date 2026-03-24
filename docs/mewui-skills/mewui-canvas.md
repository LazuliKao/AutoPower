# Canvas

A panel that positions children using absolute coordinates.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`Panel` → `FrameworkElement` → `UIElement` → `Element`

## Attached Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Canvas.Left` | `double` | `NaN` | Left position |
| `Canvas.Top` | `double` | `NaN` | Top position |
| `Canvas.Right` | `double` | `NaN` | Right position |
| `Canvas.Bottom` | `double` | `NaN` | Bottom position |

### Inherited from Panel
- `Children` - Child elements collection
- `Padding` - Inner padding

## Usage Examples

### Basic Canvas
```csharp
var canvas = new Canvas()
    .Children(
        new Button()
            .Content("Absolute")
            .SetLeft(50)
            .SetTop(30),
        new Label()
            .Text("Positioned")
            .SetLeft(100)
            .SetTop(100)
    );
```

### Position from Multiple Edges
```csharp
var canvas = new Canvas()
    .Children(
        // Stretch element using Left + Right
        new TextBox()
            .SetLeft(10)
            .SetRight(10)
            .SetTop(10),
        
        // Position from bottom-right
        new Button()
            .Content("OK")
            .SetRight(20)
            .SetBottom(20)
    );
```

### Drawing Canvas
```csharp
var canvas = new Canvas();

// Add shapes at specific positions
var rect = new Rectangle()
    .Fill(Colors.Blue)
    .Width(100)
    .Height(50);
Canvas.SetLeft(rect, 10);
Canvas.SetTop(rect, 10);
canvas.Children.Add(rect);

var ellipse = new Ellipse()
    .Fill(Colors.Red)
    .Width(60)
    .Height(60);
Canvas.SetLeft(ellipse, 150);
Canvas.SetTop(ellipse, 30);
canvas.Children.Add(ellipse);
```

### Overlay Layout
```csharp
var canvas = new Canvas()
    .Children(
        // Background image fills canvas
        new Image()
            .Source(background)
            .SetLeft(0)
            .SetTop(0),
        
        // Overlay button in corner
        new Button()
            .Content("Close")
            .SetRight(10)
            .SetTop(10)
    );
```

## Position Rules

### Left + Right
When both `Left` and `Right` are set:
- Element is positioned at `Left` from left edge
- Width is calculated as `CanvasWidth - Left - Right`

### Top + Bottom
When both `Top` and `Bottom` are set:
- Element is positioned at `Top` from top edge
- Height is calculated as `CanvasHeight - Top - Bottom`

### Single Edge
When only one edge is set:
- Element uses its DesiredSize for the other dimension
- Position is calculated relative to the set edge

## Notes
- Canvas measures children with infinite space (Size.Infinity)
- Canvas returns Size.Empty for its own desired size
- Good for absolute positioning, overlays, drawing surfaces
- Not recommended for responsive layouts
- Invisible children are still positioned
