# RadioButton

A radio button control with optional text label.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`ToggleBase` → `Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Text` | `string` | `""` | The radio button label text |
| `IsChecked` | `bool` | `false` | Whether the radio button is selected |
| `GroupName` | `string?` | `null` | Group name for mutual exclusion |

### Inherited from ToggleBase
- `IsChecked` - Toggle state

### Inherited from Control
- `Background` - Background color
- `Foreground` - Foreground (text) color
- `BorderBrush` - Border color
- `Padding` - Inner padding
- `FontFamily` - Font family
- `FontSize` - Font size
- `FontWeight` - Font weight

## Events

| Event | Type | Description |
|-------|------|-------------|
| `CheckedChanged` | `Action<bool>` | Fired when checked state changes |

## Usage Examples

### Basic RadioButton Group
```csharp
var panel = new StackPanel()
    .Orientation(Orientation.Vertical)
    .Spacing(8)
    .Children(
        new RadioButton()
            .Text("Option A")
            .GroupName("options")
            .IsChecked(true),
        new RadioButton()
            .Text("Option B")
            .GroupName("options"),
        new RadioButton()
            .Text("Option C")
            .GroupName("options")
    );
```

### Scoped RadioButton (without GroupName)
```csharp
// RadioButtons in the same parent automatically form a group
var panel = new StackPanel()
    .Children(
        new RadioButton()
            .Text("Small")
            .IsChecked(true),
        new RadioButton()
            .Text("Medium"),
        new RadioButton()
            .Text("Large")
    );
```

### Styled RadioButton
```csharp
var radioButton = new RadioButton()
    .Text("Dark Mode")
    .FontSize(14)
    .Foreground(Colors.White);
```

## Group Behavior
- **With GroupName**: Radio buttons with the same `GroupName` anywhere in the window are mutually exclusive
- **Without GroupName**: Radio buttons are mutually exclusive within their parent container

## Keyboard Support
- **Space** - Select the radio button
- **Tab** - Focus navigation

## Notes
- RadioButton is focusable by default
- Only one radio button in a group can be checked at a time
- Group registration happens when the button is attached to a Window
- The radio button circle is rendered as an ellipse
