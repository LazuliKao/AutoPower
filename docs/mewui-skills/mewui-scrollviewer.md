# ScrollViewer

A scrollable content container with horizontal and vertical scrollbars.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`ContentControl` → `Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Content` | `Element?` | `null` | The scrollable content |
| `VerticalScroll` | `ScrollMode` | `Auto` | Vertical scrollbar mode |
| `HorizontalScroll` | `ScrollMode` | `Disabled` | Horizontal scrollbar mode |
| `VerticalOffset` | `double` | `0` | Vertical scroll offset (read-only) |
| `HorizontalOffset` | `double` | `0` | Horizontal scroll offset (read-only) |
| `ViewportCornerRadius` | `double` | `0` | Corner radius for viewport clip |

### Inherited from Control
- `Background` - Background color
- `BorderBrush` - Border color
- `BorderThickness` - Border thickness
- `Padding` - Inner padding

## Events

| Event | Type | Description |
|-------|------|-------------|
| `ScrollChanged` | `Action?` | Fired when scroll metrics or offsets change |

## Methods

| Method | Description |
|--------|-------------|
| `SetScrollOffsets(double horizontal, double vertical)` | Sets both scroll offsets |
| `ScrollBy(double delta)` | Scrolls vertically by delta |
| `ScrollByHorizontal(double delta)` | Scrolls horizontally by delta |

## ScrollMode Values
- `ScrollMode.Disabled` - Scrolling is disabled
- `ScrollMode.Auto` - Scrollbars appear automatically when needed
- `ScrollMode.Visible` - Scrollbars are always visible

## Usage Examples

### Basic ScrollViewer
```csharp
var scrollViewer = new ScrollViewer()
    .Content(
        new StackPanel()
            .Children(/* many items */)
    );
```

### Horizontal Scrolling
```csharp
var scrollViewer = new ScrollViewer()
    .HorizontalScroll(ScrollMode.Auto)
    .Content(wideContent);
```

### Both Scrollbars
```csharp
var scrollViewer = new ScrollViewer()
    .VerticalScroll(ScrollMode.Auto)
    .HorizontalScroll(ScrollMode.Auto)
    .Content(largeContent);
```

### Programmatic Scrolling
```csharp
var scrollViewer = new ScrollViewer()
    .Content(content);

// Scroll to specific position
scrollViewer.SetScrollOffsets(100, 200);

// Scroll by amount
scrollViewer.ScrollBy(50);
```

### Styled ScrollViewer
```csharp
var scrollViewer = new ScrollViewer()
    .Content(content)
    .Background(Colors.White)
    .BorderBrush(Colors.Gray)
    .CornerRadius(4);
```

## Scrollbar Behavior
- Vertical scrollbar appears on the right edge
- Horizontal scrollbar appears on the bottom edge
- Scrollbars overlay the content (don't affect layout)
- Scrollbar thickness is controlled by Theme.Metrics

## Notes
- Content is clipped to viewport bounds
- Supports IScrollContent interface for virtualized content
- Mouse wheel scrolls vertically by default
- Horizontal mouse wheel (if available) scrolls horizontally
- Scroll positions are preserved when content changes
