# Button

A button control that responds to clicks.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Content` | `Element?` | `null` | The content element displayed inside the button |
| `CanClick` | `Func<bool>?` | `null` | Function that determines if the button can be clicked |

### Inherited from Control
- `Background` - Background color
- `Foreground` - Foreground (text) color
- `BorderBrush` - Border color
- `BorderThickness` - Border thickness
- `CornerRadius` - Corner radius for background/border
- `Padding` - Inner padding
- `FontFamily` - Font family
- `FontSize` - Font size
- `FontWeight` - Font weight

## Events

| Event | Type | Description |
|-------|------|-------------|
| `Click` | `Action?` | Fired when the button is clicked |

## Usage Examples

### Basic Button
```csharp
var button = new Button()
    .Content("Click Me")
    .OnClick(() => Console.WriteLine("Clicked!"));
```

### Button with Label
```csharp
var button = new Button()
    .Content(
        new Label()
            .Text("Submit")
            .FontSize(14)
            .Bold()
    )
    .OnClick(() => Application.Quit());
```

### Styled Button
```csharp
var button = new Button()
    .Content("Save")
    .Background(Colors.Blue)
    .Foreground(Colors.White)
    .CornerRadius(8)
    .Padding(new Thickness(12, 6))
    .OnClick(() => SaveDocument());
```

### Disabled Button
```csharp
var button = new Button()
    .Content("Disabled")
    .IsEnabled(false);
```

### Conditional Enable
```csharp
var button = new Button()
    .Content("Submit")
    .CanClick(() => form.IsValid)
    .OnClick(() => form.Submit());
```

## Keyboard Support
- **Space** or **Enter** - Triggers click
- **Tab** - Focus navigation

## Notes
- Button is focusable by default (`Focusable = true`)
- Mouse capture is used during press/release for reliable click detection
- The button visually responds to hover, press, and focus states
