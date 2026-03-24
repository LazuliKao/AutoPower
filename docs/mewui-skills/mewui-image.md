# Image

An image display control with scaling and alignment options.

## Namespace
`Aprillz.MewUI.Controls`

## Inheritance
`FrameworkElement` → `UIElement` → `Element`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Source` | `IImageSource?` | `null` | The image source |
| `StretchMode` | `Stretch` | `Uniform` | How the image is stretched to fill available space |
| `ImageScaleQuality` | `ImageScaleQuality` | `Default` | Image scaling quality |
| `ViewBox` | `Rect?` | `null` | The viewbox region of the source image |
| `ViewBoxUnits` | `ImageViewBoxUnits` | `Pixels` | Units for viewbox coordinates |
| `AlignmentX` | `ImageAlignmentX` | `Center` | Horizontal alignment of the image |
| `AlignmentY` | `ImageAlignmentY` | `Center` | Vertical alignment of the image |

## Methods

| Method | Description |
|--------|-------------|
| `TryPeekColor(Point positionDip, out Color color)` | Reads pixel color at the given position (local DIPs) |

## Usage Examples

### Basic Image
```csharp
var image = new Image()
    .Source(new ImageSource("path/to/image.png"));
```

### Image with Stretch
```csharp
// Fill - stretch to fill exactly (may distort)
var image = new Image()
    .Source(source)
    .StretchMode(Stretch.Fill);

// Uniform - maintain aspect ratio, fit within bounds
var image = new Image()
    .Source(source)
    .StretchMode(Stretch.Uniform);

// UniformToFill - maintain aspect ratio, fill bounds (may crop)
var image = new Image()
    .Source(source)
    .StretchMode(Stretch.UniformToFill);

// None - no stretching, original size
var image = new Image()
    .Source(source)
    .StretchMode(Stretch.None);
```

### Image with ViewBox
```csharp
// Crop a region of the image
var image = new Image()
    .Source(source)
    .ViewBox(new Rect(0, 0, 100, 100));  // First 100x100 pixels
```

### Image Alignment
```csharp
var image = new Image()
    .Source(source)
    .StretchMode(Stretch.None)
    .AlignmentX(ImageAlignmentX.Left)
    .AlignmentY(ImageAlignmentY.Top);
```

## Stretch Modes
- `Stretch.None` - Original size, no scaling
- `Stretch.Fill` - Stretch to fill bounds (may distort)
- `Stretch.Uniform` - Fit within bounds, maintain aspect ratio
- `Stretch.UniformToFill` - Fill bounds, maintain aspect ratio (may crop)

## ImageViewBoxUnits
- `ImageViewBoxUnits.Pixels` - ViewBox coordinates in pixels
- `ImageViewBoxUnits.RelativeToBoundingBox` - ViewBox coordinates relative (0-1)

## Notes
- Image caches decoded bitmaps per graphics factory
- Supports INotifyImageChanged for dynamic sources (e.g., WriteableBitmap)
- Clips to control bounds during rendering
