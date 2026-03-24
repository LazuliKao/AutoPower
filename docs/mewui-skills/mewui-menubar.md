# MenuBar

A horizontal menu bar control for application menus.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Items` | `IList<MenuItem>` | Empty | Menu items collection |
| `Spacing` | `double` | `2` | Spacing between menu items |

### Inherited from Control
- `Background` - Background color
- `Foreground` - Foreground (text) color
- `FontFamily` - Font family
- `FontSize` - Font size
- `Padding` - Inner padding

## Methods

| Method | Description |
|--------|-------------|
| `Add(MenuItem item)` | Adds a menu item |
| `SetItems(params MenuItem[] items)` | Sets menu items collection |

## Usage Examples

### Basic MenuBar
```csharp
var menuBar = new MenuBar()
    .SetItems(
        new MenuItem("File")
            .SubMenu(
                new Menu()
                    .Item("New", () => NewFile())
                    .Item("Open", () => OpenFile())
                    .Separator()
                    .Item("Exit", () => Application.Quit())
            ),
        new MenuItem("Edit")
            .SubMenu(
                new Menu()
                    .Item("Undo", () => Undo(), shortcutText: "Ctrl+Z")
                    .Item("Redo", () => Redo(), shortcutText: "Ctrl+Y")
            ),
        new MenuItem("Help")
            .SubMenu(
                new Menu()
                    .Item("About", () => ShowAbout())
            )
    );
```

### Window with MenuBar
```csharp
var window = new Window()
    .Title("My App")
    .Content(
        new DockPanel()
            .Children(
                menuBar.DockTop(),
                mainContent.DockFill()
            )
    );
```

### MenuItem Properties

| Property | Type | Description |
|----------|------|-------------|
| `Text` | `string` | Menu item text |
| `SubMenu` | `Menu?` | Submenu (if any) |
| `Click` | `Action?` | Click handler |
| `IsEnabled` | `bool` | Whether item is enabled |
| `ShortcutText` | `string?` | Keyboard shortcut display text |

## Notes
- MenuBar is focusable
- Supports submenu navigation
- Items highlight on hover
- Bottom separator line is drawn automatically
