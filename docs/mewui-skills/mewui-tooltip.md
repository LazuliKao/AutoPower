# ToolTip

A tooltip popup control for displaying help text.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`ContentControl` → `Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Text` | `string` | `""` | Tooltip text (ignored if Content is set) |
| `Content` | `Element?` | `null` | Custom content element |

### Inherited from Control
- `Background` - Background color
- `Foreground` - Foreground (text) color
- `BorderBrush` - Border color
- `CornerRadius` - Corner radius
- `Padding` - Inner padding
- `FontFamily` - Font family
- `FontSize` - Font size

## Usage Examples

### Basic ToolTip (via Control property)
```csharp
var button = new Button()
    .Content("Hover me")
    .ToolTipText = "Click to submit";
```

### Custom Content ToolTip
```csharp
var button = new Button()
    .Content("Hover me");
button.ToolTipContent = new StackPanel()
    .Children(
        new Label().Text("Title").Bold(),
        new Label().Text("Detailed description here")
    );
```

### On TextBox
```csharp
var textBox = new TextBox()
    .Placeholder("Email")
    .ToolTipText = "Enter your email address";
```

## Setting ToolTips

### Using ToolTipText
```csharp
control.ToolTipText = "Simple tooltip text";
```

### Using ToolTipContent
```csharp
control.ToolTipContent = new Label()
    .Text("Rich tooltip")
    .FontSize(12);
```

## Behavior
- ToolTip appears on mouse hover after a short delay
- ToolTip follows mouse cursor with offset
- ToolTip disappears on mouse leave or any click
- ToolTip content is measured and positioned to stay within window bounds

## Notes
- ToolTip is not hit-test visible by default
- Position is calculated relative to mouse cursor
- Multiple controls can share tooltip instances
- ToolTipText takes precedence if Content is null
