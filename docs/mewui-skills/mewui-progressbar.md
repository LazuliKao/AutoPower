# ProgressBar

A progress bar control for displaying completion percentage.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`RangeBase` → `Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Value` | `double` | `0` | Current progress value |
| `Minimum` | `double` | `0` | Minimum value |
| `Maximum` | `double` | `100` | Maximum value |

### Inherited from Control
- `Background` - Track background color
- `BorderBrush` - Border color
- `CornerRadius` - Corner radius
- `Padding` - Inner padding

## Usage Examples

### Basic ProgressBar
```csharp
var progressBar = new ProgressBar()
    .Value(50);
```

### With Observable Value
```csharp
var progress = new ObservableValue<double>(0);

var progressBar = new ProgressBar()
    .BindValue(progress);

// Later: update progress
progress.Value = 0.75;
```

### Custom Range
```csharp
var progressBar = new ProgressBar()
    .Minimum(0)
    .Maximum(1)
    .Value(0.5);
```

### Styled ProgressBar
```csharp
var progressBar = new ProgressBar()
    .CornerRadius(4)
    .Background(Colors.LightGray)
    .Height(12);
```

### Indeterminate Style (Conceptual)
```csharp
// MewUI doesn't have built-in indeterminate animation
// You would need to animate Value manually
var progress = new ObservableValue<double>(0);
var progressBar = new ProgressBar()
    .BindValue(progress);

// Animate using DispatcherTimer or AnimationManager
```

## Value Display
- Value is normalized to 0-1 range internally
- Filled portion shows `(Value - Minimum) / (Maximum - Minimum)`
- Accent color is used for the fill when enabled
- Disabled accent color is used when disabled

## Notes
- Default Maximum is 100
- Default Height is 10 DIPs
- Default DesiredWidth is 120 DIPs
- The fill is drawn with rounded corners matching the track
- Not interactive (no mouse/keyboard input)
