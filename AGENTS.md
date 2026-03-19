# AGENTS.md — AutoPower Development Guide for AI Agents

## ⚙️ Build & Test Commands

### Core Commands
```bash
# Build
dotnet build                                    # Debug build
dotnet build -c Release                         # Release build (NativeAOT-compatible)

# Test
dotnet test                                     # Run all 32 tests
dotnet test --filter "StrategyEvaluatorTests"   # Run single test class
dotnet test --filter "StrategyEvaluatorTests.NoRules_ReturnsNull"  # Single test method
dotnet test -c Release                          # Release mode tests

# Publish (NativeAOT)
dotnet publish src/AutoPower -c Release -r win-x64 -p:PublishAot=true
# Output: src/AutoPower/bin/Release/net10.0/win-x64/publish/AutoPower.exe (~5.6 MB)
```

---

## 🏗️ Architecture Quirks

### 1. **NativeAOT Constraints** (CRITICAL)
- ❌ **NO reflection** — breaks AOT
- ❌ **NO dynamic code** — use `[DynamicDependency]` if unavoidable
- ❌ **NO `DllImport`** — use `LibraryImport` ONLY
- ✅ **record/readonly struct** — AOT-friendly immutables
- ✅ **System.Text.Json SourceGen** — no reflection serialization

### 2. **Namespace Organization**
```
AutoPower.Core/           # Domain logic (NO UI, NO P/Invoke)
├── Core/                # AppController (state machine), Models
├── Detection/           # IdleDetector, MonitorStateDetector
├── Power/              # PowerPlanManager (Win32 API wrapper)
├── Strategy/           # StrategyEvaluator (rule engine)
└── Infrastructure/     # ConfigService, LogService, Win32/ P/Invoke

AutoPower/              # UI + Entry point (NO domain logic here)
├── Program.cs          # Manual DI (no Microsoft.Extensions.*)
├── Infrastructure/     # AdminElevationManager, StartupManager
└── UI/                # TrayIcon, SettingsWindow (MewUI)

AutoPower.Tests/        # Mirror source structure
```

### 3. **Namespace Convention** (MUST FOLLOW)
- Use **file-scoped namespaces**: `namespace AutoPower.Core.Core;`
- Models are always in `AutoPower.Core.Core.Models.*`
- Services are in `AutoPower.Core.Infrastructure.*` or `AutoPower.Core.{Domain}.*`
- Win32 P/Invoke is ALWAYS in `AutoPower.Core.Infrastructure.Win32.*`

### 4. **Static Services** (Global State)
```csharp
// These are static facades in AutoPower.Core.Infrastructure
ConfigService.Load()
ConfigService.Save(config)
LoggerService.Info("message")
PowerPlanManager.EnumeratePlans()
```
❌ Never instantiate these directly — they have internal `Initialize()` methods.

---

## 💻 Code Style Rules

### Imports & Usings
```csharp
// ✅ File-scoped namespace (C# 11+)
namespace AutoPower.Core.Core.Models;

// ✅ NO global usings
using System.Runtime.Versioning;  // Put at top
using AutoPower.Core.Infrastructure;

// ✅ Alias long types (especially Win32 P/Invoke)
using Kernel32 = AutoPower.Core.Infrastructure.Win32.Kernel32;
```

### Type Declarations
```csharp
// ✅ Public record for immutable data (all .Init properties)
public sealed record AppConfig
{
    public int SchemaVersion { get; init; } = 1;
    public List<StrategyRule> Rules { get; init; } = new();
}

// ✅ Internal enum for internal use
internal enum DetectionMode { KeyboardMouse, MonitorSleep, Both }

// ✅ Static class for services
internal static class LoggerService { ... }

// ❌ NO class for data (use record)
// ❌ NO nullable fields without ? suffix
```

### Null Safety & Naming
```csharp
#nullable enable  // Enabled globally

// ✅ Clear intent
string? optionalName = null;
string requiredName = "...";
Guid planGuid = default;  // OK only for Guid (struct)
Guid? maybePlanGuid = null;

// ✅ Factory method returns nullable explicitly
public Guid? GetActivePlan() { ... }

// ❌ NO implicit null allowed
public string Name { get; }  // MUST be initialized in constructor
```

### Error Handling
```csharp
// ✅ Log + return false pattern (no exceptions)
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

// ✅ All exception handlers log before rethrowing
catch (Exception ex)
{
    LoggerService.Error("Operation failed", ex);
    throw;  // ✅ or return false
}

// ❌ NO empty catch blocks
// ❌ NO swallowing exceptions silently
```

### Properties & Fields
```csharp
// ✅ Public properties with private backing field
private string _name = "default";
public string Name => _name;

// ✅ Init-only properties in records
public Guid Id { get; init; }

// ✅ Private fields for state
private ManualResetEventSlim _exitSignal = new();

// ❌ NO auto-properties for complex state
// ❌ NO public fields
```

---

## 🔌 Win32 P/Invoke Patterns

### LibraryImport (NativeAOT-compatible)
```csharp
using System.Runtime.InteropServices;

namespace AutoPower.Core.Infrastructure.Win32;

internal static partial class User32
{
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    internal struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }
}

// ✅ Use in managed code
var info = new User32.LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<User32.LASTINPUTINFO>() };
User32.GetLastInputInfo(ref info);
```

### Common GUIDs (Pre-defined in Win32/Kernel32.cs)
```csharp
internal static class Win32Constants
{
    // Display state: 02731015-4510-4526-99E6-E5A17EBD1AEA
    public static readonly Guid GUID_CONSOLE_DISPLAY_STATE = new("02731015-4510-4526-99E6-E5A17EBD1AEA");
}
```

---

## 🧪 Testing Patterns

### Test File Structure
```csharp
using Xunit;
using AutoPower.Core.Strategy;

namespace AutoPower.Tests.Strategy;

public class StrategyEvaluatorTests
{
    // ✅ Helper factory method
    private static StrategyRule CreateRule(
        Guid? id = null,
        DayType dayType = DayType.All,
        bool isEnabled = true
    ) => new() { Id = id ?? Guid.NewGuid(), DayType = dayType, IsEnabled = isEnabled };

    // ✅ [Fact] for deterministic tests
    [Fact]
    public void NoRules_ReturnsNull()
    {
        var rules = new List<StrategyRule>();
        var result = StrategyEvaluator.Evaluate(rules, DateTime.Now);
        Assert.Null(result);
    }

    // ✅ [Theory] + [InlineData] for parameterized tests
    [Theory]
    [InlineData(DayOfWeek.Monday, true)]
    [InlineData(DayOfWeek.Saturday, false)]
    public void DayTypeFiltering(DayOfWeek dow, bool shouldMatch) { ... }
}
```

### Mock Patterns
- Use ✅ **Moq** for Win32 API mocks
- Use ✅ **ManualResetEventSlim** for async event testing
- Use ✅ Static test constants (e.g., `TestMonday`, `TestSaturday`)
- ❌ NO real file I/O in tests

---

## 🚨 Don't Forget These

### BeforeSaving/Committing
1. ✅ **Run tests**: `dotnet test` (32 tests must pass)
2. ✅ **NativeAOT publish**: `dotnet publish ... -p:PublishAot=true` (zero trim warnings)
3. ✅ **No using AdminElevationManager outside Program.cs**
4. ✅ **Log errors, never silently catch**

### Forbidden Patterns
- ❌ `DllImport` (use `LibraryImport`)
- ❌ `Newtonsoft.Json` (use System.Text.Json SourceGen)
- ❌ `ServiceCollection`/`IServiceProvider` (manual DI only)
- ❌ Reflection anywhere (read-only in NativeAOT)
- ❌ `async void` (use Task)
- ❌ `as any` / `!` type suppression (use proper types)

### When Adding New Files
1. Create **both** `.cs` (implementation) and `Tests.cs` (unit tests)
2. Match namespace to folder structure
3. Use `record` for data, `static class` for services
4. If Win32: add to `AutoPower.Core.Infrastructure.Win32.*`
5. Add `[SupportedOSPlatform("windows")]` if using Win32

---

## 📋 Quick Checklist Before PR

- [ ] `dotnet test --filter "YourTestName"` passes
- [ ] `dotnet build -c Release` has 0 warnings
- [ ] No `as any`, `#pragma disable`, or `!` hacks
- [ ] All exceptions are logged (not silently caught)
- [ ] No reflection or dynamic code outside tests
- [ ] Using `LibraryImport` for all P/Invoke
- [ ] `record` for data models, `static class` for services
- [ ] File-scoped namespace in all files
- [ ] NativeAOT publish completes without warnings
