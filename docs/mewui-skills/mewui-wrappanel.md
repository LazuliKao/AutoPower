# WrapPanel

A panel that arranges children in a flowing layout, wrapping to the next line when needed.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`Panel` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Orientation` | `Orientation` | `Horizontal` | Flow orientation |
| `Spacing` | `double` | `0` | Spacing between items and lines |
| `ItemWidth` | `double` | `NaN` | Fixed width for all items (NaN = auto) |
| `ItemHeight` | `double` | `NaN` | Fixed height for all items (NaN = auto) |

### Inherited from Panel
- `Children` - Child elements collection
- `Padding` - Inner padding

## Usage Examples

### Basic WrapPanel
```csharp
var wrapPanel = new WrapPanel()
    .Spacing(8)
    .Children(
        new Button().Content("Tag 1"),
        new Button().Content("Tag 2"),
        new Button().Content("Tag 3"),
        new Button().Content("Tag 4"),
        new Button().Content("Tag 5"),
        new Button().Content("Tag 6")
    );
// Wraps when container is too narrow
```

### Fixed Item Size
```csharp
var wrapPanel = new WrapPanel()
    .ItemWidth(100)
    .ItemHeight(32)
    .Spacing(4)
    .Children(
        items.Select(item => new Button().Content(item))
    );
```

### Vertical Wrap
```csharp
var wrapPanel = new WrapPanel()
    .Orientation(Orientation.Vertical)
    .Spacing(8)
    .Children(
        verticalItems
    );
```

### Tag Cloud
```csharp
var tags = new[] { "C#", ".NET", "WPF", "MewUI", "UI", "Framework", "Cross-Platform" };
var tagCloud = new WrapPanel()
    .Spacing(4)
    .Children(
        tags.Select(tag => 
            new Label()
                .Text(tag)
                .Background(Colors.LightBlue)
                .Padding(new Thickness(8, 4))
                .CornerRadius(4)
        )
    );
```

### Image Thumbnails
```csharp
var gallery = new WrapPanel()
    .ItemWidth(150)
    .ItemHeight(100)
    .Spacing(8)
    .Padding(new Thickness(8))
    .Children(
        images.Select(img => new Image().Source(img))
    );
```

## Orientation Values
- `Orientation.Horizontal` - Flow left-to-right, wrap vertically (default)
- `Orientation.Vertical` - Flow top-to-bottom, wrap horizontally

## Layout Behavior
- Items flow in the main direction
- When items exceed available space, they wrap to the next line
- Each line has consistent height (horizontal) or width (vertical)
- Spacing applies between items and between lines

## Notes
- Good for tag clouds, button bars, image galleries
- ItemWidth/ItemHeight enforce uniform sizing when set
- Without fixed sizes, items use their desired size
- Wrapping is calculated based on available container width/height
