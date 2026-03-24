# ListBox

A scrollable list control with item selection.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`VirtualizedItemsBase` → `Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ItemsSource` | `ISelectableItemsView` | Empty | The items data source |
| `SelectedIndex` | `int` | `-1` | Selected item index |
| `SelectedItem` | `object?` | - | Currently selected item (read-only) |
| `SelectedText` | `string?` | - | Currently selected item text (read-only) |
| `ItemHeight` | `double` | `NaN` | Height of each list item |
| `ItemPadding` | `Thickness` | Theme default | Padding around each item's text |
| `ItemTemplate` | `IDataTemplate` | Default template | Item template |
| `PresenterMode` | `ItemsPresenterMode` | `Fixed` | Virtualization strategy |
| `ZebraStriping` | `bool` | `true` | Enable alternating row colors |

### Inherited from Control
- `Background` - Background color
- `Foreground` - Foreground (text) color
- `BorderBrush` - Border color
- `CornerRadius` - Corner radius
- `Padding` - Inner padding

## Events

| Event | Type | Description |
|-------|------|-------------|
| `SelectionChanged` | `Action<object?>` | Fired when selected item changes |
| `ItemActivated` | `Action<int>` | Fired when item is activated (click or Enter) |

## Methods

| Method | Description |
|--------|-------------|
| `ScrollIntoView(int index)` | Scrolls the specified item into view |
| `TryGetItemIndexAt(Point position, out int index)` | Gets item index at position |

## Usage Examples

### Basic ListBox
```csharp
var items = new[] { "Item 1", "Item 2", "Item 3", "Item 4" };
var listBox = new ListBox()
    .ItemsSource(ItemsView.Create(items))
    .OnSelectionChanged(item => Console.WriteLine($"Selected: {item}"));
```

### With Custom Item Height
```csharp
var listBox = new ListBox()
    .ItemsSource(ItemsView.Create(items))
    .ItemHeight(32);
```

### Styled ListBox
```csharp
var listBox = new ListBox()
    .ItemsSource(ItemsView.Create(items))
    .CornerRadius(4)
    .BorderBrush(Colors.Gray)
    .ItemPadding(new Thickness(8, 4));
```

### Without Zebra Striping
```csharp
var listBox = new ListBox()
    .ItemsSource(ItemsView.Create(items))
    .ZebraStriping(false);
```

### Variable Height Items
```csharp
var listBox = new ListBox()
    .ItemsSource(ItemsView.Create(items))
    .PresenterMode(ItemsPresenterMode.Variable);
```

## Keyboard Support
- **Up/Down** - Navigate items
- **Home** - First item
- **End** - Last item
- **Enter** - Activate selected item
- **Tab** - Focus navigation

## Notes
- Supports virtualization for large item collections
- ScrollViewer is used internally for scrolling
- Default item template shows text with Label
- Hover highlighting is supported
- Selection highlighting uses theme accent color
