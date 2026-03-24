# GroupBox

A container control that draws a border with a header (WinForms-style GroupBox).

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`HeaderedContentControl` → `ContentControl` → `Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Header` | `Element?` | `null` | Header content element |
| `Content` | `Element?` | `null` | Main content element |
| `HeaderSpacing` | `double` | `4` | Spacing between header and content |
| `HeaderInset` | `double` | `0` | Horizontal inset for the header |

### Inherited from Control
- `Background` - Background color
- `BorderBrush` - Border color
- `BorderThickness` - Border thickness
- `CornerRadius` - Corner radius
- `Padding` - Inner padding

## Usage Examples

### Basic GroupBox
```csharp
var groupBox = new GroupBox()
    .Header("Settings")
    .Content(
        new StackPanel()
            .Spacing(8)
            .Children(
                new CheckBox().Text("Option 1"),
                new CheckBox().Text("Option 2")
            )
    );
```

### With Custom Header
```csharp
var groupBox = new GroupBox()
    .Header(
        new Label()
            .Text("Advanced Options")
            .Bold()
            .FontSize(14)
    )
    .Content(content);
```

### Styled GroupBox
```csharp
var groupBox = new GroupBox()
    .Header("Configuration")
    .Content(content)
    .BorderBrush(Colors.Gray)
    .CornerRadius(8)
    .Padding(new Thickness(12));
```

### With Header Inset
```csharp
var groupBox = new GroupBox()
    .HeaderInset(12)
    .Header("Indented Header")
    .Content(content);
```

## Notes
- GroupBox is not focusable (`Focusable = false`)
- Header is positioned above the bordered content area
- The border does not include the header area (WinForms style)
- HeaderSpacing controls gap between header text and content border
