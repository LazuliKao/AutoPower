# Shape (Base Class)

Abstract base class for shape elements that render a PathGeometry.

## Namespace
`Aprillz.MewUI`

## Inheritance
`FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Fill` | `IBrush?` | `null` | Brush used to fill the shape interior |
| `Stroke` | `IBrush?` | `null` | Brush used to stroke the shape outline |
| `StrokeThickness` | `double` | `0` | Stroke thickness in DIPs |
| `StrokeStyle` | `StrokeStyle` | default | Line cap, line join, dash pattern |
| `Stretch` | `Stretch` | `None` | How geometry is stretched to fill bounds |

## Stretch Modes

| Mode | Description |
|------|-------------|
| `Stretch.None` | No stretching, use geometry bounds |
| `Stretch.Fill` | Stretch to fill exactly (may distort) |
| `Stretch.Uniform` | Fit within bounds, maintain aspect ratio |
| `Stretch.UniformToFill` | Fill bounds, maintain aspect ratio (may crop) |

## StrokeStyle

Controls stroke appearance:
- `LineCap` - Start/end cap style (Flat, Round, Square)
- `LineJoin` - Corner join style (Miter, Round, Bevel)
- `DashPattern` - Array of dash/gap lengths

## Usage

Shape is abstract. Use concrete implementations:
- `Ellipse` - Ellipse/circle
- `Rectangle` - Rectangle (rounded corners)
- `Line` - Straight line
- `PathShape` - Custom path

## Notes
- Geometry is rendered relative to element bounds
- Stroke is deflated to stay within bounds
- Geometry is cached for performance
- Stretch transforms are applied during rendering
