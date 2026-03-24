# Slider

A slider control for selecting a numeric value within a range.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`RangeBase` → `Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Value` | `double` | `0` | Current slider value |
| `Minimum` | `double` | `0` | Minimum value |
| `Maximum` | `double` | `100` | Maximum value |
| `SmallChange` | `double` | `1` | Increment for small changes |
| `ChangeOnWheel` | `bool` | `true` | Whether mouse wheel changes value |
| `ThumbBrush` | `Color` | - | Thumb fill color |
| `ThumbBorderBrush` | `Color` | - | Thumb border color |

### Inherited from Control
- `Background` - Track background color
- `BorderBrush` - Track border color
- `Padding` - Inner padding

## Events

| Event | Type | Description |
|-------|------|-------------|
| `ValueChanged` | `Action<double>` | Fired when value changes |

## Usage Examples

### Basic Slider
```csharp
var slider = new Slider()
    .Minimum(0)
    .Maximum(100)
    .Value(50)
    .OnValueChanged(v => Console.WriteLine($"Value: {v}"));
```

### Slider with Label
```csharp
var percent = new ObservableValue<double>(0.25);

var slider = new Slider()
    .BindValue(percent);

var label = new Label()
    .BindText(percent, v => $"Percent: {v:P0}");
```

### Custom Range
```csharp
var slider = new Slider()
    .Minimum(-100)
    .Maximum(100)
    .SmallChange(10)
    .Value(0);
```

### Styled Slider
```csharp
var slider = new Slider()
    .ThumbBrush(Colors.Blue)
    .ThumbBorderBrush(Colors.DarkBlue)
    .Background(Colors.LightGray);
```

### Disabled Wheel
```csharp
var slider = new Slider()
    .ChangeOnWheel(false)
    .Value(50);
```

## Keyboard Support
- **Left/Down** - Decrease by SmallChange
- **Right/Up** - Increase by SmallChange
- **Page Down** - Decrease by 10% of range
- **Page Up** - Increase by 10% of range
- **Home** - Set to Minimum
- **End** - Set to Maximum

## Notes
- Default Maximum is 100
- Thumb is a 14x14 circle
- Track height is 4 DIPs
- Supports mouse drag for value changes
- Filled track shows progress from minimum to current value
