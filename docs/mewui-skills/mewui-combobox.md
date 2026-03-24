# ComboBox

A drop-down selection control with text header and popup list.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`DropDownBase` → `Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ItemsSource` | `ISelectableItemsView` | Empty | The items data source |
| `SelectedIndex` | `int` | `-1` | Selected item index |
| `SelectedItem` | `object?` | - | Currently selected item (read-only) |
| `SelectedText` | `string?` | - | Currently selected item text (read-only) |
| `Placeholder` | `string` | `""` | Placeholder text when no item selected |
| `ItemHeight` | `double` | `NaN` | Height of items in dropdown |
| `ItemTemplate` | `IDataTemplate?` | `null` | Item template for dropdown |
| `ZebraStriping` | `bool` | `true` | Enable alternating row colors |
| `ChangeOnWheel` | `bool` | `true` | Whether wheel changes selection |

### Inherited from Control
- `Background` - Background color
- `Foreground` - Foreground (text) color
- `BorderBrush` - Border color
- `CornerRadius` - Corner radius
- `Padding` - Inner padding
- `FontFamily` - Font family
- `FontSize` - Font size

## Events

| Event | Type | Description |
|-------|------|-------------|
| `SelectionChanged` | `Action<object?>` | Fired when selected item changes |

## Usage Examples

### Basic ComboBox
```csharp
var items = new[] { "Apple", "Banana", "Cherry" };
var comboBox = new ComboBox()
    .ItemsSource(ItemsView.Create(items))
    .Placeholder("Select a fruit...")
    .OnSelectionChanged(item => Console.WriteLine($"Selected: {item}"));
```

### With Initial Selection
```csharp
var comboBox = new ComboBox()
    .ItemsSource(ItemsView.Create(items))
    .SelectedIndex(0);
```

### Styled ComboBox
```csharp
var comboBox = new ComboBox()
    .ItemsSource(ItemsView.Create(items))
    .FontSize(14)
    .Padding(new Thickness(8, 6))
    .CornerRadius(4);
```

### Custom Item Template
```csharp
var comboBox = new ComboBox()
    .ItemsSource(ItemsView.Create(items))
    .ItemTemplate(new DelegateTemplate<string>(
        build: _ => new Label(),
        bind: (view, item, index, _) => ((Label)view).Text = item.ToUpper()
    ));
```

### Data Binding
```csharp
var selected = new ObservableValue<int>(-1);

var comboBox = new ComboBox()
    .ItemsSource(ItemsView.Create(items))
    .BindSelectedIndex(selected);
```

## Keyboard Support
- **Down Arrow** - Open dropdown and move to next item
- **Up Arrow** - Open dropdown and move to previous item
- **Enter** - Select current item and close
- **Escape** - Close dropdown
- **Tab** - Focus navigation

## Notes
- Default Maximum dropdown height is controlled by `MaxDropDownHeight`
- Popup appears below the ComboBox (or above if insufficient space)
- Supports horizontal scrolling in the header for long text
- Items selection is synchronized between ComboBox and popup ListBox
