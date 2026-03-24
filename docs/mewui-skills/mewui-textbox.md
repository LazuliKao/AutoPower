# TextBox

A single-line text input control.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`TextBase` → `Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

### Inherited from TextBase
| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Text` | `string` | `""` | The text content |
| `Placeholder` | `string` | `""` | Placeholder text shown when empty |
| `IsReadOnly` | `bool` | `false` | Whether the text is read-only |
| `CaretPosition` | `int` | `0` | Current caret position |
| `HasSelection` | `bool` | - | Whether text is selected |
| `AcceptTab` | `bool` | `false` | Whether to accept tab characters |

### Inherited from Control
- `Background` - Background color
- `Foreground` - Foreground (text) color
- `BorderBrush` - Border color
- `BorderThickness` - Border thickness
- `CornerRadius` - Corner radius
- `Padding` - Inner padding
- `FontFamily` - Font family
- `FontSize` - Font size
- `FontWeight` - Font weight

## Events

| Event | Type | Description |
|-------|------|-------------|
| `TextChanged` | `Action<string>` | Fired when text changes |

## Usage Examples

### Basic TextBox
```csharp
var textBox = new TextBox()
    .Placeholder("Enter your name...")
    .OnTextChanged(text => Console.WriteLine(text));
```

### Styled TextBox
```csharp
var textBox = new TextBox()
    .Placeholder("Email address")
    .FontSize(14)
    .Padding(new Thickness(8, 6))
    .CornerRadius(4)
    .BorderBrush(Colors.Gray);
```

### Read-only TextBox
```csharp
var textBox = new TextBox()
    .Text("Cannot edit this")
    .IsReadOnly(true);
```

### Password-like (conceptual)
```csharp
// Note: MewUI doesn't have built-in password masking
// This would require custom implementation
var textBox = new TextBox()
    .Placeholder("Password");
```

## Keyboard Support
- **Left/Right** - Move caret
- **Home/End** - Move to start/end
- **Backspace/Delete** - Delete characters
- **Ctrl+A** - Select all
- **Ctrl+C/X/V** - Copy/Cut/Paste
- **Tab** - Focus navigation (unless AcceptTab is true)

## Notes
- TextBox is focusable by default
- Supports horizontal scrolling when text exceeds width
- Supports IME composition for CJK input
- Selection can be manipulated via keyboard or mouse
