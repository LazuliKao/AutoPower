# StackPanel

A panel that arranges children in a stack (vertical or horizontal).

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`Panel` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Orientation` | `Orientation` | `Vertical` | Stack orientation |
| `Spacing` | `double` | `0` | Spacing between children |

### Inherited from Panel
- `Children` - Child elements collection
- `Padding` - Inner padding

## Usage Examples

### Vertical Stack (Default)
```csharp
var stackPanel = new StackPanel()
    .Spacing(8)
    .Children(
        new Label().Text("Item 1"),
        new Label().Text("Item 2"),
        new Label().Text("Item 3")
    );
```

### Horizontal Stack
```csharp
var stackPanel = new StackPanel()
    .Orientation(Orientation.Horizontal)
    .Spacing(12)
    .Children(
        new Button().Content("Save"),
        new Button().Content("Cancel"),
        new Button().Content("Help")
    );
```

### Form Layout
```csharp
var form = new StackPanel()
    .Spacing(8)
    .Padding(new Thickness(16))
    .Children(
        new Label().Text("Username"),
        new TextBox().Placeholder("Enter username"),
        new Label().Text("Password"),
        new TextBox().Placeholder("Enter password"),
        new Button().Content("Login")
    );
```

### Toolbar
```csharp
var toolbar = new StackPanel()
    .Orientation(Orientation.Horizontal)
    .Spacing(4)
    .Children(
        new Button().Content("New"),
        new Button().Content("Open"),
        new Button().Content("Save"),
        new Separator(),
        new Button().Content("Cut"),
        new Button().Content("Copy"),
        new Button().Content("Paste")
    );
```

## Orientation Values
- `Orientation.Vertical` - Stack children vertically (default)
- `Orientation.Horizontal` - Stack children horizontally

## Layout Behavior
- **Vertical**: Children get full width, height is their desired height
- **Horizontal**: Children get full height, width is their desired width
- Spacing is added between children (not before first or after last)

## Notes
- Simple and lightweight panel
- No wrapping (use WrapPanel for wrapping)
- No star sizing (use Grid for flexible sizing)
- Children are sized to their desired size in the stack direction
