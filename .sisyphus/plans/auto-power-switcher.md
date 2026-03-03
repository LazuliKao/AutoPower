# AutoPower — 自动电源模式切换器

## TL;DR

> **Quick Summary**: 构建一个 Windows 系统托盘应用，通过键鼠空闲检测、显示器熄屏事件和可优先级叠加的时间段策略，自动切换 Windows 电源计划（如高性能/省电），用户离开时切换，回来时恢复原始计划。
>
> **Deliverables**:
> - `AutoPower.exe` — 单一自包含 NativeAOT 可执行文件（~3.5MB）
> - 系统托盘图标（Win32 Shell_NotifyIcon）+ 右键菜单
> - MewUI 设置窗口（检测方式、电源计划、策略管理、开机自启）
> - JSON 配置持久化（`%AppData%\AutoPower\config.json`）
> - 滚动日志（`%AppData%\AutoPower\logs\`）
> - 单元测试（策略评估器 + 空闲恢复逻辑）
>
> **Estimated Effort**: Large
> **Parallel Execution**: YES — 4 waves
> **Critical Path**: Task 1 → Task 2 → Task 4 → Task 6 → Task 9 → Task 12 → Task 14

---

## Context

### Original Request
用户希望构建一个轻量 Windows 程序，有人使用时切换为高性能电源模式，没人时切换为省电模式。

### Interview Summary
**Key Discussions**:
- 检测方式: 三模式可选（键鼠/显示器/同时），OR 逻辑
- 电源计划: 读取系统所有计划（含自定义），用户从下拉列表选择
- 恢复行为: 记忆并恢复原始计划（非硬切换）
- 策略系统: 时间段规则 + 工作日/周末区分 + 优先级叠加 + 手动覆盖（支持 TTL）
- 开机自启: Windows 任务计划程序（schtasks）
- UI: MewUI 设置窗口 + Win32 P/Invoke 托盘图标

**Research Findings**:
- MewUI v0.12.1：NativeAOT-first，无内置托盘控件，须 Win32 P/Invoke 补充
- `GetLastInputInfo`（User32.dll）/ `RegisterPowerSettingNotification` / `PowerSetActiveScheme`（PowrProf.dll）/ `Shell_NotifyIcon`（Shell32.dll）

### Metis Review — Identified Gaps (Addressed)
- 平局决策: 相同优先级规则 → 创建时间更早者优先
- 手动覆盖过期: 支持可选 TTL（不设则永久）
- 唤醒行为: 立即重新评估策略 + 空闲状态
- 缺失 GUID 回退: 托盘气泡通知 + 跳过 + 写日志
- Explorer 重启: 监听 `TaskbarCreated` 消息，自动重注册托盘
- 跨午夜规则: v1 不支持（Start < End 约束）
- Session 锁定: 视为"空闲"（由显示器熄屏事件自然处理）

---

## Work Objectives

### Core Objective
构建一个在后台静默运行的 Windows 系统托盘程序，根据用户活跃状态和可配置的时间段策略自动切换 Windows 电源计划。

### Concrete Deliverables
- `src/AutoPower/` — C# / .NET 8 NativeAOT 项目
- 系统托盘图标 + 右键菜单（Win32 P/Invoke）
- MewUI 设置窗口（4 个功能标签页）
- 策略评估器（带单元测试）
- 空闲检测引擎（键鼠 + 显示器事件）
- 电源计划管理器（Win32 PowrProf.dll 封装）
- 配置服务（JSON 序列化 + 版本迁移）
- 滚动日志服务
- 开机自启动管理（schtasks 封装）
- 单元测试项目 `src/AutoPower.Tests/`

### Definition of Done
- [ ] `dotnet publish -c Release -r win-x64 /p:PublishAot=true` 成功输出 `AutoPower.exe`
- [ ] EXE 启动后在托盘显示图标，右键弹出菜单
- [ ] 设置窗口可正常打开，显示所有系统电源计划
- [ ] 空闲 N 分钟后自动切换电源计划，恢复输入后还原
- [ ] 策略规则在设定时间段内正确生效
- [ ] `dotnet test` 全部通过

### Must Have
- 3 种检测模式可在设置中切换（键鼠/显示器/同时）
- 读取系统所有电源计划并在下拉列表中显示
- 恢复原始计划（而非硬切换）
- 时间段策略 + 工作日/周末 + 优先级叠加
- 手动覆盖（支持可选 TTL）
- 开机自启动（任务计划程序）
- 睡眠/唤醒后立即重新评估
- Explorer 重启后自动恢复托盘图标
- GUID 缺失时气泡通知 + 跳过 + 写日志
- NativeAOT 单 EXE 分发

### Must NOT Have (Guardrails)
- ❌ 摄像头/蓝牙/传感器存在检测
- ❌ 远程管理或企业策略推送
- ❌ 安装包（.msi/.msix）
- ❌ CLI 模式
- ❌ 跨午夜时间段规则（v1）
- ❌ 智能/AI 预测调度
- ❌ 电池电量、进程感知等高级规则
- ❌ 手动覆盖时实时强制覆盖外部计划变更（只在下次触发时重新应用）
- ❌ 过度注释、XAML、全局 `as any`/反射

---

## Verification Strategy

> **ZERO HUMAN INTERVENTION** — ALL verification is agent-executed. No exceptions.

### Test Decision
- **Infrastructure exists**: NO (greenfield)
- **Automated tests**: Tests-after (unit tests for strategy evaluator + idle restore logic)
- **Framework**: `dotnet test` (xUnit)

### QA Policy
- **Backend/Logic**: `dotnet test` + `dotnet run` smoke test
- **Win32 Integration**: `curl`/PowerShell scripts verifying schtasks, config file, power plan switching
- **Build**: `dotnet publish` with NativeAOT flags

---

## Execution Strategy

### Parallel Execution Waves

```
Wave 1 (Start Immediately — foundation):
├── Task 1: Project scaffold + solution structure + NativeAOT config [quick]
├── Task 2: Win32 P/Invoke bindings (User32, Shell32, PowrProf) [quick]
└── Task 3: Core data models + config schema (JSON) [quick]

Wave 2 (After Wave 1 — core services, MAX PARALLEL):
├── Task 4: Power plan manager (enumerate + switch via Win32) [quick]
├── Task 5: Config service (load/save/migrate JSON) [quick]
├── Task 6: Logger service (rolling daily file) [quick]
└── Task 7: Startup manager (schtasks register/unregister) [quick]

Wave 3 (After Wave 2 — detection + strategy engine):
├── Task 8:  Idle detection engine (GetLastInputInfo + timer) [unspecified-high]
├── Task 9:  Monitor state detector (RegisterPowerSettingNotification + HWND) [unspecified-high]
├── Task 10: Strategy evaluator (rule matching + priority sort + tie-break) [deep]
└── Task 11: Strategy evaluator unit tests (tie, cross-day, priority, edge cases) [quick]

Wave 4 (After Wave 3 — orchestration + UI):
├── Task 12: App state machine (ManualOverride > Strategy > IdleFallback) [deep]
├── Task 13: Tray icon + context menu (Win32 Shell_NotifyIcon + TaskbarCreated) [unspecified-high]
└── Task 14: MewUI settings window (4 tabs: Detection, Plans, Strategy, System) [visual-engineering]

Wave 5 (After Wave 4 — integration + QA):
├── Task 15: Integration wiring + NativeAOT publish verification [unspecified-high]
└── Task 16: End-to-end QA scenarios + idle restore integration test [deep]

Wave FINAL (After ALL tasks — independent parallel review):
├── Task F1: Plan compliance audit [oracle]
├── Task F2: Code quality + NativeAOT trim warnings review [unspecified-high]
├── Task F3: Real QA — all tray/settings/strategy scenarios [unspecified-high]
└── Task F4: Scope fidelity check [deep]
```

**Critical Path**: T1 → T2 → T4 → T8/T9 → T10 → T12 → T13/T14 → T15 → T16 → F1-F4
**Parallel Speedup**: ~65% faster than sequential
**Max Concurrent**: 4 (Wave 2 & 3)

### Agent Dispatch Summary
- **Wave 1**: 3 tasks → `quick` × 3
- **Wave 2**: 4 tasks → `quick` × 4
- **Wave 3**: 4 tasks → `unspecified-high` × 2, `deep` × 1, `quick` × 1
- **Wave 4**: 3 tasks → `deep` × 1, `unspecified-high` × 1, `visual-engineering` × 1
- **Wave 5**: 2 tasks → `unspecified-high` × 1, `deep` × 1
- **Final**: 4 tasks → `oracle`, `unspecified-high`, `unspecified-high`, `deep`

---

## TODOs

- [ ] 1. Project Scaffold + Solution Structure + NativeAOT Config

  **What to do**:
  - 创建 `src/AutoPower/` (.NET 8 Console Application) + `src/AutoPower.Tests/` (xUnit) 解决方案
  - 配置 `AutoPower.csproj`：`<PublishAot>true</PublishAot>`、`<RuntimeIdentifier>win-x64</RuntimeIdentifier>`、`<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`、`<ImplicitUsings>enable</ImplicitUsings>`、`<Nullable>enable</Nullable>`
  - 添加 NuGet 引用：`Aprillz.MewUI.Windows`（MewUI）、`xunit`、`xunit.runner.visualstudio`（测试项目）
  - 创建项目目录结构：`src/AutoPower/{Core,Detection,Power,Strategy,UI,Infrastructure}/`
  - 创建 `Program.cs` 入口点（暂时只启动 MewUI Application loop 占位）
  - 创建 `app.manifest`（以普通用户权限运行，声明 DPI 感知）
  - 添加 `.gitignore`、`global.json`（固定 .NET SDK 版本）

  **Must NOT do**:
  - 不引入 WinForms/WPF 依赖
  - 不使用反射（NativeAOT 不兼容）
  - 不创建安装包项目

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 1 — 起始任务
  - **Blocks**: Task 2, 3（需要项目结构存在）
  - **Blocked By**: None

  **References**:
  - MewUI NuGet: `dotnet add package Aprillz.MewUI` (metapackage) + `Aprillz.MewUI.Windows`
  - NativeAOT 配置参考：https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/
  - MewUI 最小示例：`new Window().Title("AutoPower").Content(...); Application.Run(window);`

  **Acceptance Criteria**:
  - [ ] `dotnet build src/AutoPower.sln` ExitCode 0
  - [ ] `dotnet publish src/AutoPower/ -c Release -r win-x64 /p:PublishAot=true` 成功（即使暂时为空程序）
  - [ ] 目录结构存在：`src/AutoPower/{Core,Detection,Power,Strategy,UI,Infrastructure}/`

  **QA Scenarios**:
  ```
  Scenario: 项目构建成功
    Tool: Bash
    Steps:
      1. 运行 `dotnet build src/AutoPower.sln --configuration Release`
      2. 断言 ExitCode == 0
      3. 断言输出不含 "error CS"
    Expected Result: Build succeeded, 0 Error(s)
    Evidence: .sisyphus/evidence/task-1-build.txt

  Scenario: NativeAOT 发布不报错
    Tool: Bash
    Steps:
      1. 运行 `dotnet publish src/AutoPower/ -c Release -r win-x64 /p:PublishAot=true`
      2. 断言 ExitCode == 0
      3. 断言 `publish/AutoPower.exe` 文件存在
    Expected Result: 发布成功，exe 存在
    Evidence: .sisyphus/evidence/task-1-publish.txt
  ```

  **Commit**: YES (Wave 1 group)
  - Message: `chore(scaffold): init project structure and NativeAOT config`

- [ ] 2. Win32 P/Invoke Bindings

  **What to do**:
  - 创建 `src/AutoPower/Infrastructure/Win32/` 目录，分文件定义所有 P/Invoke：
    - `User32.cs`：`GetLastInputInfo`（`LASTINPUTINFO` struct）、`RegisterPowerSettingNotification`、`UnregisterPowerSettingNotification`、`CreateWindowEx`（隐藏消息窗口用）、`DefWindowProc`、`RegisterClassEx`、`PostQuitMessage`、`GetMessage`/`DispatchMessage`
    - `Shell32.cs`：`Shell_NotifyIcon`（`NIM_ADD`/`NIM_MODIFY`/`NIM_DELETE`）、`NOTIFYICONDATA` struct、`WM_TASKBARCREATED` 注册
    - `PowrProf.cs`：`PowerEnumerate`、`PowerSetActiveScheme`、`PowerGetActiveScheme`、`PowerReadFriendlyName`（读计划名称）
    - `Kernel32.cs`：`GetLastError`（错误诊断用）
  - 所有 P/Invoke 使用 `LibraryImportAttribute`（NativeAOT 兼容，非 `DllImport`）
  - 定义相关 GUIDs：`GUID_CONSOLE_DISPLAY_STATE`、三个内置计划 GUID（高性能/均衡/省电）

  **Must NOT do**:
  - 不使用 `DllImport`（用 `LibraryImport` 替代，NativeAOT 友好）
  - 不暴露 unsafe 指针到 Win32 层之外

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES（与 Task 3 并行）
  - **Parallel Group**: Wave 1（与 Task 3）
  - **Blocks**: Task 4, 8, 9, 13
  - **Blocked By**: Task 1

  **References**:
  - `LibraryImport` 文档: https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke-source-generation
  - `NOTIFYICONDATA` 结构: https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-notifyicondataw
  - `PowerEnumerate`: https://learn.microsoft.com/en-us/windows/win32/api/powrprof/nf-powrprof-powerenumerate
  - `GUID_CONSOLE_DISPLAY_STATE`: `02731015-4510-4526-99E6-E5A17EBD1AEA`

  **Acceptance Criteria**:
  - [ ] `dotnet build` 无 P/Invoke 相关编译错误
  - [ ] NativeAOT publish 无 P/Invoke trim 警告（或已用 `[DynamicDependency]` 解决）

  **QA Scenarios**:
  ```
  Scenario: P/Invoke 编译无错误
    Tool: Bash
    Steps:
      1. 运行 `dotnet build src/AutoPower/ -c Release`
      2. 断言输出不含 "error" 或 "warning IL" trim 相关警告
    Expected Result: 0 errors, 0 critical warnings
    Evidence: .sisyphus/evidence/task-2-build.txt
  ```

  **Commit**: YES (Wave 1 group)

- [ ] 3. Core Data Models + Config Schema

  **What to do**:
  - 创建 `src/AutoPower/Core/Models/` 下的所有数据模型：
    - `PowerPlanInfo.cs`：`{ Guid, Name, IsActive }`
    - `DetectionMode.cs`：`enum { KeyboardMouse, MonitorSleep, Both }`
    - `StrategyRule.cs`：`{ Guid Id, string Name, DayType DayType, TimeOnly Start, TimeOnly End, Guid TargetPlanGuid, int Priority, DateTime CreatedAt, bool IsEnabled }`
    - `DayType.cs`：`enum { All, Weekday, Weekend }`
    - `OverrideState.cs`：`{ bool IsActive, Guid? PlanGuid, DateTime? ExpiresAt }`
    - `AppState.cs`：`enum { Active, Idle, ManualOverride }`
    - `AppConfig.cs`：`{ int SchemaVersion, DetectionMode Mode, int IdleTimeoutMinutes, Guid ActivePlanGuid, Guid IdlePlanGuid, List<StrategyRule> Rules, bool AutoStartEnabled, OverrideState Override }`
  - 所有模型使用 `record` 或 `readonly struct`（AOT 友好）
  - 为 `AppConfig` 配置 `[JsonSerializable]` source generator（System.Text.Json）

  **Must NOT do**:
  - 不使用 `JsonSerializer` 反射模式（须用 source generator）
  - 不引入 Newtonsoft.Json

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES（与 Task 2 并行）
  - **Parallel Group**: Wave 1（与 Task 2）
  - **Blocks**: Task 4, 5, 8, 9, 10
  - **Blocked By**: Task 1

  **References**:
  - System.Text.Json source generation: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation
  - NativeAOT + JSON: https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/

  **Acceptance Criteria**:
  - [ ] `dotnet build` 无错误
  - [ ] `AppConfig` 可被 `JsonSerializer.Serialize/Deserialize` 通过 source generator 正确处理（单元测试验证）

  **QA Scenarios**:
  ```
  Scenario: 配置模型序列化往返
    Tool: Bash
    Steps:
      1. 运行 `dotnet test --filter "ConfigSerializationTests"`
      2. 断言 ExitCode == 0，测试全部通过
    Expected Result: AppConfig 序列化再反序列化后值完全一致
    Evidence: .sisyphus/evidence/task-3-serialization.txt
  ```

  **Commit**: YES (Wave 1 group)

- [ ] 4. Power Plan Manager

  **What to do**:
  - 创建 `src/AutoPower/Power/PowerPlanManager.cs`，封装所有电源计划操作：
    - `IEnumerable<PowerPlanInfo> GetAllPlans()` — 调用 `PowerEnumerate` 枚举系统中所有电源方案，再用 `PowerReadFriendlyName` 读取名称
    - `Guid? GetActiveScheme()` — 调用 `PowerGetActiveScheme`
    - `bool TrySetActiveScheme(Guid planGuid, out string? errorMessage)` — 调用 `PowerSetActiveScheme`，若 GUID 不存在返回 false + 错误信息
    - `bool PlanExists(Guid planGuid)` — 验证计划是否存在（枚举中是否包含）
    - 捕获 Win32 错误码，通过 `ILogger` 写日志
  - 定义 `IPowerPlanManager` 接口（便于测试 mock）

  **Must NOT do**:
  - 不调用 `powercfg.exe`（用直接 Win32 API）
  - 不在失败时抛出异常（返回 bool + 错误信息模式）

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES（与 Task 5, 6, 7 并行）
  - **Parallel Group**: Wave 2
  - **Blocks**: Task 8, 9, 12
  - **Blocked By**: Task 2, 3

  **References**:
  - `PowerEnumerate` ACCESS_SCHEME: https://learn.microsoft.com/en-us/windows/win32/api/powrprof/nf-powrprof-powerenumerate
  - `PowerSetActiveScheme`: https://learn.microsoft.com/en-us/windows/win32/api/powrprof/nf-powrprof-powersetactivescheme
  - Win32 P/Invoke 定义位置：`src/AutoPower/Infrastructure/Win32/PowrProf.cs`（Task 2 产出）

  **Acceptance Criteria**:
  - [ ] 调用 `GetAllPlans()` 返回至少 1 个计划（系统有内置计划）
  - [ ] 调用 `TrySetActiveScheme(validGuid)` 返回 true
  - [ ] 调用 `TrySetActiveScheme(randomGuid)` 返回 false + 非空错误信息

  **QA Scenarios**:
  ```
  Scenario: 枚举电源计划
    Tool: Bash (PowerShell)
    Steps:
      1. 运行 dotnet test --filter "PowerPlanManagerTests.GetAllPlans_ReturnsAtLeastOne"
      2. 断言 ExitCode == 0
    Expected Result: 测试通过
    Evidence: .sisyphus/evidence/task-4-power-enum.txt

  Scenario: 设置无效 GUID 返回 false
    Tool: Bash
    Steps:
      1. 运行 dotnet test --filter "PowerPlanManagerTests.TrySetActiveScheme_InvalidGuid_ReturnsFalse"
      2. 断言 ExitCode == 0
    Expected Result: 测试通过，errorMessage 非空
    Evidence: .sisyphus/evidence/task-4-invalid-guid.txt
  ```

  **Commit**: YES (Wave 2 group)

- [ ] 5. Config Service (Load / Save / Migrate)

  **What to do**:
  - 创建 `src/AutoPower/Infrastructure/ConfigService.cs`：
    - `AppConfig Load()` — 从 `%AppData%\AutoPower\config.json` 读取；若文件不存在则创建默认配置并保存；若 `SchemaVersion` 旧则执行迁移
    - `void Save(AppConfig config)` — 原子写入（先写 `.tmp` 再重命名，防止写入中断损坏）
    - 默认配置：`Mode = Both`, `IdleTimeoutMinutes = 5`, 两个计划 GUID 均为 "均衡"（Balanced），无策略规则，`AutoStartEnabled = false`
    - Schema 迁移：当前版本为 `1`，预留 `MigrateFromV0ToV1` 方法结构（目前为空）
  - 定义 `IConfigService` 接口
  - 配置文件目录不存在时自动创建

  **Must NOT do**:
  - 不使用反射序列化（System.Text.Json source generator）
  - 不在读取失败时静默忽略（记录日志并使用默认配置）

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES（与 Task 4, 6, 7 并行）
  - **Parallel Group**: Wave 2
  - **Blocks**: Task 12, 14
  - **Blocked By**: Task 3

  **References**:
  - System.Text.Json source gen：Task 3 中 `AppConfig` 已配置 `[JsonSerializable]`
  - 原子写入模式：先写 `config.json.tmp`，再 `File.Move(tmp, target, overwrite: true)`

  **Acceptance Criteria**:
  - [ ] 首次运行后 `%AppData%\AutoPower\config.json` 存在
  - [ ] `dotnet test --filter "ConfigServiceTests"` 全部通过（包含：首次创建默认、读取修改后保存、损坏文件降级为默认）

  **QA Scenarios**:
  ```
  Scenario: 首次运行创建默认配置
    Tool: Bash (PowerShell)
    Steps:
      1. 删除 %APPDATA%\AutoPower\config.json（若存在）
      2. 运行程序 1 秒后退出
      3. Test-Path "$env:APPDATA\AutoPower\config.json"
      4. 断言返回 True
    Expected Result: 配置文件已创建，包含有效 JSON
    Evidence: .sisyphus/evidence/task-5-config-created.txt
  ```

  **Commit**: YES (Wave 2 group)

- [ ] 6. Logger Service (Rolling Daily File)

  **What to do**:
  - 创建 `src/AutoPower/Infrastructure/LogService.cs`：
    - 日志目录：`%AppData%\AutoPower\logs\`
    - 文件名格式：`autopower-2026-03-02.log`
    - 日志格式：`[2026-03-02 14:30:00.123] [INFO] 消息内容`
    - 级别：`Debug`、`Info`、`Warn`、`Error`
    - 滚动：每次写入时检查日期，若日期变更则切换到新文件
    - 最多保留 7 天日志文件（超出自动删除最旧的）
    - 线程安全：使用 `lock` 保护文件写入
  - 定义 `ILogService` 接口
  - 同时在 `Debug` 模式下输出到 `Debug.WriteLine`

  **Must NOT do**:
  - 不引入 Serilog/NLog 等第三方日志库（保持轻量）
  - 不阻塞主线程（写入可以同步但要快）

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES（与 Task 4, 5, 7 并行）
  - **Parallel Group**: Wave 2
  - **Blocks**: Task 4（反向依赖 Logger 用于错误输出）, Task 8, 9, 12
  - **Blocked By**: Task 1

  **Acceptance Criteria**:
  - [ ] `dotnet test --filter "LogServiceTests"` 通过（写入后文件存在，格式正确，超 7 天文件被删除）

  **QA Scenarios**:
  ```
  Scenario: 日志写入并读回
    Tool: Bash
    Steps:
      1. 运行 dotnet test --filter "LogServiceTests.Write_CreatesLogFile"
      2. 断言 ExitCode == 0
      3. 读取日志文件，断言包含写入的消息内容和正确时间格式
    Expected Result: 日志文件存在，格式为 [timestamp] [LEVEL] message
    Evidence: .sisyphus/evidence/task-6-log-write.txt
  ```

  **Commit**: YES (Wave 2 group)

- [ ] 7. Startup Manager (schtasks Register / Unregister)

  **What to do**:
  - 创建 `src/AutoPower/Infrastructure/StartupManager.cs`：
    - `bool IsRegistered()` — 调用 `schtasks /Query /TN "AutoPower"`，检查 ExitCode == 0
    - `bool Register(string exePath)` — 调用 `schtasks /Create /TN "AutoPower" /TR "\"<exePath>\"" /SC ONLOGON /RU "%USERNAME%" /RL HIGHEST /F`
    - `bool Unregister()` — 调用 `schtasks /Delete /TN "AutoPower" /F`
    - 所有 `schtasks` 调用通过 `Process.Start` 启动，`CreateNoWindow = true`, `RedirectStandardOutput = true`，捕获输出写日志
  - 定义 `IStartupManager` 接口

  **Must NOT do**:
  - 不通过注册表 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 实现（用任务计划程序）
  - 不要求管理员权限（`/RL HIGHEST` 会在任务计划中请求高权限，但注册时不需要）

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES（与 Task 4, 5, 6 并行）
  - **Parallel Group**: Wave 2
  - **Blocks**: Task 14
  - **Blocked By**: Task 1

  **Acceptance Criteria**:
  - [ ] `Register()` 后 `schtasks /Query /TN "AutoPower"` ExitCode == 0
  - [ ] `Unregister()` 后 `schtasks /Query /TN "AutoPower"` ExitCode != 0

  **QA Scenarios**:
  ```
  Scenario: 注册开机自启任务
    Tool: Bash (PowerShell)
    Steps:
      1. 调用 StartupManager.Register(exePath)
      2. 运行 schtasks /Query /TN "AutoPower"
      3. 断言 ExitCode == 0
      4. 断言输出包含 "AutoPower"
    Expected Result: 任务已注册
    Evidence: .sisyphus/evidence/task-7-startup-register.txt

  Scenario: 注销开机自启任务
    Tool: Bash (PowerShell)
    Steps:
      1. 调用 StartupManager.Unregister()
      2. 运行 schtasks /Query /TN "AutoPower"
      3. 断言 ExitCode != 0
    Expected Result: 任务已注销
    Evidence: .sisyphus/evidence/task-7-startup-unregister.txt
  ```

  **Commit**: YES (Wave 2 group)

- [ ] 8. Idle Detection Engine (GetLastInputInfo + Timer)

  **What to do**:
  - 创建 `src/AutoPower/Detection/IdleDetector.cs`，实现基于键鼠空闲时间的检测：
    - 私有 `System.Threading.Timer`，每 30 秒触发一次（不轮询太频繁）
    - 每次 tick：调用 `User32.GetLastInputInfo(ref LASTINPUTINFO)` 计算空闲秒数
    - 若空闲秒数 >= `config.IdleTimeoutMinutes * 60` 且当前状态为"活跃" → 触发 `OnBecameIdle` 事件
    - 若上次状态为"空闲"但现在空闲时间 < 10 秒（有新输入）→ 触发 `OnBecameActive` 事件
    - 提供 `Start()` / `Stop()` 方法，`Stop()` 时 Dispose timer
    - 提供 `TimeSpan GetCurrentIdleTime()` 供 UI 实时显示
    - 仅在 `config.Mode == KeyboardMouse || config.Mode == Both` 时生效
  - 定义 `IIdleDetector` 接口，含 `event Action OnBecameIdle`, `event Action OnBecameActive`

  **Must NOT do**:
  - 不轮询频率高于每 30 秒（对 CPU 的影响可忽略不计）
  - 检测模式为 `MonitorSleep` 时，此组件 `Start()` 应直接返回（不启动 timer）

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES（与 Task 9, 10, 11 并行）
  - **Parallel Group**: Wave 3
  - **Blocks**: Task 12
  - **Blocked By**: Task 2, 3

  **References**:
  - `GetLastInputInfo` P/Invoke：`src/AutoPower/Infrastructure/Win32/User32.cs` (Task 2 产出)
  - `DetectionMode` enum：`src/AutoPower/Core/Models/DetectionMode.cs` (Task 3 产出)
  - `LASTINPUTINFO` struct：`{ uint cbSize; uint dwTime; }` — `dwTime` 是 GetTickCount 值，计算差值得到空闲时间

  **Acceptance Criteria**:
  - [ ] `dotnet test --filter "IdleDetectorTests"` 通过（mock timer 触发，验证事件在正确时机发出）

  **QA Scenarios**:
  ```
  Scenario: 超过超时时间触发 OnBecameIdle（单元测试）
    Tool: Bash
    Steps:
      1. 运行 dotnet test --filter "IdleDetectorTests.FiresOnBecameIdle_AfterTimeout"
      2. 断言 ExitCode == 0
    Expected Result: 事件在 mock 超时后被触发
    Evidence: .sisyphus/evidence/task-8-idle-event.txt

  Scenario: 有新输入后触发 OnBecameActive（单元测试）
    Tool: Bash
    Steps:
      1. 运行 dotnet test --filter "IdleDetectorTests.FiresOnBecameActive_AfterNewInput"
      2. 断言 ExitCode == 0
    Expected Result: 空闲状态下检测到新输入后事件被触发
    Evidence: .sisyphus/evidence/task-8-active-event.txt
  ```

  **Commit**: YES (Wave 3 group)

- [ ] 9. Monitor State Detector (RegisterPowerSettingNotification + Hidden HWND)

  **What to do**:
  - 创建 `src/AutoPower/Detection/MonitorStateDetector.cs`，在专用后台线程上运行 Win32 消息循环：
    - `Start()`：在新 `Thread`（STA）上执行：
      1. `RegisterClassEx` 注册隐藏窗口类
      2. `CreateWindowEx(WS_EX_NOACTIVATE, ..., HWND_MESSAGE)` 创建消息窗口
      3. `RegisterPowerSettingNotification(hwnd, GUID_CONSOLE_DISPLAY_STATE, DEVICE_NOTIFY_WINDOW_HANDLE)` 注册通知
      4. `RegisterWindowMessage("TaskbarCreated")` 记录消息 ID 供 tray 使用
      5. 进入 `GetMessage` / `DispatchMessage` 消息循环
    - 自定义 `WndProc`：处理 `WM_POWERBROADCAST` 中的 `PBT_POWERSETTINGCHANGE`
      - `GUID_CONSOLE_DISPLAY_STATE` 值 `0` → 显示器关闭 → 触发 `OnMonitorOff`
      - `GUID_CONSOLE_DISPLAY_STATE` 值 `1` → 显示器开启 → 触发 `OnMonitorOn`
    - 处理 `WM_POWERBROADCAST` 中的 `PBT_APMRESUMEAUTOMATIC` / `PBT_APMSUSPEND` → 触发 `OnSystemResumed` / `OnSystemSuspended`
    - 收到 `TaskbarCreated` 消息 → 触发 `OnTaskbarCreated`（供 TrayIcon 重注册）
    - `Stop()`：`PostQuitMessage(0)` 退出消息循环
    - 公开 `IntPtr MessageWindowHandle` 属性（供 TrayIcon 注册使用）
    - 仅在 `config.Mode == MonitorSleep || config.Mode == Both` 时注册 `GUID_CONSOLE_DISPLAY_STATE` 通知（但消息窗口总是启动，供 TaskbarCreated 使用）
  - 定义 `IMonitorStateDetector` 接口

  **Must NOT do**:
  - 不在 UI 线程上运行消息循环（须独立线程避免 MewUI 主线程阻塞）
  - 不泄漏注册的通知句柄（Stop 时 `UnregisterPowerSettingNotification`）

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES（与 Task 8, 10, 11 并行）
  - **Parallel Group**: Wave 3
  - **Blocks**: Task 12, 13
  - **Blocked By**: Task 2, 3

  **References**:
  - `RegisterPowerSettingNotification`：`src/AutoPower/Infrastructure/Win32/User32.cs` (Task 2)
  - `GUID_CONSOLE_DISPLAY_STATE`：`02731015-4510-4526-99E6-E5A17EBD1AEA` (Task 2 中已定义)
  - Win32 Message-only window 模式：`CreateWindowEx(0, "STATIC", null, 0, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero)`
  - `PBT_POWERSETTINGCHANGE` 结构：`POWERBROADCAST_SETTING { Guid PowerSetting; uint DataLength; byte Data[1]; }`

  **Acceptance Criteria**:
  - [ ] `dotnet test --filter "MonitorStateDetectorTests"` 通过（mock WndProc 消息，验证事件触发）
  - [ ] 消息窗口句柄非零（`MessageWindowHandle != IntPtr.Zero`）

  **QA Scenarios**:
  ```
  Scenario: 接收 WM_POWERBROADCAST 后触发 OnMonitorOff（单元测试）
    Tool: Bash
    Steps:
      1. 运行 dotnet test --filter "MonitorStateDetectorTests.OnMonitorOff_TriggeredOnDisplayOff"
      2. 断言 ExitCode == 0
    Expected Result: 模拟 GUID_CONSOLE_DISPLAY_STATE=0 消息时事件被触发
    Evidence: .sisyphus/evidence/task-9-monitor-off.txt
  ```

  **Commit**: YES (Wave 3 group)

- [ ] 10. Strategy Evaluator (Rule Matching + Priority Sort + Tie-Break)

  **What to do**:
  - 创建 `src/AutoPower/Strategy/StrategyEvaluator.cs`：
    - `Guid? Evaluate(IReadOnlyList<StrategyRule> rules, DateTime now)` — 核心评估方法：
      1. 过滤 `IsEnabled == true` 的规则
      2. 过滤 `DayType` 匹配（All / Weekday: DayOfWeek 1-5 / Weekend: 0,6）
      3. 过滤 `Start <= now.TimeOfDay <= End`（Start < End 约束，v1 不支持跨午夜）
      4. 按 `Priority desc` 排序；相同 Priority 则按 `CreatedAt asc` 排序（更早创建的优先）
      5. 返回第一条匹配规则的 `TargetPlanGuid`；无匹配返回 `null`
    - `bool ValidateRule(StrategyRule rule, out string errorMessage)` — 验证规则合法性（Start < End, 计划 GUID 存在等）
  - 定义 `IStrategyEvaluator` 接口

  **Must NOT do**:
  - 不支持跨午夜时间段（Start >= End → ValidateRule 返回 false）
  - 不引入正则/脚本引擎等复杂条件判断

  **Recommended Agent Profile**:
  - **Category**: `deep`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES（与 Task 8, 9, 11 并行）
  - **Parallel Group**: Wave 3
  - **Blocks**: Task 11, 12
  - **Blocked By**: Task 3

  **References**:
  - 数据模型：`src/AutoPower/Core/Models/StrategyRule.cs`, `DayType.cs` (Task 3 产出)
  - 平局规则：相同 Priority → `CreatedAt` 升序（更早的优先）—— 由用户决策确认

  **Acceptance Criteria**:
  - [ ] `dotnet test --filter "StrategyEvaluatorTests"` 全部通过（见 Task 11）

  **QA Scenarios**:
  ```
  Scenario: 策略评估覆盖 Task 11 的所有单元测试
    Tool: Bash
    Steps:
      1. 运行 dotnet test --filter "StrategyEvaluatorTests"
      2. 断言 ExitCode == 0，所有测试通过
    Expected Result: 规则评估逻辑正确
    Evidence: .sisyphus/evidence/task-10-strategy-eval.txt
  ```

  **Commit**: YES (Wave 3 group)

- [ ] 11. Strategy Evaluator Unit Tests

  **What to do**:
  - 在 `src/AutoPower.Tests/Strategy/StrategyEvaluatorTests.cs` 编写全面的单元测试（xUnit）：
    - `NoRules_ReturnsNull` — 空规则列表返回 null
    - `DisabledRule_Ignored` — `IsEnabled=false` 的规则被跳过
    - `WrongDayType_NotMatched` — 周末规则在工作日不匹配
    - `TimeOutOfRange_NotMatched` — 当前时间不在规则时间段内
    - `SingleMatchingRule_ReturnsGuid` — 单条匹配规则正确返回 GUID
    - `HigherPriorityWins` — 多条匹配规则中 Priority 最高的胜出
    - `SamePriority_EarlierCreatedWins` — 相同优先级时 CreatedAt 更早的胜出（平局规则）
    - `SamePriority_SameCreatedAt_DeterministicOrder` — 极端情况：完全相同的 CreatedAt，结果应确定性（按 GUID 字符串排序）
    - `All_DayType_MatchesAnyDay` — DayType.All 在所有日期匹配
    - `ValidateRule_CrossMidnight_ReturnsFalse` — Start >= End 验证失败
    - `ValidateRule_ValidRule_ReturnsTrue` — 合法规则通过验证

  **Must NOT do**:
  - 测试不依赖真实系统时间（使用 mock `DateTime`）
  - 不测试 P/Invoke 或 Win32 API

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES（与 Task 8, 9, 10 并行 — 测试可先于实现编写，TDD）
  - **Parallel Group**: Wave 3
  - **Blocks**: 无（测试文件本身不阻塞其他任务，但 Task 10 需要它通过）
  - **Blocked By**: Task 3（需要数据模型定义）

  **References**:
  - `StrategyRule` model：`src/AutoPower/Core/Models/StrategyRule.cs` (Task 3)
  - xUnit 基础模式：`[Fact]` 和 `[Theory]` + `[InlineData]`

  **Acceptance Criteria**:
  - [ ] `dotnet test --filter "StrategyEvaluatorTests"` — 11 条测试全部通过

  **QA Scenarios**:
  ```
  Scenario: 所有策略单元测试通过
    Tool: Bash
    Steps:
      1. 运行 dotnet test src/AutoPower.Tests/ --filter "StrategyEvaluatorTests" --logger "console;verbosity=detailed"
      2. 断言 ExitCode == 0
      3. 断言输出包含 "11 passed"
    Expected Result: 11 passed, 0 failed
    Evidence: .sisyphus/evidence/task-11-strategy-tests.txt
  ```

  **Commit**: YES (Wave 3 group)

---

- [ ] 12. App State Machine (ManualOverride > Strategy > IdleFallback)

  **What to do**:
  - 创建 `src/AutoPower/Core/AppController.cs`，实现核心状态机：
    - 维护当前 `AppState`（`Active` / `Idle` / `ManualOverride`）
    - 启动时：记录当前电源计划为 `_snapshotPlan`（用于恢复）
    - 订阅事件：
      - `IIdleDetector.OnBecameIdle` → 评估是否切换（若无覆盖且无策略）
      - `IIdleDetector.OnBecameActive` → 尝试恢复 `_snapshotPlan`（若处于 IdleFallback 状态）
      - `IMonitorStateDetector.OnMonitorOff` → 同 OnBecameIdle 逻辑
      - `IMonitorStateDetector.OnMonitorOn` → 同 OnBecameActive 逻辑
      - `IMonitorStateDetector.OnSystemResumed` → 立即重新评估全部逻辑
    - 每分钟定时器：重新运行 `StrategyEvaluator.Evaluate(rules, DateTime.Now)`，若结果变化则切换
    - 优先级判断（从高到低）：
      1. `ManualOverride.IsActive == true`（且 TTL 未过期）→ 维持覆盖计划，跳过其他
      2. `StrategyEvaluator` 返回非 null → 切换到策略计划（不记录 snapshot）
      3. 空闲事件 → 切换到 `IdlePlanGuid`（先保存 snapshot）；恢复事件 → 恢复 snapshot
    - 手动覆盖 TTL 过期处理：每分钟检查 `Override.ExpiresAt`，过期后清除 override 并重新评估
    - `_snapshotPlan` 更新时机：用户活跃时若外部更改了计划（非 AutoPower 切换），更新 snapshot
    - 提供 `SetManualOverride(Guid planGuid, TimeSpan? ttl)` 和 `ClearManualOverride()` 方法

  **Must NOT do**:
  - 不允许策略切换触发 snapshot 更新（snapshot 只在 Active 状态且非自动切换时更新）
  - 不在 ManualOverride 期间响应空闲/策略事件

  **Recommended Agent Profile**:
  - **Category**: `deep`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES（与 Task 13, 14 并行）
  - **Parallel Group**: Wave 4
  - **Blocks**: Task 15
  - **Blocked By**: Task 4, 6, 8, 9, 10, 11

  **References**:
  - 数据模型：`src/AutoPower/Core/Models/` (Task 3 产出)
  - `IPowerPlanManager`：`src/AutoPower/Power/PowerPlanManager.cs` (Task 4 产出)
  - `IStrategyEvaluator`：`src/AutoPower/Strategy/StrategyEvaluator.cs` (Task 10 产出)
  - `IIdleDetector`：`src/AutoPower/Detection/IdleDetector.cs` (Task 8 产出)
  - `IMonitorStateDetector`：`src/AutoPower/Detection/MonitorStateDetector.cs` (Task 9 产出)

  **Acceptance Criteria**:
  - [ ] `dotnet test --filter "AppControllerTests"` 全部通过（包含：空闲→切换→恢复，策略优先于空闲，手动覆盖期间空闲事件被忽略，TTL 过期后恢复自动模式）

  **QA Scenarios**:
  ```
  Scenario: 空闲→切换→恢复 完整流程（单元测试）
    Tool: Bash
    Steps:
      1. 运行 dotnet test --filter "AppControllerTests.IdleFallback_SwitchAndRestore"
      2. 断言 ExitCode == 0
    Expected Result: 切换到空闲计划后，active 事件触发时恢复 snapshot
    Evidence: .sisyphus/evidence/task-12-idle-restore.txt

  Scenario: 手动覆盖 TTL 过期后自动清除
    Tool: Bash
    Steps:
      1. 运行 dotnet test --filter "AppControllerTests.ManualOverride_ExpiresAfterTTL"
      2. 断言 ExitCode == 0
    Expected Result: TTL 过期后 ManualOverride.IsActive == false，重新评估策略
    Evidence: .sisyphus/evidence/task-12-override-ttl.txt
  ```

  **Commit**: YES (Wave 4 group)

- [ ] 13. Tray Icon + Context Menu (Win32 Shell_NotifyIcon + TaskbarCreated)

  **What to do**:
  - 创建 `src/AutoPower/UI/TrayIcon.cs`，管理系统托盘图标生命周期：
    - `Initialize(HWND messageHwnd)` — 调用 `Shell_NotifyIcon(NIM_ADD, ...)` 注册图标
      - 图标：嵌入 `.ico` 资源（16x16，至少提供活跃/空闲两个状态图标）
      - Tooltip：显示当前计划名称和状态（"AutoPower — 活跃: 高性能"）
    - `UpdateStatus(AppState state, string planName)` — 调用 `NIM_MODIFY` 更新图标和 tooltip
    - `ShowBalloon(string title, string message, int timeoutMs)` — 使用 `NIIF_INFO` 气泡通知
    - `ShowContextMenu()` — 在鼠标位置创建弹出菜单（`CreatePopupMenu` + `AppendMenu` + `TrackPopupMenu`）
      - 菜单项："当前计划：[名称]"（灰色，信息用）、"打开设置…"、分隔符、"手动覆盖 ▶"（子菜单：计划列表 + 过期时长选项）、"清除覆盖"（若当前覆盖中）、分隔符、"退出"
    - 处理 `NIN_BALLOONUSERCLICK`（气泡点击打开设置）
    - 处理 `TaskbarCreated` 消息（Explorer 重启后自动调用 `NIM_ADD` 重注册）
    - `Dispose()` — 调用 `NIM_DELETE` 清除图标

  **Must NOT do**:
  - 不在托盘右键菜单中显示超过 8 个计划（若计划数 > 8，只显示当前活跃的 + 前 7 个）
  - 不使用 WinForms `NotifyIcon`

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES（与 Task 12, 14 并行）
  - **Parallel Group**: Wave 4
  - **Blocks**: Task 15
  - **Blocked By**: Task 2, 9（共用消息循环 HWND）

  **References**:
  - `Shell_NotifyIcon` P/Invoke：`src/AutoPower/Infrastructure/Win32/Shell32.cs` (Task 2 产出)
  - `NOTIFYICONDATA` 结构：Task 2 中已定义
  - `TaskbarCreated` 消息：`RegisterWindowMessage("TaskbarCreated")`（Task 9 的消息循环转发 OnTaskbarCreated 事件）
  - 图标资源嵌入：`<EmbeddedResource Include="Resources\tray-active.ico" />`

  **Acceptance Criteria**:
  - [ ] 程序启动后托盘中出现图标（真实运行验证，无法纯单元测试）
  - [ ] 右键菜单包含"打开设置"和"退出"选项
  - [ ] 任务管理器关闭 explorer.exe 后重启，图标自动恢复

  **QA Scenarios**:
  ```
  Scenario: 托盘图标显示并有右键菜单
    Tool: Bash (PowerShell)
    Steps:
      1. 启动 AutoPower.exe（后台进程）
      2. 运行 PowerShell：`(Get-Process AutoPower).Id` 断言进程存在
      3. 检查 Shell_NotifyIcon 调用成功（通过日志文件验证）
    Expected Result: 进程运行中，日志包含 "TrayIcon initialized"
    Evidence: .sisyphus/evidence/task-13-tray-init.txt

  Scenario: 缺失 GUID 时显示气泡通知
    Tool: Bash
    Steps:
      1. 在 config.json 中将 ActivePlanGuid 设为随机 GUID
      2. 触发计划切换（模拟活跃事件）
      3. 读取日志文件，断言包含 "Power plan GUID not found" 和 "balloon notification"
    Expected Result: 未崩溃，日志记录错误，气泡通知已发送
    Evidence: .sisyphus/evidence/task-13-missing-guid.txt
  ```

  **Commit**: YES (Wave 4 group)

- [ ] 14. MewUI Settings Window (4 Tabs)

  **What to do**:
  - 创建 `src/AutoPower/UI/SettingsWindow.cs`，使用 MewUI 代码优先 API 构建设置窗口：
    - 窗口：`new Window().Title("AutoPower 设置").Size(520, 480).Resizable(false)`
    - 使用 `TabControl` 分 4 个标签页：

    **Tab 1 — 检测**:
    - RadioButton 组：键盘/鼠标、显示器熄屏、两者同时
    - NumericUpDown：空闲超时（分钟，1–120，仅键鼠/两者模式下启用）
    - Label：当前空闲时间（实时更新，绑定 ObservableValue）

    **Tab 2 — 电源计划**:
    - ComboBox "活跃时使用"：列出所有系统计划
    - ComboBox "空闲时使用"：列出所有系统计划
    - Label：当前活跃计划（实时显示）

    **Tab 3 — 策略**:
    - GridView 列表：显示所有策略规则（优先级 | 生效日 | 时间段 | 目标计划 | 启用）
    - Button [+ 新建]、[编辑]、[删除]、[↑ 上移]、[↓ 下移]（改变优先级）
    - 新建/编辑弹窗：ComboBox（生效日: 全部/工作日/周末）+ 时间范围输入（HH:mm）+ 计划选择 + 优先级数字 + 启用开关

    **Tab 4 — 系统**:
    - ToggleSwitch "开机自启"（绑定 `IStartupManager.IsRegistered()`）
    - Button "立即启用手动覆盖"（触发子选单：选计划 + 过期时长）
    - Button "清除手动覆盖"（仅覆盖中时启用）
    - Label "应用版本：1.0.0"
    - Button "打开日志目录"（`Process.Start("explorer.exe", logsPath)`）

    - 底部：Button [保存] + Button [取消]（保存前调用 `IConfigService.Save`）
    - 窗口关闭时隐藏（不退出程序），点托盘"退出"才退出

  **Must NOT do**:
  - 不在窗口关闭时退出进程（`window.OnClosing(() => { window.Hide(); return false; })`）
  - 不使用 XAML/反射绑定

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES（与 Task 12, 13 并行）
  - **Parallel Group**: Wave 4
  - **Blocks**: Task 15
  - **Blocked By**: Task 5, 7

  **References**:
  - MewUI 控件列表（Task 研究结果）：Window, TabControl, RadioButton, ComboBox, GridView, ToggleSwitch, NumericUpDown, Button, Label, StackPanel, DockPanel
  - MewUI ObservableValue 绑定：`new ObservableValue<T>(initial); control.BindText(obs, v => ...)`
  - MewUI 最小示例：`Application.Run(window)` — 需在主线程调用
  - `IConfigService` 接口：Task 5 产出
  - `IStartupManager` 接口：Task 7 产出

  **Acceptance Criteria**:
  - [ ] 设置窗口可打开，4 个标签页均可切换
  - [ ] 保存按钮调用 `IConfigService.Save`，重启后设置保留
  - [ ] 关闭窗口后程序仍在托盘运行（进程不退出）

  **QA Scenarios**:
  ```
  Scenario: 设置窗口打开并显示所有电源计划
    Tool: Bash (PowerShell)
    Steps:
      1. 启动 AutoPower.exe
      2. 通过右键菜单触发"打开设置"（或程序启动时直接打开设置窗口）
      3. 读取日志确认"SettingsWindow opened"
      4. 检查 config.json 中计划 GUID 是否为有效系统计划
    Expected Result: 日志记录窗口打开，配置包含有效计划 GUID
    Evidence: .sisyphus/evidence/task-14-settings-open.txt

  Scenario: 保存设置后持久化
    Tool: Bash (PowerShell)
    Steps:
      1. 修改 config.json 中 IdleTimeoutMinutes 为 10
      2. 重启程序
      3. 读取 %APPDATA%\AutoPower\config.json
      4. 断言 idleTimeoutMinutes == 10
    Expected Result: 配置正确持久化
    Evidence: .sisyphus/evidence/task-14-config-persist.txt
  ```

  **Commit**: YES (Wave 4 group)

- [ ] 15. Integration Wiring + NativeAOT Publish Verification

  **What to do**:
  - 更新 `Program.cs`，完成所有组件的依赖注入和启动序列：
    - 构建依赖容器（手动 DI，无需 `Microsoft.Extensions.DependencyInjection`，直接 `new`）：
      1. `ILogService` → `LogService`
      2. `IConfigService` → `ConfigService(logger)`
      3. `AppConfig config = configService.Load()`
      4. `IPowerPlanManager` → `PowerPlanManager(logger)`
      5. `IStartupManager` → `StartupManager(logger)`
      6. `IStrategyEvaluator` → `StrategyEvaluator(logger)`
      7. `IIdleDetector` → `IdleDetector(config, logger)`
      8. `IMonitorStateDetector` → `MonitorStateDetector(logger)`（在此 HWND 上注册 TaskbarCreated 转发）
      9. `AppController` → `AppController(config, powerMgr, stratEval, idleDet, monDet, logger)`
      10. `TrayIcon` → `TrayIcon(config, controller, powerMgr, logger)`（传入 MonitorStateDetector 的 HWND）
      11. `SettingsWindow` → `SettingsWindow(config, configService, powerMgr, startupMgr, controller)`
    - 启动顺序：`monDet.Start()` → `idleDet.Start()` → `controller.Start()` → `trayIcon.Initialize(hwnd)` → `Application.Run(settingsWindowHidden)`
    - 关闭时：`Application.OnExit(() => { trayIcon.Dispose(); idleDet.Stop(); monDet.Stop(); })`
    - 单实例检查：使用命名 `Mutex` 防止重复启动（已有实例时将其设置窗口置前并退出）
  - 执行 NativeAOT 发布并修复所有 trim 警告：
    - 运行 `dotnet publish -c Release -r win-x64 /p:PublishAot=true`
    - 逐个修复 IL 警告（通常为 P/Invoke 结构体或回调需 `[DynamicDependency]`）
    - 验证输出 exe 大小 ≤ 10MB
    - 验证 exe 在干净 Windows 10/11 环境可运行（无需 .NET 运行时）

  **Must NOT do**:
  - 不引入 `Microsoft.Extensions.DependencyInjection`（手动 DI 保持轻量）
  - 不允许 trim 警告被忽略（必须逐个解决）

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: NO（依赖所有 Wave 4 完成）
  - **Parallel Group**: Wave 5
  - **Blocks**: Task 16, Final Wave
  - **Blocked By**: Task 12, 13, 14

  **Acceptance Criteria**:
  - [ ] `dotnet publish -c Release -r win-x64 /p:PublishAot=true` ExitCode 0
  - [ ] 发布目录存在 `AutoPower.exe`，大小 ≤ 10MB
  - [ ] 启动 exe 后托盘图标出现
  - [ ] 重复启动时第二个实例直接退出（单实例保护）

  **QA Scenarios**:
  ```
  Scenario: NativeAOT 发布成功且文件大小合理
    Tool: Bash (PowerShell)
    Steps:
      1. 运行 dotnet publish -c Release -r win-x64 /p:PublishAot=true
      2. 断言 ExitCode == 0
      3. $size = (Get-Item publish/AutoPower.exe).Length / 1MB; 断言 $size -le 10
    Expected Result: 发布成功，EXE ≤ 10MB
    Evidence: .sisyphus/evidence/task-15-publish.txt

  Scenario: 单实例保护
    Tool: Bash (PowerShell)
    Steps:
      1. 启动第一个 AutoPower.exe 实例
      2. 再启动第二个实例
      3. 断言第二个实例进程已退出（ExitCode 0 或 GetProcessesByName count == 1）
    Expected Result: 只有一个实例运行
    Evidence: .sisyphus/evidence/task-15-single-instance.txt
  ```

  **Commit**: YES (Wave 5 group)

- [ ] 16. End-to-End QA Scenarios + Idle Restore Integration Test

  **What to do**:
  - 在 `src/AutoPower.Tests/Integration/` 下编写集成测试（模拟组件交互，不依赖真实 Win32）：
    - `IdleRestoreIntegrationTest.cs`：
      - Mock `IPowerPlanManager`（记录所有 `TrySetActiveScheme` 调用）
      - Mock `IIdleDetector`（手动触发事件）
      - 验证：启动 → 记录 snapshot → 触发 OnBecameIdle → 切换到 IdlePlan → 触发 OnBecameActive → 恢复 snapshot
    - `StrategyOverrideIntegrationTest.cs`：
      - 添加一条当前时段匹配的策略规则
      - 验证：策略切换生效 + 空闲事件在策略模式下不切换
    - `ManualOverrideTTLIntegrationTest.cs`：
      - 设置 TTL = 1ms 的覆盖
      - 推进时间 Mock，验证 TTL 过期后清除覆盖并重新评估
  - 运行真实 E2E 场景验证（使用 PowerShell 脚本）：
    - 读取当前电源计划，记录初始值
    - 修改 config 将空闲超时设为 1 分钟
    - 等待 70 秒无输入
    - 验证电源计划已切换（`powercfg /getactivescheme`）
    - 模拟输入（`[System.Windows.Forms.SendKeys]`）
    - 验证恢复到初始计划

  **Must NOT do**:
  - 不依赖真实系统空闲时间（集成测试须 mock 时间/事件）
  - 不在 E2E 脚本中硬编码计划 GUID（从系统动态读取）

  **Recommended Agent Profile**:
  - **Category**: `deep`
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES（可与 Task 15 后期阶段并行）
  - **Parallel Group**: Wave 5（依赖 Task 15 的发布产出）
  - **Blocks**: Final Wave
  - **Blocked By**: Task 12, 13, 14, 15

  **Acceptance Criteria**:
  - [ ] `dotnet test --filter "Integration"` 全部通过（3 个集成测试）
  - [ ] E2E PowerShell 脚本运行后验证真实计划切换和恢复

  **QA Scenarios**:
  ```
  Scenario: 集成测试全部通过
    Tool: Bash
    Steps:
      1. 运行 dotnet test src/AutoPower.Tests/ --filter "Integration" --logger "console;verbosity=normal"
      2. 断言 ExitCode == 0，不含 "Failed"
    Expected Result: 3 integration tests passed
    Evidence: .sisyphus/evidence/task-16-integration-tests.txt

  Scenario: E2E 真实电源计划切换（需手动无输入60秒）
    Tool: Bash (PowerShell)
    Steps:
      1. 读取当前计划：powercfg /getactivescheme，记录 GUID
      2. 将 idleTimeoutMinutes 设为 1，重启程序
      3. 等待 70 秒（脚本 sleep）
      4. 再次 powercfg /getactivescheme，断言 GUID 已变更为 idlePlanGuid
      5. 运行 [System.Windows.Forms.SendKeys]::SendWait(" ") 模拟输入
      6. 等待 3 秒，再次 powercfg /getactivescheme，断言 GUID 已恢复初始值
    Expected Result: 切换并恢复成功
    Evidence: .sisyphus/evidence/task-16-e2e-powerswitch.txt
  ```

  **Commit**: YES (Wave 5 group)

---

- [ ] F1. **Plan Compliance Audit** — `oracle`
  逐条核查 Must Have 列表：读取相关文件/运行命令验证功能存在；搜索 Must NOT Have 禁止模式；检查 `.sisyphus/evidence/` 中证据文件是否完整。
  Output: `Must Have [N/N] | Must NOT Have [N/N] | Evidence [N/N] | VERDICT: APPROVE/REJECT`

- [ ] F2. **Code Quality + NativeAOT Review** — `unspecified-high`
  运行 `dotnet publish -c Release -r win-x64 /p:PublishAot=true`，检查 trim 警告；运行 `dotnet test`；审查所有文件中的 `as any`、空 catch、console.log、注释代码、未用 import；检查反射用法（NativeAOT 不兼容）。
  Output: `Build [PASS/FAIL] | Tests [N pass/N fail] | TrimWarnings [N] | VERDICT`

- [ ] F3. **Real Manual QA** — `unspecified-high`
  从干净状态执行每个任务的全部 QA 场景；测试托盘图标右键菜单；打开设置窗口每个标签页；验证策略切换；验证手动覆盖 TTL；验证开机自启注册/注销；保存证据到 `.sisyphus/evidence/final-qa/`。
  Output: `Scenarios [N/N pass] | VERDICT`

- [ ] F4. **Scope Fidelity Check** — `deep`
  对每个任务：对比"What to do"与实际 diff；验证无遗漏、无多余；检查 Must NOT Do 合规；标记跨任务污染。
  Output: `Tasks [N/N compliant] | Contamination [CLEAN/N issues] | VERDICT`

---

## Commit Strategy

- **Wave 1**: `chore(scaffold): init project structure and Win32 P/Invoke bindings`
- **Wave 2**: `feat(core): power manager, config service, logger, startup manager`
- **Wave 3**: `feat(engine): idle detection, monitor state, strategy evaluator + tests`
- **Wave 4**: `feat(ui): app state machine, tray icon, MewUI settings window`
- **Wave 5**: `feat(integration): wire all components, NativeAOT publish, E2E QA`

---

## Success Criteria

### Verification Commands
```powershell
# Build succeeds with NativeAOT
dotnet publish -c Release -r win-x64 /p:PublishAot=true
# Expected: ExitCode 0, AutoPower.exe exists in publish/

# All unit tests pass
dotnet test src/AutoPower.Tests/
# Expected: X tests passed, 0 failed

# Config file created on first run
Test-Path "$env:APPDATA\AutoPower\config.json"
# Expected: True

# Startup task registered
schtasks /Query /TN "AutoPower"
# Expected: ExitCode 0, task listed

# Startup task removed after disable
# Expected: ExitCode != 0 (task not found)
```

### Final Checklist
- [ ] 所有 Must Have 功能已实现
- [ ] 所有 Must NOT Have 禁止模式不存在
- [ ] `dotnet test` 全部通过
- [ ] NativeAOT 发布无阻塞性 trim 警告
- [ ] `AutoPower.exe` 体积 ≤ 10MB
- [ ] `.sisyphus/evidence/` 中所有场景证据文件存在
