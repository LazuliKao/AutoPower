# ContextMenu

A context menu popup control for displaying menu items.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Menu` | `Menu` | New Menu | The menu model |
| `Items` | `IList<MenuEntry>` | - | Menu items collection (read-only) |
| `ItemHeight` | `double` | `NaN` | Height of menu items |
| `ItemPadding` | `Thickness` | Theme default | Padding around items |
| `MaxMenuHeight` | `double` | `320` | Maximum menu height |

### Inherited from Control
- `Background` - Background color
- `Foreground` - Foreground (text) color
- `BorderBrush` - Border color
- `CornerRadius` - Corner radius
- `FontFamily` - Font family
- `FontSize` - Font size

## Methods

| Method | Description |
|--------|-------------|
| `AddItem(string text, Action? onClick, bool isEnabled, string? shortcutText)` | Adds a menu item |
| `AddSubMenu(string text, Menu subMenu, bool isEnabled, string? shortcutText)` | Adds a submenu |
| `AddEntry(MenuEntry entry)` | Adds a menu entry |
| `AddSeparator()` | Adds a separator |
| `SetItems(params MenuEntry[] items)` | Sets menu items |

## Usage Examples

### Basic ContextMenu
```csharp
var contextMenu = new ContextMenu()
    .AddItem("Copy", () => Copy(), shortcutText: "Ctrl+C")
    .AddItem("Paste", () => Paste(), shortcutText: "Ctrl+V")
    .AddSeparator()
    .AddItem("Delete", () => Delete());

var control = new TextBox()
    .ContextMenu = contextMenu;
```

### With Submenus
```csharp
var contextMenu = new ContextMenu()
    .AddItem("New", () => New())
    .AddSubMenu("Export", 
        new Menu()
            .Item("As PDF", () => ExportPdf())
            .Item("As HTML", () => ExportHtml())
    )
    .AddSeparator()
    .AddItem("Properties", () => ShowProperties());
```

### Styled ContextMenu
```csharp
var contextMenu = new ContextMenu()
    .AddItem("Option 1", () => { })
    .AddItem("Option 2", () => { })
    .FontSize(14)
    .ItemPadding(new Thickness(12, 6));
```

## MenuEntry Types
- `MenuItem` - Regular clickable item
- `MenuSeparator` - Visual separator line
- SubMenu items have a chevron indicator

## Keyboard Support
- **Up/Down** - Navigate items
- **Enter** - Activate item
- **Escape** - Close menu
- **Right** - Open submenu
- **Left** - Close submenu (if open)

## Notes
- ContextMenu is focusable
- Supports nested submenus
- Automatically scrolls when items exceed MaxMenuHeight
- Keyboard shortcuts are displayed in a right-aligned column
- Items highlight on hover
