---
name: rule-tree-with-detectors-refactor-plan
overview: 将 AutoPower 的策略系统重构为支持树形规则与运行时条件上下文的决策引擎，把 `Keyboard/Mouse` 与 `Monitor` 检测从全局空闲分支提升为可组合条件节点，同时统一默认回退、预览与实时切换。
todos:
  - id: freeze-rule-semantics
    content: 使用 [subagent:code-explorer] 固化检测条件、默认回退与兼容语义
    status: completed
  - id: refactor-config-and-models
    content: 重构配置模型与归一化加载，兼容旧规则和旧检测字段
    status: completed
    dependencies:
      - freeze-rule-semantics
  - id: unify-evaluation-flow
    content: 引入求值上下文，统一规则树、Override 与安全回退链路
    status: completed
    dependencies:
      - refactor-config-and-models
  - id: upgrade-settings-and-preview
    content: 改造设置页规则编辑、检测源配置和预览状态说明
    status: completed
    dependencies:
      - unify-evaluation-flow
  - id: verify-tests-and-docs
    content: 补齐测试与 README，验证兼容性、预览与 AOT 约束
    status: completed
    dependencies:
      - unify-evaluation-flow
      - upgrade-settings-and-preview
---

## User Requirements

### User Requirements

- 将当前只支持日期与时间段的扁平规则，升级为可组合的条件规则体系，把 `Keyboard/Mouse` 检测和 `Monitor` 检测都纳入规则条件，而不是仅作为独立的空闲回退逻辑。
- 为未命中规则的情况保留统一默认出口，并继续提供安全回退，避免不同入口出现“无结果”或切换不一致。
- 不需要兼容旧配置，完全重构配置文件。

### Product Overview

- 应用仍然负责自动切换电源计划，但规则表达会从“线性时间表”提升为“带检测条件的层级规则”。
- 设置界面中的规则区域会从简单列表演进为分组化卡片，支持编辑检测条件、默认项与预览说明；视觉上更接近树状规则面板，但仍保持当前卡片式操作方式。

### Core Features

- 支持时间、星期、键鼠空闲、显示器状态等条件组合判断
- 支持统一默认结果与最终安全回退
- 支持实时切换、手动覆盖、预览说明共用同一套判断语义
- 不做旧配置兼容。

## Tech Stack Selection

- 运行时：基于现有 `.NET 10 / C#`
- 配置序列化：继续使用 `System.Text.Json` Source Generation，入口已在 `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Core/Models/AppConfigJsonContext.cs`
- 桌面界面：复用现有 `MewUI` 设置窗口
- 测试：复用现有 `xUnit` 与 `Manual/DebugTests`

## Implementation Approach

- 推荐把本次重构落为“动作规则 + 条件树 + 运行时上下文”，而不是直接让 `plan` 与 `condition` 节点混排后共同产出结果。原因是当前用户样例里若 `all/none` 组直接命中，会出现“该返回哪个计划”的语义歧义。
- 更稳妥的方案是：`StrategyRule` 继续代表可产出电源计划的规则；规则内部新增条件树，条件树支持 `any/all/none`，原子条件包含 `dayType`、时间段、`Keyboard/Mouse Idle`、`Monitor Off`。这样与现有 `TargetPlanGuid` 模型更贴合，也更利于旧配置迁移。
- 引入统一的 `StrategyEvaluationContext`，由 `AppController` 把 `DateTime.Now`、键鼠空闲状态、显示器状态、检测源是否启用等信息组装后传给 `StrategyEvaluator`。检测器本身保留在 `Detection/`，只负责采集状态，不再直接决定计划。
- 保持兼容回退顺序：`Manual Override → 规则命中 → default → 现有 Active/Idle fallback`。这样即使新规则缺失或引用无效，也不会破坏当前行为。
- `PreviewEngine` 不能真实预测未来的键鼠/显示器状态，因此应改成“基于假定检测上下文预览”。默认可按“当前检测快照”预览，并对含实时条件的规则标注来源，避免把实时条件伪装成可精确预测的时间表。
- 性能上，当前 `StrategyEvaluator` 每次会筛选并排序，约为 `O(n log n)`；重构后应在 `ConfigService.Load()` 阶段完成旧规则归一化与顺序固化，运行时按规则顺序和条件树递归求值，单次评估可控制在 `O(n)`。预览仍保留检查点扫描，避免逐分钟遍历。

## Implementation Notes

- 复用 `LoggerService`，仅记录规则 Id、默认命中来源、配置校验摘要，避免输出整份配置。
- `ConfigService.Load()` 增加归一化与校验：兼容旧扁平规则、清理无效默认引用、检测重复 Id 与循环结构，并安全降级。
- `AppController.InitializeDetectors()` 先继续复用 `DetectionMode` 决定启用哪些检测器，减少改动面；规则若引用未启用检测源，则按“不满足条件”处理并记录可读原因。
- `PreviewEngine` 与运行时必须共用同一求值器，禁止再次复制时间/检测判断逻辑。
- 保持 NativeAOT 友好：新增模型都显式注册到 `AppConfigJsonContext`，避免运行时多态黑箱。

## Architecture Design

- `IdleDetector` 与 `MonitorStateDetector`：继续采集状态，发出事件
- `AppController`：维护检测快照，构建 `StrategyEvaluationContext`，组合 Override 与最终回退
- `StrategyEvaluator`：唯一规则求值入口，返回统一决策结果
- `PreviewEngine`：基于时间检查点和假定检测上下文生成时间线说明
- `SettingsWindow`：编辑检测源配置、规则条件树、默认项和预览假设

## Directory Structure

## Directory Structure Summary

下方目录和新文件的逻辑仅供参考，可以根据实际需求调整。
本次改造以“规则模型升级、检测状态上下文化、统一求值链、设置页扩展、测试回归”为主，优先复用现有目录并最小化对 NativeAOT 的风险。

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Core/Models/StrategyRule.cs` [MODIFY]  
目的：把当前扁平计划规则升级为可承载条件树的动作规则。
内容：保留现有计划字段，新增条件根节点、兼容旧字段读取。
要求：旧 JSON 缺少新字段时仍能作为旧规则加载。

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Core/Models/StrategyConditionGroup.cs` [NEW]  
目的：表达 `any/all/none` 条件组。
内容：定义分组类型与子条件集合。
要求：结构简单、可被 SourceGen 显式注册。

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Core/Models/StrategyCondition.cs` [NEW]  
目的：表达原子条件。
内容：封装星期、时间段、键鼠空闲、显示器状态等条件参数。
要求：避免反射式多态，字段显式可序列化。

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Core/Models/StrategyEvaluationContext.cs` [NEW]  
目的：统一运行时输入。
内容：包含当前时间、检测快照、检测源可用性。
要求：供实时切换与预览共用。

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Core/Models/AppConfig.cs` [MODIFY]  
目的：承载新规则结构、默认项和旧字段兼容。
内容：扩展 `Rules` 与 `Default` 定义，保留 `Mode/ActivePlanGuid/IdlePlanGuid` 作为兼容回退。
要求：缺省值安全。

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Core/Models/AppConfigJsonContext.cs` [MODIFY]  
目的：补全新模型的 SourceGen 元数据。
内容：显式注册条件模型与配置模型。
要求：继续满足 NativeAOT。

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Core/ConfigService.cs` [MODIFY]  
目的：加载时完成归一化和校验。
内容：旧规则迁移、默认引用校验、重复 Id/循环检测、安全降级。
要求：失败不阻塞启动。

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Core/AppController.cs` [MODIFY]  
目的：把检测器状态改为规则求值上下文输入。
内容：维护检测快照，调用统一求值器，落实最终回退链。
要求：不改变现有 Override 生命周期。

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Strategy/StrategyEvaluator.cs` [MODIFY]  
目的：统一规则求值。
内容：从上下文求值条件树并输出统一决策结果。
要求：运行期避免重复排序和重复分配。

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Strategy/StrategyDecision.cs` [NEW]  
目的：承载统一决策结果。
内容：包含计划 Guid、来源说明、命中规则 Id、是否默认/回退。
要求：供 `AppController` 与 `PreviewEngine` 共用。

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Strategy/PreviewEngine.cs` [MODIFY]  
目的：移除重复匹配逻辑。
内容：复用统一求值入口，并支持“按当前检测状态预览”。
要求：明确标注实时条件的不确定性。

- `a:/Documents/GitHub/AutoPower/src/AutoPower/UI/SettingsWindow.cs` [MODIFY]  
目的：扩展设置界面。
内容：把全局检测配置与规则条件编辑整合进规则系统，并补充默认项和预览说明。
要求：先保证稳定编辑，再考虑更复杂交互。

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Tests/Strategy/StrategyEvaluatorTests.cs` [MODIFY]  
目的：覆盖规则语义。
内容：增加检测条件、条件组、默认出口、禁用检测源、循环防护、旧规则兼容测试。
要求：保持现有测试风格。

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Tests/Core/ConfigSerializationTests.cs` [MODIFY]  
目的：验证序列化兼容。
内容：旧配置读取、新结构往返、缺省字段兼容。
要求：继续基于 `AppConfigJsonContext`。

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Tests/Core/ConfigServiceTests.cs` [MODIFY]  
目的：验证加载期归一化与降级。
内容：无效默认引用、重复 Id、循环结构、损坏配置。
要求：不引入真实外部依赖。

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Tests/Manual/DebugTests.cs` [MODIFY]  
目的：保留人工验证入口。
内容：增加按当前检测快照预览和检测状态联动验证。
要求：继续作为手工调试辅助。

- `a:/Documents/GitHub/AutoPower/README.md` [MODIFY]  
目的：更新规则模型与兼容说明。
内容：补充条件树、检测条件、默认出口和预览限制。
要求：避免新旧配置混淆。

## Agent Extensions

### SubAgent

- **code-explorer**
- Purpose: 复核检测器、规则模型、求值链路、预览与 UI 的全部影响面
- Expected outcome: 输出完整调用点与残留扁平规则假设清单，降低重构遗漏风险