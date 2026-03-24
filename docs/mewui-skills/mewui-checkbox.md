# CheckBox

A checkbox control with optional text label.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Text` | `string` | `""` | The checkbox label text |
| `IsChecked` | `bool?` | `false` | The checked state |
| `IsThreeState` | `bool` | `false` | Whether the checkbox supports indeterminate state |

### Inherited from Control
- `Background` - Background color
- `Foreground` - Foreground (text) color
- `BorderBrush` - Border color
- `CornerRadius` - Corner radius
- `Padding` - Inner padding
- `FontFamily` - Font family
- `FontSize` - Font size
- `FontWeight` - Font weight

## Events

| Event | Type | Description |
|-------|------|-------------|
| `CheckedChanged` | `Action<bool?>` | Fired when the checked state changes |

## Usage Examples

### Basic CheckBox
```csharp
var checkBox = new CheckBox()
    .Text("Accept terms")
    .OnCheckedChanged(isChecked => Console.WriteLine($"Checked: {isChecked}"));
```

### Checked by Default
```csharp
var checkBox = new CheckBox()
    .Text("Enable notifications")
    .IsChecked(true);
```

### Three-State CheckBox
```csharp
var checkBox = new CheckBox()
    .Text("Select items")
    .IsThreeState(true)
    .IsChecked(null);  // Indeterminate state

// States cycle: false → true → null → false...
```

### Styled CheckBox
```csharp
var checkBox = new CheckBox()
    .Text("Remember me")
    .FontSize(12)
    .Foreground(Colors.DarkGray);
```

### Data Binding
```csharp
var isEnabled = new ObservableValue<bool>(true);

var checkBox = new CheckBox()
    .Text("Enable feature")
    .BindIsChecked(isEnabled);
```

## Visual States
- **Unchecked** - `IsChecked == false`
- **Checked** - `IsChecked == true`
- **Indeterminate** - `IsChecked == null` (only when `IsThreeState == true`)

## Keyboard Support
- **Space** - Toggle checked state
- **Tab** - Focus navigation

## Notes
- CheckBox is focusable by default
- The checkbox box size is fixed at 14x14 DIPs
- Supports `VisualStateFlags.Checked` and `VisualStateFlags.Indeterminate` for styling
