# Window

Represents a top-level window.

## Namespace
`Aprillz.MewUI`

## Inheritance
`ContentControl` → `Control` → `FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Title` | `string` | `"Window"` | Window title |
| `Icon` | `IconSource?` | `null` | Window icon |
| `Content` | `Element?` | `null` | Window content |
| `WindowSize` | `WindowSize` | Resizable(800, 600) | Window size configuration |
| `StartupLocation` | `WindowStartupLocation` | `CenterScreen` | Initial placement |
| `Opacity` | `double` | `1.0` | Window opacity (0-1) |
| `AllowsTransparency` | `bool` | `false` | Per-pixel transparency support |
| `UseLayoutRounding` | `bool` | `true` | Layout rounding enabled |
| `Width` | `double` | - | Client width in DIPs (read-only) |
| `Height` | `double` | - | Client height in DIPs (read-only) |
| `ClientSize` | `Size` | - | Client size in DIPs (read-only) |
| `Dpi` | `uint` | `96` | Current DPI (read-only) |
| `IsActive` | `bool` | - | Whether window is active (read-only) |
| `Handle` | `nint` | `0` | Platform window handle (read-only) |

## Events

| Event | Type | Description |
|-------|------|-------------|
| `Loaded` | `Action?` | Window is loaded and ready |
| `Closed` | `Action?` | Window is closed |
| `Activated` | `Action?` | Window is activated |
| `Deactivated` | `Action?` | Window is deactivated |
| `ClientSizeChanged` | `Action<Size>?` | Client size changes |
| `DpiChanged` | `Action<uint, uint>?` | DPI changes |
| `ThemeChanged` | `Action<Theme, Theme>?` | Theme changes |
| `FirstFrameRendered` | `Action?` | First frame is rendered |
| `FrameRendered` | `Action?` | After each frame rendered |

## Methods

| Method | Description |
|--------|-------------|
| `Show(Window? owner = null)` | Shows the window |
| `Hide()` | Hides the window |
| `Close()` | Closes the window |
| `Activate()` | Activates the window |
| `ShowDialogAsync(Window? owner = null)` | Shows as modal dialog |
| `CenterOnOwner()` | Centers on owner window |
| `MoveTo(double leftDip, double topDip)` | Moves window |
| `ClientToScreen(Point clientPointDip)` | Converts client to screen coords |
| `ScreenToClient(Point screenPointPx)` | Converts screen to client coords |
| `CaptureMouse(UIElement element)` | Captures mouse input |
| `ReleaseMouseCapture()` | Releases mouse capture |
| `InvalidateVisual()` | Requests redraw |
| `InvalidateMeasure()` | Invalidates layout |
| `ShowToast(string text)` | Shows toast notification |

## Usage Examples

### Basic Window
```csharp
var window = new Window()
    .Title("Hello MewUI")
    .Size(520, 360)
    .Content(
        new Label()
            .Text("Hello, World!")
    );

Application.Run(window);
```

### Window with Content
```csharp
var window = new Window()
    .Title("My Application")
    .Size(800, 600)
    .Padding(12)
    .Content(
        new StackPanel()
            .Spacing(8)
            .Children(
                new Label()
                    .Text("Welcome")
                    .FontSize(24)
                    .Bold(),
                new Button()
                    .Content("Quit")
                    .OnClick(() => Application.Quit())
            )
    );
```

### Modal Dialog
```csharp
var dialog = new Window()
    .Title("Settings")
    .Size(400, 300)
    .StartupLocation(WindowStartupLocation.CenterOwner)
    .Content(settingsContent);

await dialog.ShowDialogAsync(mainWindow);
```

### Fixed Size Window
```csharp
var window = new Window()
    .Title("Fixed")
    .WindowSize(WindowSize.Fixed(300, 200));
```

### Transparent Window
```csharp
var window = new Window()
    .AllowsTransparency(true)
    .Background(Colors.Transparent)
    .Content(overlayContent);
```

## WindowSize Options
- `WindowSize.Resizable(width, height)` - Resizable with size
- `WindowSize.Fixed(width, height)` - Fixed size, not resizable
- `WindowSize.FitContentSize()` - Size to content
- `WindowSize.ResizableWithConstraints(...)` - With min/max constraints

## WindowStartupLocation Values
- `WindowStartupLocation.CenterScreen` - Center on screen
- `WindowStartupLocation.CenterOwner` - Center on owner
- `WindowStartupLocation.Manual` - Manual positioning

## Notes
- Window is the visual root for all elements
- Layout is performed via `PerformLayout()`
- Rendering happens via `RenderFrame()`
- DPI awareness is built-in
- Theme changes are broadcast to all child elements
