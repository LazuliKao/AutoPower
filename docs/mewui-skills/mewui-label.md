# Label

A control that displays text.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Text` | `string` | `""` | The text content to display |
| `TextAlignment` | `TextAlignment` | `Left` | Horizontal text alignment |
| `VerticalTextAlignment` | `TextAlignment` | `Center` | Vertical text alignment |
| `TextWrapping` | `TextWrapping` | `NoWrap` | Text wrapping mode |
| `TextTrimming` | `TextTrimming` | `None` | Text trimming mode |

### Inherited from Control
- `Background` - Background color
- `Foreground` - Foreground (text) color
- `FontFamily` - Font family
- `FontSize` - Font size
- `FontWeight` - Font weight
- `Padding` - Inner padding

## Usage Examples

### Basic Label
```csharp
var label = new Label()
    .Text("Hello, World!");
```

### Styled Label
```csharp
var label = new Label()
    .Text("Welcome to MewUI")
    .FontSize(24)
    .Bold()
    .Foreground(Colors.DarkBlue);
```

### Multi-line with Wrapping
```csharp
var label = new Label()
    .Text("This is a long text that will wrap to multiple lines when the container is too narrow.")
    .TextWrapping(TextWrapping.Wrap)
    .MaxWidth(300);
```

### Centered Text
```csharp
var label = new Label()
    .Text("Centered")
    .TextAlignment(TextAlignment.Center)
    .VerticalTextAlignment(TextAlignment.Center);
```

### Text with Trimming
```csharp
var label = new Label()
    .Text("Very long text that might be truncated...")
    .TextTrimming(TextTrimming.Ellipsis)
    .MaxWidth(200);
```

## TextAlignment Values
- `TextAlignment.Left` - Left-aligned
- `TextAlignment.Center` - Center-aligned
- `TextAlignment.Right` - Right-aligned

## TextWrapping Values
- `TextWrapping.NoWrap` - No wrapping (default)
- `TextWrapping.Wrap` - Wrap at container edge

## TextTrimming Values
- `TextTrimming.None` - No trimming
- `TextTrimming.Ellipsis` - Trim with ellipsis (...)

## Notes
- Label does not respond to mouse events by default
- Explicit line breaks (\\r, \\n) in text automatically enable wrapping
- Text measurement is cached for performance
