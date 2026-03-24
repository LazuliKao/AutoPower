# PathShape

Renders an arbitrary PathGeometry.

## Namespace
`Aprillz.MewUI`

## Inheritance
`Shape` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Data` | `PathGeometry?` | `null` | The geometry that defines this path |

### Inherited from Shape
| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Fill` | `IBrush?` | `null` | Fill brush for the interior |
| `Stroke` | `IBrush?` | `null` | Stroke brush for the outline |
| `StrokeThickness` | `double` | `0` | Stroke thickness in DIPs |
| `StrokeStyle` | `StrokeStyle` | default | Line cap, join, dash pattern |
| `Stretch` | `Stretch` | `None` | How geometry fills bounds |

## Usage Examples

### Custom Path
```csharp
var path = new PathGeometry();
path.MoveTo(10, 10);
path.LineTo(100, 10);
path.LineTo(55, 90);
path.Close();

var triangle = new PathShape()
    .Data(path)
    .Fill(Colors.Blue);
```

### SVG-like Path
```csharp
var path = new PathGeometry();
path.MoveTo(0, 50);
path.CurveTo(25, 0, 75, 0, 100, 50);
path.CurveTo(75, 100, 25, 100, 0, 50);

var heart = new PathShape()
    .Data(path)
    .Fill(Colors.Red)
    .Width(100)
    .Height(100);
```

### Using PathGeometry Static Methods
```csharp
// Create from predefined shapes
var path = PathGeometry.FromRect(new Rect(0, 0, 100, 50));
var shape = new PathShape()
    .Data(path)
    .Fill(Colors.Green);

var ellipsePath = PathGeometry.FromEllipse(new Rect(0, 0, 80, 80));
var ellipse = new PathShape()
    .Data(ellipsePath)
    .Stroke(Colors.Blue)
    .StrokeThickness(2);
```

### Stretched Path
```csharp
var path = PathGeometry.FromRect(new Rect(0, 0, 10, 10));

var shape = new PathShape()
    .Data(path)
    .Fill(Colors.Orange)
    .Stretch(Stretch.Uniform)
    .Width(200)
    .Height(150);
```

## PathGeometry Methods

| Method | Description |
|--------|-------------|
| `MoveTo(x, y)` | Moves to point without drawing |
| `LineTo(x, y)` | Draws line to point |
| `CurveTo(cx1, cy1, cx2, cy2, x, y)` | Cubic Bezier curve |
| `ArcTo(rx, ry, rotation, largeArc, sweep, x, y)` | Arc segment |
| `Close()` | Closes the path |
| `FromRect(Rect)` | Creates rectangle path |
| `FromEllipse(Rect)` | Creates ellipse path |
| `FromRoundedRect(Rect, rx, ry)` | Creates rounded rectangle path |

## Notes
- PathShape is the base for custom vector graphics
- PathGeometry defines the shape in local coordinates
- Geometry is cached for performance
- Use Stretch to scale the path to fill bounds
- Fill and Stroke can be used independently or together
