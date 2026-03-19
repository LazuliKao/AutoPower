---
name: rule-tree-refactor-plan
overview: 为 AutoPower 的策略系统制定一份以实施为导向的重构计划：将扁平 `StrategyRule` 升级为树形规则节点，统一默认回退与求值入口，并分阶段改造配置、引擎、UI 与测试。
todos:
  - id: freeze-rule-contract
    content: 使用 [subagent:code-explorer] 固化规则树语义与兼容边界
    status: in_progress
  - id: refactor-config-model
    content: 重构 StrategyRule、AppConfig、ConfigService 与 JsonContext
    status: pending
    dependencies:
      - freeze-rule-contract
  - id: unify-decision-flow
    content: 统一 StrategyEvaluator、AppController、PreviewEngine 决策链路
    status: pending
    dependencies:
      - refactor-config-model
  - id: upgrade-settings-window
    content: 改造 SettingsWindow 支持条件组与默认项编辑
    status: pending
    dependencies:
      - unify-decision-flow
  - id: verify-regression
    content: 使用 [subagent:code-explorer] 补齐测试、README 与回归验证
    status: pending
    dependencies:
      - unify-decision-flow
      - upgrade-settings-window
---

## User Requirements

- 将当前基于时间段的扁平规则，重构为可表达层级布尔逻辑的规则树；叶子节点为 `plan`，条件节点支持 `any`、`all`、`none`。
- 为未命中任何规则的场景提供统一默认出口，并保留现有安全回退，避免运行时出现无结果或行为不一致。
- 规则判断、实时切换、预览时间线、手动覆盖之间应保持一致，避免同一配置在不同入口得到不同结果。
- 需要兼容旧配置，处理空条件组、无效默认引用、重复或悬挂节点、循环嵌套等异常情况。

## Product Overview

- 当前应用会按日期与时间窗口自动切换电源计划；重构后，规则可从“单条时间规则”升级为“可嵌套组合的条件规则”。
- 配置层会增加默认结果与规则树结构，用户可以表达更复杂的命中关系。
- 设置界面中的规则区域将从线性卡片列表演进为分组化、层级化编辑，视觉上更接近树状规则面板，但仍保持现有卡片式编辑体验。

## Core Features

- 支持 `plan` 节点与 `any/all/none` 条件组的组合求值
- 支持统一默认命中结果与最终安全回退
- 支持旧配置自动兼容与加载时校验归一化
- 支持预览与运行时共用同一套规则判断结果

## Tech Stack Selection

- 运行时：`.NET 10` / `C#`
- 配置序列化：`System.Text.Json` Source Generation
- 桌面界面：`MewUI`
- 测试：`xUnit`
- 约束：NativeAOT、无反射、静态 `ConfigService` / `LoggerService`

## Implementation Approach

采用“统一节点模型 + 单一决策结果 + 加载期归一化”的重构策略。现有 `StrategyEvaluator`、`AppController`、`PreviewEngine` 都依赖扁平 `StrategyRule` 假设，且未命中回退并不一致，因此核心目标不是单纯改 JSON，而是把“配置结构、求值链路、默认出口、预览说明”统一到同一份语义上。

关键决策：

1. 保留 `StrategyRule` 作为统一节点 record，在现有字段基础上扩展 `Type`、`Condition`、`Rules` 等字段，旧 JSON 缺少 `type` 时按 `plan` 节点处理，减少迁移成本。
2. `AppConfig` 增加默认规则定义，优先兼容用户的 `default.id` 思路，但内部只允许引用可直接产出计划的 `plan` 节点；条件节点不允许作为默认目标。
3. `ConfigService.Load()` 增加归一化与校验流程，在加载期完成旧结构兼容、非法默认引用清理、空组与循环检测、安全降级，避免运行期重复检查。
4. `StrategyEvaluator` 输出统一“决策结果”，至少包含目标计划 GUID、来源说明、命中节点 Id、是否来自默认出口，供 `AppController` 与 `PreviewEngine` 复用。
5. 规则求值顺序保持兼容链路：`Override → 规则树命中 → default → Active/Idle 回退`。这样即使新规则无效，也不会破坏现有基本行为。

性能与可靠性：

- 当前 `StrategyEvaluator` 每次评估都有筛选与排序，复杂度约为 `O(n log n)`；重构后在加载期归一化节点顺序，运行期递归遍历一次即可，单次评估控制在 `O(n)`。
- `PreviewEngine` 当前通过检查点采样避免逐分钟扫描；重构后保留该思路，但检查点计算与命中判断必须复用同一套规则求值入口，避免预览与实时行为分叉。
- 主要瓶颈不在算力，而在语义歧义；因此优先级最高的是冻结节点语义、默认规则约束和失败降级策略。

## Implementation Notes

- 复用 `LoggerService`，只记录节点 Id、默认出口来源、配置校验摘要，不输出整份配置 JSON。
- `ConfigService.Load()` 对无效默认引用、重复 Id、循环结构执行安全降级，不阻塞启动；保存时继续写出归一化后的合法结构。
- 避免引入运行时多态反序列化黑箱，所有新字段都显式纳入 `AppConfigJsonContext`。
- 优先复用现有文件与目录，先稳定模型和求值，再扩展 `SettingsWindow` 编辑体验，避免一次性扩大改动面。

## Architecture Design

- `ConfigService`
- 负责加载、序列化、归一化、校验配置
- 将旧扁平规则兼容为新节点结构
- `StrategyEvaluator`
- 负责唯一的规则树求值与默认出口判定
- 向调用方输出统一决策结果
- `AppController`
- 只负责组合 Override、规则结果与 Active/Idle 回退，并执行切换
- `PreviewEngine`
- 只负责按检查点生成时间线，复用 `StrategyEvaluator`
- `SettingsWindow`
- 负责编辑规则节点树、默认项与基础校验提示，不再自行定义求值语义

## Directory Structure

## Directory Structure Summary

本次重构以“模型升级、求值统一、调用方收敛、UI 编辑器扩展、测试补齐”为主，优先复用现有文件，减少 NativeAOT 风险与无关改动。

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Core/Models/StrategyRule.cs`  `[MODIFY]`
- 目的：把当前扁平规则扩展为统一节点模型
- 内容：增加节点类型、条件组、子节点、兼容字段
- 要求：旧配置缺少 `type` 时可按 `plan` 加载

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Core/Models/AppConfig.cs`  `[MODIFY]`
- 目的：承载规则树与默认出口
- 内容：扩展 `Rules` 与 `Default` 配置
- 要求：缺省值安全，兼容旧配置

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Core/Models/AppConfigJsonContext.cs`  `[MODIFY]`
- 目的：补全 SourceGen 元数据
- 内容：显式注册新模型结构
- 要求：保持 NativeAOT 兼容，无反射兜底

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Core/ConfigService.cs`  `[MODIFY]`
- 目的：在加载时完成归一化与校验
- 内容：兼容旧结构、清理非法默认引用、处理循环与空组
- 要求：失败时安全降级并记录日志

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Strategy/StrategyEvaluator.cs`  `[MODIFY]`
- 目的：从扁平筛选升级为规则树统一求值
- 内容：输出统一决策结果并复用既有时间判断规则
- 要求：运行期避免重复排序与重复分配

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Core/AppController.cs`  `[MODIFY]`
- 目的：消费统一决策结果
- 内容：落实 `Override → 规则树 → default → Active/Idle` 链路
- 要求：不改变现有手动覆盖生命周期

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Core/Strategy/PreviewEngine.cs`  `[MODIFY]`
- 目的：移除重复匹配逻辑
- 内容：复用统一求值入口与来源说明
- 要求：时间线与实时切换结果一致

- `a:/Documents/GitHub/AutoPower/src/AutoPower/UI/SettingsWindow.cs`  `[MODIFY]`
- 目的：支持树形节点与默认项编辑
- 内容：从扁平规则卡片扩展为分组节点编辑
- 要求：先保证稳定增删改与基础校验，再考虑高级交互

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Tests/Strategy/StrategyEvaluatorTests.cs`  `[MODIFY]`
- 目的：覆盖核心语义
- 内容：补充 `any/all/none`、默认出口、空组、循环、兼容排序场景
- 要求：保持现有测试风格与命名方式

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Tests/Core/ConfigSerializationTests.cs`  `[MODIFY]`
- 目的：验证新旧配置序列化兼容
- 内容：旧 JSON 读取、新结构往返、缺省字段兼容
- 要求：继续使用 `AppConfigJsonContext`

- `a:/Documents/GitHub/AutoPower/src/AutoPower.Tests/Core/ConfigServiceTests.cs`  `[MODIFY]`
- 目的：验证加载与保存的安全降级行为
- 内容：非法默认引用、损坏配置、归一化后保存重载
- 要求：避免扩大到真实外部依赖

- `a:/Documents/GitHub/AutoPower/README.md`  `[MODIFY]`
- 目的：更新配置示例与规则说明
- 内容：补充规则树、默认出口、兼容说明
- 要求：避免新旧格式混淆

## Agent Extensions

### SubAgent

- **code-explorer**
- Purpose: 复核规则模型、序列化、求值链路、UI 与测试影响面，确认所有扁平规则假设都被收敛
- Expected outcome: 形成完整改动边界清单，并在重构完成前后验证不存在遗漏调用点或残留分叉逻辑