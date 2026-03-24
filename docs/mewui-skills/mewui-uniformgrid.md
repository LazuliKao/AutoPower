# UniformGrid

A panel that arranges children in a grid with equal-sized cells.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`Panel` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Rows` | `int` | `0` | Number of rows (0 = auto) |
| `Columns` | `int` | `0` | Number of columns (0 = auto) |
| `Spacing` | `double` | `0` | Spacing between cells |

### Inherited from Panel
- `Children` - Child elements collection
- `Padding` - Inner padding

## Usage Examples

### Auto Grid Size
```csharp
// Rows and columns calculated automatically (roughly square)
var uniformGrid = new UniformGrid()
    .Spacing(4)
    .Children(
        button1, button2, button3,
        button4, button5, button6
    );
// Results in 3x2 or 2x3 grid
```

### Fixed Columns
```csharp
// 3 columns, rows calculated automatically
var uniformGrid = new UniformGrid()
    .Columns(3)
    .Spacing(8)
    .Children(
        item1, item2, item3,
        item4, item5, item6
    );
```

### Fixed Rows
```csharp
// 2 rows, columns calculated automatically
var uniformGrid = new UniformGrid()
    .Rows(2)
    .Spacing(8)
    .Children(
        item1, item2, item3, item4
    );
```

### Calculator Layout
```csharp
var calculator = new UniformGrid()
    .Columns(4)
    .Spacing(2)
    .Children(
        new Button().Content("7"),
        new Button().Content("8"),
        new Button().Content("9"),
        new Button().Content("/"),
        new Button().Content("4"),
        new Button().Content("5"),
        new Button().Content("6"),
        new Button().Content("*"),
        new Button().Content("1"),
        new Button().Content("2"),
        new Button().Content("3"),
        new Button().Content("-"),
        new Button().Content("0"),
        new Button().Content("."),
        new Button().Content("="),
        new Button().Content("+")
    );
```

### Image Gallery
```csharp
var gallery = new UniformGrid()
    .Columns(3)
    .Spacing(8)
    .Padding(new Thickness(8))
    .Children(
        thumbnails.Select(t => new Image().Source(t))
    );
```

## Auto-Calculation Rules
- **Both 0**: Make roughly square (columns = √count, rows = count/columns)
- **Rows=0**: rows = ceil(count/columns)
- **Columns=0**: columns = ceil(count/rows)

## Notes
- All cells are the same size
- Children are arranged left-to-right, top-to-bottom
- Invisible children are skipped
- Good for icon grids, tool palettes, calculator buttons
