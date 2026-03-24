# Rectangle

Renders a rectangle, optionally with rounded corners.

## Namespace
`Aprillz.MewUI`

## Inheritance
`Shape` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RadiusX` | `double` | `0` | X-axis corner radius |
| `RadiusY` | `double` | `0` | Y-axis corner radius |

### Inherited from Shape
| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Fill` | `IBrush?` | `null` | Fill brush for the interior |
| `Stroke` | `IBrush?` | `null` | Stroke brush for the outline |
| `StrokeThickness` | `double` | `0` | Stroke thickness in DIPs |
| `StrokeStyle` | `StrokeStyle` | default | Line cap, join, dash pattern |
| `Stretch` | `Stretch` | `None` | How geometry fills bounds |

## Usage Examples

### Basic Rectangle
```csharp
var rect = new Rectangle()
    .Fill(Colors.Blue)
    .Width(100)
    .Height(50);
```

### Rounded Rectangle
```csharp
var rect = new Rectangle()
    .Fill(Colors.LightGray)
    .RadiusX(8)
    .RadiusY(8)
    .Width(150)
    .Height(100);
```

### Rectangle with Stroke
```csharp
var rect = new Rectangle()
    .Fill(Colors.White)
    .Stroke(Colors.Black)
    .StrokeThickness(2)
    .Width(200)
    .Height(100);
```

### Outlined Only
```csharp
var rect = new Rectangle()
    .Stroke(Colors.Red)
    .StrokeThickness(3)
    .Width(120)
    .Height(80);
```

### Stretched Rectangle
```csharp
var rect = new Rectangle()
    .Fill(Colors.Green)
    .Stretch(Stretch.Fill)
    .Width(300)
    .Height(200);
```

### Different X/Y Radius
```csharp
var rect = new Rectangle()
    .Fill(Colors.Purple)
    .RadiusX(20)
    .RadiusY(10)
    .Width(200)
    .Height(100);
```

## Notes
- Rectangle automatically fits within element bounds
- Stroke is deflated by half thickness to stay within bounds
- Geometry is cached for performance
- Both RadiusX and RadiusY can be set independently for elliptical corners
