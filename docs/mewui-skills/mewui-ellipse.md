# Ellipse

Renders an ellipse that fills the element bounds.

## Namespace
`Aprillz.MewUI`

## Inheritance
`Shape` → `FrameworkElement` → `UIElement` → `Element`

## Properties

### Inherited from Shape
| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Fill` | `IBrush?` | `null` | Fill brush for the interior |
| `Stroke` | `IBrush?` | `null` | Stroke brush for the outline |
| `StrokeThickness` | `double` | `0` | Stroke thickness in DIPs |
| `StrokeStyle` | `StrokeStyle` | default | Line cap, join, dash pattern |
| `Stretch` | `Stretch` | `None` | How geometry fills bounds |

## Usage Examples

### Basic Ellipse
```csharp
var ellipse = new Ellipse()
    .Fill(Colors.Blue)
    .Width(100)
    .Height(100);
```

### Ellipse with Stroke
```csharp
var ellipse = new Ellipse()
    .Fill(Colors.LightBlue)
    .Stroke(Colors.DarkBlue)
    .StrokeThickness(2)
    .Width(100)
    .Height(100);
```

### Stretched Ellipse
```csharp
var ellipse = new Ellipse()
    .Fill(Colors.Red)
    .Stretch(Stretch.Uniform)
    .Width(200)
    .Height(150);
```

### Circle (Equal Width/Height)
```csharp
var circle = new Ellipse()
    .Fill(Colors.Green)
    .Width(80)
    .Height(80);
```

## Notes
- Ellipse automatically fits within element bounds
- Stroke is deflated by half thickness to stay within bounds
- Geometry is cached for performance
- Default stretch is None (uses geometry bounds)
