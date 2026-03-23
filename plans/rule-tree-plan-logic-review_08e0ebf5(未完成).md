---
name: rule-tree-plan-logic-review
overview: 评估将当前扁平 `StrategyRule` 调度改为 `plan + condition(any/all/none)` 树形规则结构的可行性，明确对模型、序列化、评估器、默认回退和 UI/测试的影响，并给出可执行的重构方向。
todos:
  - id: freeze-strategy-contract
    content: 使用 [subagent:code-explorer] 固化规则树语义、默认回退与影响面
    status: pending
  - id: refactor-config-model
    content: 重构 AppConfig 与规则节点模型，兼容旧配置反序列化
    status: pending
    dependencies:
      - freeze-strategy-contract
  - id: unify-decision-engine
    content: 统一 StrategyEvaluator、AppController、PreviewEngine 的求值与回退
    status: pending
    dependencies:
      - refactor-config-model
  - id: upgrade-settings-window
    content: 改造 SettingsWindow 支持条件组、默认项与节点校验
    status: pending
    dependencies:
      - unify-decision-engine
  - id: verify-and-document
    content: 补齐测试与 README，验证 Release 和 AOT 发布
    status: pending
    dependencies:
      - unify-decision-engine
      - upgrade-settings-window
---

## User Requirements

- 将当前按时间段匹配电源计划的扁平规则，升级为可表达嵌套布尔逻辑的规则结构；叶子节点为 `plan`，条件节点支持 `any`、`all`、`none`。
- 为未命中规则的场景增加统一默认出口，避免只能依赖现有活动/空闲计划回退。
- 结合现有代码评估该方案是否可行，并给出更稳妥、可维护、可扩展的优化建议，重点覆盖配置结构、求值链路、预览一致性、兼容性与边界情况。

## Product Overview

- 当前系统会根据日期与时间窗口自动切换电源计划；改造后，规则表达能力将从“单条时间规则”提升为“可组合条件规则”。
- 如果后续落地到设置界面，原有扁平规则卡片会演变为层级分组式编辑，视觉上会更接近树状或嵌套分组面板。

## Core Features

- 支持 `plan` 节点与 `any/all/none` 条件组组合判断
- 支持统一默认命中结果与安全回退
- 保证实时切换、预览时间线、手动覆盖的行为一致
- 兼容旧配置并处理空组、无效引用、冲突优先级等边界情况

## Tech Stack Selection

- 核心运行时：C# / .NET 10
- 配置序列化：System.Text.Json Source Generation，入口为 `src/AutoPower.Core/Core/Models/AppConfigJsonContext.cs`
- 桌面 UI：MewUI Win32，入口为 `src/AutoPower/UI/SettingsWindow.cs`
- 测试：xUnit
- 现有约束：NativeAOT、无反射、手动 DI、静态 `ConfigService` 与 `LoggerService`

## Implementation Approach

结论：方向可行，但按你给出的 JSON 直接落地还不够完整。当前代码里 `StrategyEvaluator` 只能从扁平规则中返回一条规则，`AppController` 与 `PreviewEngine` 又各自定义了未命中回退；如果直接把 `plan` 和 `any/all/none` 混在一起，`all` 与 `none` 命中后究竟返回哪个计划、`default.id` 是否允许指向条件节点、空条件组是否永真，都需要先冻结语义。

推荐方案：

1. 采用 AOT 友好的统一节点模型，优先复用现有规则字段并扩展 `type`、`condition`、`rules`、默认回退定义，避免运行时多态反射。旧 JSON 缺少 `type` 时按 `plan` 处理。
2. 引入单一“决策结果”契约，由 `StrategyEvaluator` 统一返回目标计划、来源说明、命中节点 Id、是否走默认/系统回退；`AppController` 与 `PreviewEngine` 全部复用它，消除双份逻辑。
3. 回退顺序建议保持兼容：手动 Override ＞ 规则树命中 ＞ `default` ＞ 现有 `ActivePlanGuid` / `IdlePlanGuid`。这样即使新配置缺失或无效，旧行为仍可保留。
4. 若保留 `default.id`，应只允许引用可直接产出计划的节点，并在加载/保存时校验唯一 Id、悬挂引用、循环嵌套、空 `all/none` 组；如果希望更简单稳健，更推荐直接落到 `defaultPlanGuid`。
5. 不建议在树结构里同时保留“兄弟顺序”和全局 `priority` 两套优先级。长期更稳妥的是用节点顺序表达优先级；若要平滑迁移，可先按当前 `Priority desc / CreatedAt asc / Id asc` 规则把旧列表归一化为新顺序，再逐步淡化 `priority`。

性能与可靠性：

- 当前 `StrategyEvaluator.Evaluate` 每次评估都会筛选、排序并创建新列表，复杂度约为 O(n log n)。树形改造后应在配置加载时做一次归一化，运行期按节点顺序递归求值，单次评估控制在 O(n)。
- `PreviewEngine` 现在已采用检查点而非逐分钟扫描；改造后应继续保留该思路，但边界采集与实际求值必须复用同一套规则遍历，避免预览和实时切换出现分叉。
- 主要风险不在性能，而在语义不一致导致的错误切换，因此优先级最高的是“单一求值源 + 严格校验 + 安全降级”。

## Implementation Notes

- 复用 `LoggerService`，只记录节点 Id、默认回退原因、配置校验摘要，避免打印整份配置。
- 对无效 `default`、循环节点、空条件组使用安全降级，不阻塞应用启动。
- 先收敛模型和求值链，再改设置界面；避免同一批次同时引入模型、引擎、编辑器三套未稳定语义。

## Architecture Design

- `ConfigService.Load` 读取配置后，先做规则树归一化与校验。
- `StrategyEvaluator` 负责唯一的策略求值与默认回退判定。
- `AppController` 只负责状态优先级与最终计划切换。
- `PreviewEngine` 只负责时间线采样，并复用同一求值入口。
- `SettingsWindow` 只负责编辑规则节点与默认项，不再自行定义求值规则。

## Directory Structure

整体上是“模型升级 + 单一求值引擎 + 调用方收敛 + UI/测试补齐”的改造，优先复用现有目录与文件。

- `src/AutoPower.Core/Core/Models/StrategyRule.cs` [MODIFY]：将现有扁平规则模型扩展为可表达节点类型、条件组、子节点与兼容字段的统一策略节点；实现要求是尽量保留旧字段命名，降低旧 JSON 迁移成本。
- `src/AutoPower.Core/Core/Models/AppConfig.cs` [MODIFY]：承载新的规则树与默认回退定义；实现要求是缺省值安全、缺字段时仍可加载。
- `src/AutoPower.Core/Core/Models/AppConfigJsonContext.cs` [MODIFY]：补全新模型的 SourceGen 元数据；实现要求是继续满足 NativeAOT，无反射兜底。
- `src/AutoPower.Core/Strategy/StrategyEvaluator.cs` [MODIFY]：从“扁平列表找第一条”升级为“树形节点统一求值 + 校验 + 决策结果输出”；实现要求是运行期避免重复排序，并返回可复用于预览的结果信息。
- `src/AutoPower.Core/Core/AppController.cs` [MODIFY]：改为消费统一决策结果，并清晰落实 Override、规则树、`default`、Active/Idle 的优先级；实现要求是不改变现有手动覆盖生命周期。
- `src/AutoPower.Core/Strategy/PreviewEngine.cs` [MODIFY]：删除重复匹配逻辑，复用统一求值；实现要求是时间线说明文案与实时行为一致。
- `src/AutoPower/UI/SettingsWindow.cs` [MODIFY]：把当前扁平规则卡片改为分组/节点编辑，增加默认项选择与基本校验提示；实现要求是先保证稳定编辑，再考虑拖拽或高级交互。
- `src/AutoPower.Tests/Strategy/StrategyEvaluatorTests.cs` [MODIFY]：覆盖 `any/all/none`、默认回退、空组、循环防护、旧排序迁移等核心语义。
- `src/AutoPower.Tests/Core/ConfigSerializationTests.cs` [MODIFY]：覆盖旧配置读取、新配置往返序列化、缺省字段兼容。
- `src/AutoPower.Tests/Core/ConfigServiceTests.cs` [MODIFY]：覆盖加载失败、安全降级、无效默认引用与保存后重载行为。
- `README.md` [MODIFY]：更新配置示例与规则语义说明，避免新旧配置格式混淆。

## Agent Extensions

### SubAgent

- **code-explorer**
- Purpose: 在重构前后复核规则模型、序列化、求值、UI、测试的所有调用点，避免遗漏扁平规则假设
- Expected outcome: 形成完整影响面清单，并在实现完成后确认不再残留旧逻辑分叉