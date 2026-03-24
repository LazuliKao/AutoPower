# MewUI Components Reference

Complete reference for all MewUI components.

## Controls

| Component | File | Description |
|-----------|------|-------------|
| [Button](mewui-button.md) | Controls/Button.cs | Clickable button control |
| [Label](mewui-label.md) | Controls/Label.cs | Text display control |
| [Image](mewui-image.md) | Controls/Image.cs | Image display with scaling |
| [TextBox](mewui-textbox.md) | Controls/TextBox.cs | Single-line text input |
| [MultiLineTextBox](mewui-multilinetextbox.md) | Controls/MultiLineTextBox.cs | Multi-line text input |
| [CheckBox](mewui-checkbox.md) | Controls/CheckBox.cs | Checkbox with label |
| [RadioButton](mewui-radiobutton.md) | Controls/RadioButton.cs | Radio button with groups |
| [ToggleSwitch](mewui-toggleswitch.md) | Controls/ToggleSwitch.cs | Toggle switch control |
| [Slider](mewui-slider.md) | Controls/Slider.cs | Numeric value slider |
| [ProgressBar](mewui-progressbar.md) | Controls/ProgressBar.cs | Progress indicator |
| [NumericUpDown](mewui-numericupdown.md) | Controls/NumericUpDown.cs | Numeric input with spinners |
| [ComboBox](mewui-combobox.md) | Controls/ComboBox.cs | Drop-down selection |
| [ListBox](mewui-listbox.md) | Controls/ListBox.cs | Scrollable list |
| [TreeView](mewui-treeview.md) | Controls/TreeView.cs | Hierarchical tree view |
| [GridView](mewui-gridview.md) | Controls/GridView.cs | Data grid with columns |
| [TabControl](mewui-tabcontrol.md) | Controls/TabControl.cs | Tabbed container |
| [GroupBox](mewui-groupbox.md) | Controls/GroupBox.cs | Grouped container |
| [Expander](mewui-expander.md) | Controls/Expander.cs | Expandable content |
| [MenuBar](mewui-menubar.md) | Controls/MenuBar.cs | Application menu bar |
| [ContextMenu](mewui-contextmenu.md) | Controls/ContextMenu.cs | Right-click popup menu |
| [ToolTip](mewui-tooltip.md) | Controls/ToolTip.cs | Hover tooltip |
| [ScrollViewer](mewui-scrollviewer.md) | Controls/ScrollViewer.cs | Scrollable container |
| [Window](mewui-window.md) | Controls/Window.cs | Top-level window |

## Panels

| Component | File | Description |
|-----------|------|-------------|
| [Grid](mewui-grid.md) | Panels/Grid.cs | Rows/columns with star sizing |
| [StackPanel](mewui-stackpanel.md) | Panels/StackPanel.cs | Vertical/horizontal stack |
| [DockPanel](mewui-dockpanel.md) | Panels/DockPanel.cs | Edge docking layout |
| [UniformGrid](mewui-uniformgrid.md) | Panels/UniformGrid.cs | Equal-sized cells |
| [WrapPanel](mewui-wrappanel.md) | Panels/WrapPanel.cs | Flowing wrap layout |
| [SplitPanel](mewui-splitpanel.md) | Panels/SplitPanel.cs | Draggable splitter |
| [Canvas](mewui-canvas.md) | Panels/Canvas.cs | Absolute positioning |

## Shapes

| Component | File | Description |
|-----------|------|-------------|
| [Shape](mewui-shape.md) | Shapes/Shape.cs | Base class for shapes |
| [Ellipse](mewui-ellipse.md) | Shapes/Ellipse.cs | Ellipse/circle |
| [Rectangle](mewui-rectangle.md) | Shapes/Rectangle.cs | Rectangle with corners |
| [Line](mewui-line.md) | Shapes/Line.cs | Straight line |
| [PathShape](mewui-pathshape.md) | Shapes/PathShape.cs | Custom path geometry |

## Common Base Classes

- `Element` - Base for all visual elements
- `UIElement` - Interactive element with hit testing
- `FrameworkElement` - Element with layout support
- `Control` - Control with styling, theming
- `ContentControl` - Control with single content
- `HeaderedContentControl` - Control with header and content
- `Panel` - Container for multiple children
- `RangeBase` - Base for value controls (Slider, ProgressBar)
- `TextBase` - Base for text input controls

## Quick Start

```csharp
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

var window = new Window()
    .Title("Hello MewUI")
    .Size(520, 360)
    .Content(
        new StackPanel()
            .Spacing(8)
            .Children(
                new Label()
                    .Text("Hello, World!")
                    .FontSize(24),
                new Button()
                    .Content("Click Me")
                    .OnClick(() => Console.WriteLine("Clicked!"))
            )
    );

Application.Run(window);
```
