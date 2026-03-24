# Line

Renders a straight line between two points.

## Namespace
`Aprillz.MewUI`

## Inheritance
`Shape` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `X1` | `double` | `0` | Start point X coordinate |
| `Y1` | `double` | `0` | Start point Y coordinate |
| `X2` | `double` | `0` | End point X coordinate |
| `Y2` | `double` | `0` | End point Y coordinate |

### Inherited from Shape
| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Stroke` | `IBrush?` | `null` | Stroke brush |
| `StrokeThickness` | `double` | `0` | Stroke thickness in DIPs |
| `StrokeStyle` | `StrokeStyle` | default | Line cap, join, dash pattern |

## Usage Examples

### Basic Line
```csharp
var line = new Line()
    .X1(0).Y1(0)
    .X2(100).Y2(100)
    .Stroke(Colors.Black)
    .StrokeThickness(2);
```

### Horizontal Line
```csharp
var line = new Line()
    .X1(0).Y1(0)
    .X2(200).Y2(0)
    .Stroke(Colors.Gray)
    .StrokeThickness(1);
```

### Styled Line
```csharp
var line = new Line()
    .X1(10).Y1(10)
    .X2(190).Y2(10)
    .Stroke(Colors.Red)
    .StrokeThickness(3)
    .StrokeStyle(new StrokeStyle(lineCap: LineCap.Round));
```

### Dashed Line
```csharp
var line = new Line()
    .X1(0).Y1(0)
    .X2(100).Y2(50)
    .Stroke(Colors.Gray)
    .StrokeThickness(1)
    .StrokeStyle(new StrokeStyle(dashPattern: new[] { 4.0, 2.0 }));
```

## Notes
- Line is defined in local coordinates (relative to element bounds)
- Only Stroke is used (Fill has no effect on lines)
- Geometry is cached for performance
- Use with Canvas for absolute positioning
