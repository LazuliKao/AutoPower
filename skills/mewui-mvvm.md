# MewUI MVVM Pattern Skill

## Core Philosophy

MewUI采用 **无反射、委托优先** 的MVVM模式，专为NativeAOT兼容设计。

与传统WPF的区别:
| WPF | MewUI |
|-----|-------|
| `{Binding PropertyName}` | `.BindText(vm.PropertyName)` |
| `INotifyPropertyChanged` | `ObservableValue<T>` |
| PropertyPath字符串 | 直接Lambda/委托 |
| 反射绑定 | 编译时验证 |

---

## 1. ObservableValue\<T> — 响应式值容器

### 基本用法

```csharp
using Aprillz.MewUI;

// 创建
var name = new ObservableValue<string>("默认值");
var count = new ObservableValue<int>(0);
var isEnabled = new ObservableValue<bool>(true);

// 读写
string currentName = name.Value;
name.Value = "新值";

// 变化监听
name.Changed += () => Console.WriteLine("Name已变更!");
name.Subscribe(() => Console.WriteLine("订阅方式"));
name.Unsubscribe(handler);
```

### 带约束的值 (Coerce)

```csharp
// 范围约束
var percent = new ObservableValue<double>(0.5, v => Math.Clamp(v, 0, 1));
percent.Value = 1.5;  // 自动修正为1

// 非负约束
var index = new ObservableValue<int>(0, v => Math.Max(0, v));

// 字符串修剪
var text = new ObservableValue<string>("", v => v?.Trim() ?? "");
```

---

## 2. 绑定模式

### 单向绑定 (Source → UI)

```csharp
var status = new ObservableValue<string>("就绪");

new Label()
    .BindText(status)  // status变化时Label自动更新

// 代码改变 → UI自动反映
status.Value = "加载中...";
```

### 双向绑定 (Source ↔ UI)

```csharp
var userName = new ObservableValue<string>("");

new TextBox()
    .BindText(userName)  // 双向绑定

// 代码改变 → UI更新
userName.Value = "张三";

// 用户输入 → Source自动更新
```

### 转换绑定

```csharp
var count = new ObservableValue<int>(5);
var price = new ObservableValue<decimal>(1234.56m);

// int → string
new Label()
    .BindText(count, c => $"数量: {c}")

// decimal → 格式化
new Label()
    .BindText(price, p => p.ToString("C"))  // ¥1,234.56
```

---

## 3. 控件绑定速查表

| 控件 | 方法 | 方向 | 示例 |
|------|------|------|------|
| Label | `.BindText(source)` | 单向 | `.BindText(vm.Status)` |
| Label | `.BindText(source, convert)` | 单向 | `.BindText(vm.Count, c => $"{c}")` |
| TextBox | `.BindText(source)` | **双向** | `.BindText(vm.Name)` |
| Button | `.BindContent(source)` | 单向 | `.BindContent(vm.ButtonText)` |
| CheckBox | `.BindIsChecked(source)` | **双向** | `.BindIsChecked(vm.IsEnabled)` |
| RadioButton | `.BindIsChecked(source)` | **双向** | `.BindIsChecked(vm.Option)` |
| ToggleSwitch | `.BindIsChecked(source)` | **双向** | `.BindIsChecked(vm.DarkMode)` |
| Slider | `.BindValue(source)` | **双向** | `.BindValue(vm.Volume)` |
| ProgressBar | `.BindValue(source)` | 单向 | `.BindValue(vm.Progress)` |
| NumericUpDown | `.BindValue(source)` | **双向** | `.BindValue(vm.Count)` |
| ListBox | `.BindSelectedIndex(source)` | **双向** | `.BindSelectedIndex(vm.SelectedIndex)` |
| ComboBox | `.BindSelectedIndex(source)` | **双向** | `.BindSelectedIndex(vm.SelectedIndex)` |
| UIElement | `.BindIsVisible(source)` | 单向 | `.BindIsVisible(vm.ShowPanel)` |
| UIElement | `.BindIsEnabled(source)` | 单向 | `.BindIsEnabled(vm.CanEdit)` |

---

## 4. ViewModel模式

### 标准ViewModel结构

```csharp
using Aprillz.MewUI;

class LoginViewModel
{
    // 所有可绑定属性使用ObservableValue
    public ObservableValue<string> Username { get; } = new("");
    public ObservableValue<string> Password { get; } = new("");
    public ObservableValue<bool> RememberMe { get; } = new(false);
    public ObservableValue<string> ErrorMessage { get; } = new("");
    public ObservableValue<bool> IsLoading { get; } = new(false);

    // 业务逻辑方法
    public void Login()
    {
        if (string.IsNullOrEmpty(Username.Value))
        {
            ErrorMessage.Value = "用户名必填";
            return;
        }

        IsLoading.Value = true;
        // ... 登录逻辑
    }

    public void Clear()
    {
        Username.Value = "";
        Password.Value = "";
        ErrorMessage.Value = "";
    }
}
```

### UI绑定

```csharp
var vm = new LoginViewModel();

new StackPanel()
    .Vertical()
    .Spacing(8)
    .Children(
        new TextBox()
            .Placeholder("用户名")
            .BindText(vm.Username),

        new TextBox()
            .Placeholder("密码")
            .BindText(vm.Password),

        new CheckBox()
            .Text("记住我")
            .BindIsChecked(vm.RememberMe),

        new Label()
            .Foreground(Colors.Red)
            .BindText(vm.ErrorMessage),

        new Button()
            .Content("登录")
            .OnCanClick(() => !vm.IsLoading.Value)
            .OnClick(() => vm.Login())
    )
```

---

## 5. 计算属性 (派生值)

### 手动订阅模式

```csharp
var firstName = new ObservableValue<string>("");
var lastName = new ObservableValue<string>("");

new Label()
    .Apply(label =>
    {
        void UpdateFullName()
        {
            label.Text = $"{firstName.Value} {lastName.Value}".Trim();
        }

        firstName.Changed += UpdateFullName;
        lastName.Changed += UpdateFullName;
        UpdateFullName();  // 初始化
    })
```

### 封装为扩展方法

```csharp
public static Label BindFullName(
    this Label label,
    ObservableValue<string> firstName,
    ObservableValue<string> lastName)
{
    void Update() => label.Text = $"{firstName.Value} {lastName.Value}".Trim();

    firstName.Changed += Update;
    lastName.Changed += Update;
    Update();

    return label;
}

// 使用
new Label().BindFullName(vm.FirstName, vm.LastName)
```

---

## 6. 最佳实践

### ✅ DO

```csharp
// 1. ViewModel中使用ObservableValue
class ViewModel
{
    public ObservableValue<string> Name { get; } = new("");
}

// 2. 使用Coerce保证值有效性
var age = new ObservableValue<int>(0, v => Math.Clamp(v, 0, 150));

// 3. 显示逻辑在UI层(转换绑定)
new Label().BindText(vm.Price, p => $"${p:N0}")

// 4. 区分单向/双向
// 单向: Label, ProgressBar (仅显示)
// 双向: TextBox, CheckBox, Slider (可输入)
```

### ❌ DON'T

```csharp
// 1. 不要使用普通属性(无法绑定)
class ViewModel
{
    public string Name { get; set; }  // ❌ 无法绑定
}

// 2. 不要在ViewModel中放显示逻辑
class ViewModel
{
    public ObservableValue<string> FormattedPrice { get; }  // ❌ 混合职责
}

// 3. 不要忘记Coerce(可能设置无效值)
var age = new ObservableValue<int>(0);  // ❌ 可能为负数
```

---

## 7. 完整示例: 计算器

```csharp
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

// 创建响应式状态
var expression = new ObservableValue<string>("");
var result = new ObservableValue<string>("0");
var error = new ObservableValue<string>("");

// 按钮工厂
Button KeyButton(string text, Action onClick) =>
    new Button()
        .Content(text)
        .Margin(2)
        .FontSize(20)
        .OnClick(onClick)
        .MinWidth(56)
        .MinHeight(44);

// 构建UI
var window = new Window()
    .Title("计算器")
    .Fixed(360, 520)
    .Content(
        new DockPanel()
            .Children(
                // 显示区域
                new StackPanel()
                    .DockTop()
                    .Spacing(6)
                    .Children(
                        new Label()
                            .BindText(expression, s => string.IsNullOrEmpty(s) ? " " : s)
                            .FontSize(16),

                        new Label()
                            .BindText(result)
                            .FontSize(38)
                            .Bold()
                            .TextAlignment(TextAlignment.Right),

                        new Label()
                            .BindText(error, s => string.IsNullOrEmpty(s) ? " " : s)
                            .Foreground(Colors.Red)
                    ),

                // 键盘区域
                new UniformGrid()
                    .Rows(5)
                    .Columns(4)
                    .Children(
                        KeyButton("C", () => { expression.Value = ""; result.Value = "0"; }),
                        KeyButton("7", () => expression.Value += "7"),
                        KeyButton("8", () => expression.Value += "8"),
                        KeyButton("9", () => expression.Value += "9"),
                        // ... 更多按钮
                    )
            )
    );

Application.Run(window);
```

---

## 8. 内存管理

绑定在控件Dispose时自动清理:

```csharp
var vm = new ViewModel();
var textBox = new TextBox().BindText(vm.Name);

// Window关闭时自动取消订阅
```

手动清理:
```csharp
counter.Subscribe(OnChanged);
counter.Unsubscribe(OnChanged);
```

---

## 9. ValueBinding\<T> (高级)

用于自定义控件开发:

```csharp
public class MyControl : Control
{
    private ValueBinding<string>? _textBinding;

    public void SetTextBinding(
        Func<string> get,
        Action<string>? set = null,
        Action<Action>? subscribe = null,
        Action<Action>? unsubscribe = null)
    {
        _textBinding?.Dispose();
        _textBinding = new ValueBinding<string>(
            get, set, subscribe, unsubscribe,
            onSourceChanged: () => Text = get()
        );
        Text = get();
    }

    protected override void OnDispose()
    {
        _textBinding?.Dispose();
        _textBinding = null;
    }
}
```

---

## 快速参考卡

```
┌─────────────────────────────────────────────────────────┐
│  MewUI MVVM 速查                                        │
├─────────────────────────────────────────────────────────┤
│  状态:  new ObservableValue<T>(初始值, coerce?)         │
│  读:    value = vm.Property.Value                       │
│  写:    vm.Property.Value = 新值                        │
│  监听:  vm.Property.Subscribe(handler)                  │
├─────────────────────────────────────────────────────────┤
│  单向:  .BindText(vm.Status)                            │
│  双向:  .BindText(vm.Name)   // TextBox专用             │
│  转换:  .BindText(vm.Count, c => $"{c}个")              │
├─────────────────────────────────────────────────────────┤
│  可见:  .BindIsVisible(vm.Show)                         │
│  启用:  .BindIsEnabled(vm.CanEdit)                      │
│  勾选:  .BindIsChecked(vm.IsChecked)                    │
│  滑块:  .BindValue(vm.Volume)                           │
└─────────────────────────────────────────────────────────┘
```
