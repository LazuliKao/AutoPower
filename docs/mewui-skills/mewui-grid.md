# Grid

A panel that arranges children in a grid of rows and columns.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`Panel` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RowDefinitions` | `IList<RowDefinition>` | Empty | Row definitions collection |
| `ColumnDefinitions` | `IList<ColumnDefinition>` | Empty | Column definitions collection |
| `AutoIndexing` | `bool` | `true` | Auto-place children without explicit Row/Column |
| `Spacing` | `double` | `0` | Spacing between grid cells |

### Inherited from Panel
- `Children` - Child elements collection
- `Padding` - Inner padding

## Attached Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Grid.Row` | `int` | `0` | Row index for element |
| `Grid.Column` | `int` | `0` | Column index for element |
| `Grid.RowSpan` | `int` | `1` | Number of rows to span |
| `Grid.ColumnSpan` | `int` | `1` | Number of columns to span |

## GridLength

Represents row/column sizing:
- `GridLength.Auto` - Size to content
- `GridLength.Star` - Proportional sizing (1*)
- `GridLength.Stars(n)` - Proportional with weight (n*)
- `GridLength.Pixels(n)` - Fixed pixel size
- Implicit from `double`: `100.0` → Pixel(100)

## Usage Examples

### Basic Grid
```csharp
var grid = new Grid()
    .RowDefinitions(
        new RowDefinition() { Height = GridLength.Auto },
        new RowDefinition() { Height = GridLength.Star }
    )
    .ColumnDefinitions(
        new ColumnDefinition() { Width = GridLength.Auto },
        new ColumnDefinition() { Width = GridLength.Star }
    )
    .Children(
        new Label() { Text = "Name:" }.SetRow(0).SetColumn(0),
        new TextBox().SetRow(0).SetColumn(1),
        new Label() { Text = "Email:" }.SetRow(1).SetColumn(0),
        new TextBox().SetRow(1).SetColumn(1)
    );
```

### With Spacing
```csharp
var grid = new Grid()
    .Spacing(8)
    .ColumnDefinitions(
        new ColumnDefinition() { Width = GridLength.Star },
        new ColumnDefinition() { Width = GridLength.Star }
    )
    .Children(
        button1.SetColumn(0),
        button2.SetColumn(1)
    );
```

### Row/Column Span
```csharp
var grid = new Grid()
    .RowDefinitions(
        new RowDefinition() { Height = GridLength.Auto },
        new RowDefinition() { Height = GridLength.Auto }
    )
    .ColumnDefinitions(
        new ColumnDefinition() { Width = GridLength.Star },
        new ColumnDefinition() { Width = GridLength.Star }
    )
    .Children(
        // Header spans both columns
        new Label().Text("Header").SetColumnSpan(2),
        
        // Content in first column
        content1.SetRow(1).SetColumn(0),
        
        // Content in second column
        content2.SetRow(1).SetColumn(1)
    );
```

### Star Sizing
```csharp
var grid = new Grid()
    .ColumnDefinitions(
        new ColumnDefinition() { Width = GridLength.Stars(2) },  // 2/3 of space
        new ColumnDefinition() { Width = GridLength.Star }        // 1/3 of space
    )
    .Children(
        sidebar.SetColumn(0),
        mainContent.SetColumn(1)
    );
```

### Auto Indexing
```csharp
// With AutoIndexing=true (default), children are placed automatically
var grid = new Grid()
    .AutoIndexing(true)
    .RowDefinitions(
        new RowDefinition(),
        new RowDefinition()
    )
    .ColumnDefinitions(
        new ColumnDefinition(),
        new ColumnDefinition()
    )
    .Children(
        // These will be placed: (0,0), (0,1), (1,0), (1,1)
        item1, item2, item3, item4
    );
```

## Notes
- Grid supports WPF-like star sizing behavior
- Auto placement respects existing explicit placements
- Spacing adds gaps between rows and columns (not around edges)
- Default row/column is Star sizing if not specified
