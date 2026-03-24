# AGENTS.md — AutoPower Development Guide for AI Agents

## ⚙️ Build & Test Commands

```bash
dotnet build                                    # Debug build
dotnet build -c Release                         # Release (NativeAOT-compatible)
dotnet test                                     # Run all tests
dotnet test --filter "StrategyEvaluatorTests"   # Single test class
dotnet test --filter "StrategyEvaluatorTests.NoRules_ReturnsNull"  # Single test method
dotnet publish src/AutoPower -c Release -r win-x64 -p:PublishAot=true  # NativeAOT publish
```

---

## 🏗️ Architecture Quirks

### NativeAOT Constraints (CRITICAL)
- ❌ **NO reflection** — breaks AOT
- ❌ **NO `DllImport`** — use `LibraryImport` ONLY
- ❌ **NO Newtonsoft.Json** — use System.Text.Json SourceGen
- ✅ **record/readonly struct** — AOT-friendly immutables

### Namespace Organization
```
AutoPower.Core/           # Domain logic (NO UI, NO P/Invoke)
├── Core/                # AppController, Models
├── Detection/           # IdleDetector, MonitorStateDetector
├── Power/               # PowerPlanManager (Win32 wrapper)
├── Strategy/            # StrategyEvaluator (rule engine)
└── Infrastructure/      # ConfigService, LogService, Win32/

AutoPower/               # UI + Entry point (NO domain logic)
├── Program.cs           # Manual DI (no Microsoft.Extensions.*)
└── UI/                  # TrayIcon, SettingsWindow (MewUI)

AutoPower.Tests/         # Mirror source structure
```

### Namespace Convention
- **File-scoped namespaces**: `namespace AutoPower.Core.Core;`
- Models: `AutoPower.Core.Core.Models.*`
- Services: `AutoPower.Core.Infrastructure.*` or `AutoPower.Core.{Domain}.*`
- Win32 P/Invoke: `AutoPower.Core.Infrastructure.Win32.*`

### Static Services (Global State)
```csharp
ConfigService.Load() / ConfigService.Save(config)
LoggerService.Info("message")
PowerPlanManager.EnumeratePlans()
```
❌ Never instantiate directly — they have internal `Initialize()` methods.

---

## 💻 Code Style Rules

```csharp
namespace AutoPower.Core.Core.Models;  // File-scoped namespace
using System.Runtime.Versioning;       // NO global usings
#nullable enable  // Enabled globally

// ✅ Public record for immutable data
public sealed record AppConfig
{
    public int SchemaVersion { get; init; } = 1;
    public List<StrategyRule> Rules { get; init; } = new();
}

// ✅ Internal enum / static class for services
internal enum DetectionMode { KeyboardMouse, MonitorSleep, Both }
internal static class LoggerService { ... }

// Null safety
string? optionalName = null;        // ✅ Explicit nullable
string requiredName = "...";        // ✅ Non-nullable initialized
Guid planGuid = default;            // ✅ OK for structs

// Error handling: Log + return false pattern (no exceptions for flow control)
public bool TrySetActiveScheme(Guid planGuid, out string? errorMessage)
{
    if (!PlanExists(planGuid))
    {
        errorMessage = $"Plan {planGuid} not found";
        LoggerService.Error(errorMessage);
        return false;
    }
    errorMessage = null;
    return true;
}
// ❌ NO empty catch blocks, NO swallowing exceptions
```

---

## 🔌 Win32 P/Invoke Patterns

```csharp
using System.Runtime.InteropServices;

namespace AutoPower.Core.Infrastructure.Win32;

internal static partial class User32
{
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    internal struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }
}
```

---

## 🧪 Testing Patterns

```csharp
using Xunit;

namespace AutoPower.Tests.Strategy;

public class StrategyEvaluatorTests
{
    private static readonly DateTime TestMonday = new(2025, 6, 2, 12, 0, 0);
    private static StrategyDecisionNode Leaf(Guid planGuid) => new() { PlanGuid = planGuid };

    [Fact]
    public void NoRules_ReturnsNull() { ... }

    [Theory]
    [InlineData(DayOfWeek.Monday, true)]
    public void DayTypeFiltering(DayOfWeek dow, bool shouldMatch) { ... }
}
```
- Use ✅ **Moq** for Win32 API mocks
- ❌ NO real file I/O in tests

---

## 🚨 Forbidden Patterns

- ❌ `DllImport` (use `LibraryImport`)
- ❌ `Newtonsoft.Json` (use System.Text.Json SourceGen)
- ❌ `ServiceCollection`/`IServiceProvider` (manual DI only)
- ❌ Reflection anywhere (read-only in NativeAOT)
- ❌ `async void` (use Task)
- ❌ `as any` / `!` type suppression (use proper types)
- ❌ Empty catch blocks

---

## 🎨 MewUI Skills (UI Components)

**CRITICAL**: When creating/modifying UI controls, read the relevant skill doc in `docs/mewui-skills/`:

| Component | Skill File | Component | Skill File |
|-----------|------------|-----------|------------|
| Button | `mewui-button.md` | Grid | `mewui-grid.md` |
| TextBox | `mewui-textbox.md` | StackPanel | `mewui-stackpanel.md` |
| Label | `mewui-label.md` | DockPanel | `mewui-dockpanel.md` |
| CheckBox | `mewui-checkbox.md` | ComboBox | `mewui-combobox.md` |
| Slider | `mewui-slider.md` | ListBox | `mewui-listbox.md` |
| **MVVM** | `mewui-mvvm.md` | Window | `mewui-window.md` |

### MVVM Pattern (NativeAOT-Compatible, Reflection-Free)

```csharp
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

// ViewModel: ObservableValue for reactive state
public ObservableValue<string> Name { get; } = new("");

// One-way binding
new Label().BindText(vm.Status);
new Label().BindText(vm.Count, c => $"{c} items");

// Two-way binding (TextBox auto-updates source)
new TextBox().BindText(vm.Name);

// Common patterns
new Button().Content("Save").CornerRadius(6).OnClick(() => Save());
new TextBox().Placeholder("Enter name...").BindText(vm.Name);
new Grid().Spacing(8).ColumnDefinitions(
    new ColumnDefinition() { Width = GridLength.Auto },
    new ColumnDefinition() { Width = GridLength.Star }
).Children(new Label().Text("Label:").SetColumn(0), new TextBox().SetColumn(1));
new StackPanel().Vertical().Spacing(8).Children(...);
```

---

## 📋 Quick Checklist Before PR

- [ ] `dotnet test` passes
- [ ] `dotnet build -c Release` has 0 warnings
- [ ] No `as any`, `#pragma disable`, or `!` hacks
- [ ] All exceptions logged (not silently caught)
- [ ] `record` for data models, `static class` for services
- [ ] File-scoped namespace in all files
- [ ] NativeAOT publish completes without warnings
- [ ] UI code follows MewUI skill docs in `docs/mewui-skills/`
