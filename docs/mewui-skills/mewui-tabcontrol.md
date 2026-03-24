# TabControl

A tabbed control with header buttons and content display.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SelectedIndex` | `int` | `-1` | Selected tab index |
| `SelectedTab` | `TabItem?` | - | Currently selected tab item (read-only) |
| `SelectedItem` | `object?` | - | Selected item object (read-only) |
| `Tabs` | `IReadOnlyList<TabItem>` | Empty | Collection of tab items (read-only) |
| `VerticalScroll` | `ScrollMode` | `Disabled` | Vertical scroll mode for content |
| `HorizontalScroll` | `ScrollMode` | `Disabled` | Horizontal scroll mode for content |

### Inherited from Control
- `Background` - Content area background
- `BorderBrush` - Border color
- `BorderThickness` - Border thickness
- `Padding` - Content padding

## Events

| Event | Type | Description |
|-------|------|-------------|
| `SelectionChanged` | `Action<object?>` | Fired when selected tab changes |

## Methods

| Method | Description |
|--------|-------------|
| `AddTab(TabItem tab)` | Adds a tab |
| `AddTabs(params TabItem[] tabs)` | Adds multiple tabs |
| `ClearTabs()` | Removes all tabs |
| `RemoveTabAt(int index)` | Removes tab at index |

## Usage Examples

### Basic TabControl
```csharp
var tabControl = new TabControl()
    .AddTabs(
        new TabItem()
            .Header("General")
            .Content(new Label().Text("General settings")),
        new TabItem()
            .Header("Advanced")
            .Content(new Label().Text("Advanced settings"))
    );
```

### Styled TabControl
```csharp
var tabControl = new TabControl()
    .Padding(new Thickness(12))
    .BorderBrush(Colors.Gray)
    .AddTabs(
        new TabItem().Header("Tab 1").Content(content1),
        new TabItem().Header("Tab 2").Content(content2)
    );
```

### Dynamic Tabs
```csharp
var tabControl = new TabControl();

// Add tab dynamically
tabControl.AddTab(new TabItem()
    .Header("New Tab")
    .Content(new Label().Text("New content")));

// Remove tab
tabControl.RemoveTabAt(0);

// Clear all
tabControl.ClearTabs();
```

### With Selection Changed
```csharp
var tabControl = new TabControl()
    .AddTabs(...)
    .OnSelectionChanged(item => 
    {
        if (item is TabItem tab)
            Console.WriteLine($"Selected: {tab.Header}");
    });
```

## Keyboard Support
- **Left Arrow** - Select previous tab
- **Right Arrow** - Select next tab
- **Ctrl+Page Up** - Select previous tab
- **Ctrl+Page Down** - Select next tab

## TabItem Properties

| Property | Type | Description |
|----------|------|-------------|
| `Header` | `Element?` | Tab header content |
| `Content` | `Element?` | Tab content |
| `IsEnabled` | `bool` | Whether tab is enabled |

## Notes
- Tab headers are rendered as TabHeaderButton controls
- Content is swapped when tabs are selected
- Scroll positions are preserved per tab
- Focus is managed when switching tabs
