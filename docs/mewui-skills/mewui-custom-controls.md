# MewUI 自定义控件与组件化封装 Skill

## 核心理念

MewUI采用 **组合优先** 的组件化策略,通过C# Fluent API构建可复用的UI组件。

---

## 1. 控件继承体系

```
Element (基础元素)
  └── UIElement (输入/焦点/可见性)
        └── FrameworkElement (布局: Measure/Arrange)
              └── Control (样式/主题/背景/边框)
                    ├── ContentControl (单内容容器)
                    │     ├── UserControl (组合式组件) ← 推荐
                    │     ├── HeaderedContentControl (Header + Content)
                    │     │     ├── GroupBox
                    │     │     └── Expander
                    │     └── Button
                    ├── ToggleBase → ToggleSwitch, CheckBox
                    └── RangeBase → Slider, ProgressBar, NumericUpDown
```

### 选择基类

| 需求 | 基类 | 适用场景 |
|------|------|----------|
| **组合已有控件** | `UserControl` | 表单、卡片、复杂布局 |
| **单内容容器** | `ContentControl` | 自定义包装器 |
| **Header + Content** | `HeaderedContentControl` | 分组、折叠面板 |
| **完全自绘** | `Control` | 图表、进度环、特殊效果 |
| **装饰层** | `Adorner` | 覆盖层、提示、粒子效果 |

---

## 2. UserControl — 组合式组件(推荐)

### 基本模式

```csharp
using Aprillz.MewUI.Controls;

public class LoginForm : UserControl
{
    // ViewModel作为响应式状态
    public ObservableValue<string> Username { get; } = new("");
    public ObservableValue<string> Password { get; } = new("");
    public ObservableValue<bool> RememberMe { get; } = new(false);
    public ObservableValue<string> Error { get; } = new("");

    // 事件
    public event Action<string, string, bool>? Submitted;

    public LoginForm()
    {
        Build(); // 必须调用Build()
    }

    // 重写OnBuild定义UI结构
    protected override Element? OnBuild() =>
        new StackPanel()
            .Vertical()
            .Spacing(8)
            .Children(
                new TextBox()
                    .Placeholder("用户名")
                    .BindText(Username),

                new TextBox()
                    .Placeholder("密码")
                    .BindText(Password),

                new CheckBox()
                    .Text("记住我")
                    .BindIsChecked(RememberMe),

                new Label()
                    .Foreground(Colors.Red)
                    .BindText(Error),

                new Button()
                    .Content("登录")
                    .OnCanClick(() => !string.IsNullOrWhiteSpace(Username.Value))
                    .OnClick(() => Submit())
            );

    private void Submit()
    {
        Error.Value = "";
        Submitted?.Invoke(Username.Value, Password.Value, RememberMe.Value);
    }
}

// 使用
var login = new LoginForm();
login.Submitted += (user, pass, remember) => { /* 处理登录 */ };

new Window()
    .Content(login)
    .Run();
```

### 带参数的组件

```csharp
public class SearchBox : UserControl
{
    public ObservableValue<string> Text { get; } = new("");
    public ObservableValue<string> Placeholder { get; } = new("搜索...");
    public event Action<string>? SearchRequested;

    public SearchBox()
    {
        Build();
    }

    protected override Element? OnBuild() =>
        new DockPanel()
            .Children(
                new Button()
                    .DockRight()
                    .Content("🔍")
                    .OnClick(() => SearchRequested?.Invoke(Text.Value)),

                new TextBox()
                    .BindText(Text)
                    .BindPlaceholder(Placeholder)  // 如果支持
                    .OnKeyDown(e =>
                    {
                        if (e.Key == Key.Enter)
                            SearchRequested?.Invoke(Text.Value);
                    })
            );
}

// 使用
var search = new SearchBox();
search.Placeholder.Value = "搜索用户...";
search.SearchRequested += query => PerformSearch(query);
```

---

## 3. 继承 ContentControl

```csharp
public class IconButton : ContentControl
{
    private bool _isPressed;

    public event Action? Click;

    public IconButton()
    {
        Padding = new Thickness(8, 4, 8, 4);
        CornerRadius = 4;
    }

    protected override void OnRender(IGraphicsContext context)
    {
        var bounds = GetSnappedBorderBounds(Bounds);
        var state = GetVisualState(isPressed: _isPressed);
        var bg = PickButtonBackground(state);
        var border = PickAccentBorder(Theme, BorderBrush, state);

        DrawBackgroundAndBorder(context, bounds, bg, border, CornerRadius);

        // 绘制自定义内容(图标等)
        if (Content != null)
        {
            Content.Render(context);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (!IsEffectivelyEnabled || e.Button != MouseButton.Left)
            return;

        _isPressed = true;
        Focus();
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_isPressed && e.Button == MouseButton.Left)
        {
            _isPressed = false;
            Click?.Invoke();
            InvalidateVisual();
            e.Handled = true;
        }
    }
}
```

---

## 4. 完全自定义控件 (继承Control)

### 关键方法

```csharp
public class CircularProgress : Control
{
    private double _value;
    private double _maximum = 100;

    public double Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = Math.Clamp(value, 0, _maximum);
                InvalidateVisual(); // 值改变 → 重绘
            }
        }
    }

    public double Maximum
    {
        get => _maximum;
        set
        {
            if (_maximum != value)
            {
                _maximum = Math.Max(1, value);
                _value = Math.Min(_value, _maximum);
                InvalidateVisual();
            }
        }
    }

    // 1. 测量期望大小
    protected override Size MeasureContent(Size available)
    {
        // 返回自然大小,不要超出available
        double size = Math.Min(
            double.IsInfinity(available.Width) ? 100 : available.Width,
            double.IsInfinity(available.Height) ? 100 : available.Height
        );
        return new Size(size, size);
    }

    // 2. 排列子元素(如果有的话)
    // protected override void ArrangeContent(Rect bounds) { }

    // 3. 渲染
    protected override void OnRender(IGraphicsContext context)
    {
        var bounds = GetSnappedBorderBounds(Bounds);
        var center = bounds.Center;
        double radius = Math.Min(bounds.Width, bounds.Height) / 2 - 4;

        // 背景圆环
        context.DrawEllipse(
            new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2),
            Theme.Palette.ControlBorder,
            4);

        // 进度弧
        double angle = (_value / _maximum) * 360;
        // ... 绘制弧形逻辑

        // 文本
        var font = GetFont();
        var text = $"{_value / _maximum:P0}";
        context.DrawText(text, bounds, font, Foreground,
            TextAlignment.Center, TextAlignment.Center, TextWrapping.NoWrap);
    }

    // 4. 主题变化处理
    protected override void OnThemeChanged(Theme oldTheme, Theme newTheme)
    {
        base.OnThemeChanged(oldTheme, newTheme);
        InvalidateVisual();
    }
}
```

### 状态驱动的视觉变化

```csharp
public class StatefulButton : Control
{
    private bool _isHovered;
    private bool _isPressed;

    public event Action? Click;

    protected override void OnRender(IGraphicsContext context)
    {
        var bounds = GetSnappedBorderBounds(Bounds);
        double radius = Theme.Metrics.ControlCornerRadius;

        // 使用VisualState系统
        var state = GetVisualState(isPressed: _isPressed);
        var bg = PickButtonBackground(state);
        var border = PickAccentBorder(Theme, BorderBrush, state, hoverMix: 0.6);

        DrawBackgroundAndBorder(context, bounds, bg, border, radius);
    }

    protected override void OnMouseEnter()
    {
        base.OnMouseEnter();
        _isHovered = true;
        InvalidateVisual(); // 仅重绘,不重新布局
    }

    protected override void OnMouseLeave()
    {
        base.OnMouseLeave();
        _isHovered = false;
        InvalidateVisual();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (!IsEffectivelyEnabled || e.Button != MouseButton.Left)
            return;

        _isPressed = true;
        Focus();
        var root = FindVisualRoot();
        if (root is Window w) w.CaptureMouse(this);
        InvalidateVisual();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_isPressed)
        {
            _isPressed = false;
            var root = FindVisualRoot();
            if (root is Window w) w.ReleaseMouseCapture();
            Click?.Invoke();
            InvalidateVisual();
        }
    }
}
```

---

## 5. HeaderedContentControl — Header+Content模式

```csharp
public class CollapsibleSection : HeaderedContentControl
{
    public static readonly MewProperty<bool> IsExpandedProperty =
        MewProperty<bool>.Register<CollapsibleSection>(
            nameof(IsExpanded), true,
            MewPropertyOptions.AffectsLayout | MewPropertyOptions.BindsTwoWayByDefault,
            (self, _, _) => self.InvalidateMeasure());

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public CollapsibleSection()
    {
        HeaderSpacing = 4;
    }

    protected override Size MeasureContent(Size availableSize)
    {
        var inner = availableSize.Deflate(Padding);
        double desiredH = 0, desiredW = 0;

        // 测量Header
        if (Header != null)
        {
            Header.Measure(new Size(inner.Width, double.PositiveInfinity));
            desiredH += Header.DesiredSize.Height;
            desiredW = Math.Max(desiredW, Header.DesiredSize.Width);
        }

        // 测量Content(如果展开)
        if (IsExpanded && Content != null)
        {
            double spacing = Math.Max(0, HeaderSpacing);
            Content.Measure(new Size(inner.Width, double.PositiveInfinity));
            desiredH += spacing + Content.DesiredSize.Height;
            desiredW = Math.Max(desiredW, Content.DesiredSize.Width);
        }

        return new Size(desiredW, desiredH).Inflate(Padding);
    }

    protected override void ArrangeContent(Rect bounds)
    {
        var inner = bounds.Deflate(Padding);
        double y = inner.Y;

        if (Header != null)
        {
            Header.Arrange(new Rect(inner.X, y, inner.Width, Header.DesiredSize.Height));
            y += Header.DesiredSize.Height + Math.Max(0, HeaderSpacing);
        }

        if (IsExpanded && Content != null)
        {
            Content.Arrange(new Rect(inner.X, y, inner.Width, Math.Max(0, inner.Bottom - y)));
        }
    }

    protected override void RenderSubtree(IGraphicsContext context)
    {
        Header?.Render(context);
        if (IsExpanded)
            Content?.Render(context);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (!IsEffectivelyEnabled || e.Button != MouseButton.Left)
            return;

        // 点击Header区域切换展开
        if (Header?.Bounds.Contains(e.Position) == true)
        {
            IsExpanded = !IsExpanded;
            e.Handled = true;
        }
    }
}
```

---

## 6. Adorner — 装饰覆盖层

```csharp
public class TooltipAdorner : Adorner
{
    private string _text = "";
    private Rect _targetBounds;

    public TooltipAdorner(UIElement adornedElement) : base(adornedElement)
    {
        IsHitTestVisible = false; // 不拦截鼠标事件
    }

    public void Show(string text, Rect bounds)
    {
        _text = text;
        _targetBounds = bounds;
        InvalidateVisual();
    }

    protected override void OnRender(IGraphicsContext context)
    {
        var font = GetFont();
        var bg = Theme.Palette.ControlBackground;
        var border = Theme.Palette.ControlBorder;

        // 绘制tooltip背景
        var tooltipBounds = new Rect(
            _targetBounds.X, _targetBounds.Bottom + 4,
            200, 30);

        DrawBackgroundAndBorder(context, tooltipBounds, bg, border, 4);

        // 绘制文本
        context.DrawText(_text, tooltipBounds.Deflate(new Thickness(8, 4)),
            font, Foreground, TextAlignment.Left, TextAlignment.Center,
            TextWrapping.NoWrap);
    }
}
```

---

## 7. MewProperty系统

### 声明依赖属性

```csharp
public class MyControl : Control
{
    // 声明属性
    public static readonly MewProperty<string> TitleProperty =
        MewProperty<string>.Register<MyControl>(
            nameof(Title),
            defaultValue: "",
            options: MewPropertyOptions.AffectsLayout,
            coerce: null,
            changed: (self, oldVal, newVal) => self.OnTitleChanged(newVal));

    // CLR包装
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    private void OnTitleChanged(string newTitle)
    {
        // 响应属性变化
        InvalidateMeasure();
        InvalidateVisual();
    }
}
```

### MewPropertyOptions

| 选项 | 效果 |
|------|------|
| `AffectsLayout` | 属性改变时触发Measure/Arrange |
| `AffectsRender` | 属性改变时仅重绘 |
| `Inherits` | 向下继承(如FontFamily) |
| `BindsTwoWayByDefault` | 默认双向绑定 |

---

## 8. 数据模板 (ItemsControl)

### DelegateTemplate模式

```csharp
// 定义数据模型
public record Person(string Name, int Age);

// 创建模板
var personTemplate = new DelegateTemplate<Person>(
    build: ctx =>
    {
        // Build只调用一次,创建视图结构
        var panel = new StackPanel().Horizontal().Spacing(8);
        var nameLabel = new TextBlock().Register(ctx, "Name");
        var ageLabel = new TextBlock().Register(ctx, "Age");
        panel.Children(nameLabel, ageLabel);
        return panel;
    },
    bind: (view, item, index, ctx) =>
    {
        // Bind每次item显示时调用
        ctx.Get<TextBlock>("Name").Text = item.Name;
        ctx.Get<TextBlock>("Age").Text = item.Age.ToString();
    }
);

// 使用
new ListBox()
    .Items(people)
    .ItemTemplate(personTemplate);
```

### 简化版(不需要TemplateContext)

```csharp
new ListBox()
    .ItemTemplate(
        build: _ => new TextBlock(),
        bind: (TextBlock view, Person item) => view.Text = item.Name)
    .Items(people);
```

---

## 9. 组件化最佳实践

### ✅ DO

```csharp
// 1. 使用UserControl组合已有控件
public class AddressForm : UserControl { ... }

// 2. 使用ObservableValue暴露响应式状态
public ObservableValue<string> Value { get; } = new("");

// 3. 使用事件暴露交互
public event Action<string>? Submitted;

// 4. 合理选择Invalidation
InvalidateMeasure();  // 大小改变
InvalidateVisual();   // 仅外观改变

// 5. 使用Theme.Palette保持主题一致
var bg = Theme.Palette.ControlBackground;
```

### ❌ DON'T

```csharp
// 1. 不要在Measure中做像素计算
protected override Size MeasureContent(Size available)
{
    var pixels = available * GetDpi() / 96.0; // ❌ DIP only!
}

// 2. 不要在OnRender中重新测量
protected override void OnRender(IGraphicsContext context)
{
    Measure(...); // ❌ 使用已有的Bounds!
}

// 3. 不要忘记调用Build()
public MyUserControl()
{
    // ❌ 忘记Build()导致OnBuild不执行
}

// 4. 不要忽略IsEffectivelyEnabled
protected override void OnMouseDown(MouseEventArgs e)
{
    if (!IsEnabled) // ❌ 应该用IsEffectivelyEnabled
}
```

---

## 10. 完整示例: 数据卡片组件

```csharp
public class DataCard : UserControl
{
    public ObservableValue<string> Title { get; } = new("");
    public ObservableValue<string> Value { get; } = new("");
    public ObservableValue<string> Unit { get; } = new("");
    public ObservableValue<Color> AccentColor { get; } = new(Colors.Blue);

    public event Action? Clicked;

    public DataCard()
    {
        Build();
    }

    protected override Element? OnBuild() =>
        new Border()
            .CornerRadius(8)
            .Padding(16)
            .WithTheme((t, c) => c.Background(t.Palette.ControlBackground))
            .Child(
                new DockPanel()
                    .Children(
                        // 顶部指示条
                        new Border()
                            .DockTop()
                            .Height(4)
                            .CornerRadius(2)
                            .BindBackground(AccentColor),

                        // 内容区
                        new StackPanel()
                            .Vertical()
                            .Spacing(4)
                            .Children(
                                new Label()
                                    .BindText(Title)
                                    .FontSize(12)
                                    .WithTheme((t, c) => c.Foreground(t.Palette.DisabledText)),

                                new StackPanel()
                                    .Horizontal()
                                    .Spacing(4)
                                    .Children(
                                        new Label()
                                            .BindText(Value)
                                            .FontSize(24)
                                            .Bold(),

                                        new Label()
                                            .BindText(Unit)
                                            .FontSize(14)
                                            .CenterVertical()
                                    )
                            )
                    )
            )
            .OnPointerPressed(_ => Clicked?.Invoke());
}

// 使用
var card = new DataCard();
card.Title.Value = "活跃用户";
card.Value.Value = "12,345";
card.Unit.Value = "人";
card.AccentColor.Value = Colors.Green;
card.Clicked += () => ShowDetails();
```

---

## 快速参考

```
┌─────────────────────────────────────────────────────────────┐
│  MewUI 自定义控件速查                                       │
├─────────────────────────────────────────────────────────────┤
│  组合:    class MyForm : UserControl { OnBuild() }          │
│  容器:    class MyBox : ContentControl { Content }          │
│  分组:    class MyGroup : HeaderedContentControl            │
│  自绘:    class MyShape : Control { OnRender() }            │
│  装饰:    class MyOverlay : Adorner { OnRender() }          │
├─────────────────────────────────────────────────────────────┤
│  测量:    MeasureContent(Size) → Size                       │
│  排列:    ArrangeContent(Rect)                              │
│  渲染:    OnRender(IGraphicsContext)                        │
│  状态:    OnMouseEnter/Leave/Down/Up, OnKeyDown/Up          │
├─────────────────────────────────────────────────────────────┤
│  失效:    InvalidateMeasure()  // 大小改变                  │
│           InvalidateVisual()   // 仅外观                    │
├─────────────────────────────────────────────────────────────┤
│  主题:    Theme.Palette.* (颜色)                            │
│           Theme.Metrics.* (尺寸)                            │
│           PickButtonBackground(state)                       │
│           DrawBackgroundAndBorder(ctx, bounds, bg, border)  │
└─────────────────────────────────────────────────────────────┘
```
