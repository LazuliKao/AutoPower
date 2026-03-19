using AutoPower.Core.Core.Models;
using AutoPower.Core.Core;
using AutoPower.Core.Strategy;

namespace AutoPower.Tests.Strategy;

public class StrategyEvaluatorTests
{
    private static readonly Guid ActivePlanGuid = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid IdlePlanGuid = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly DateTime TestMonday = new(2025, 6, 2, 12, 0, 0);
    private static readonly DateTime TestSaturday = new(2025, 6, 7, 12, 0, 0);

    private static AppConfig CreateConfig(
        IReadOnlyList<StrategyRule>? rules = null,
        Guid? defaultPlanGuid = null,
        DetectionMode mode = DetectionMode.Both
    )
    {
        return new()
        {
            Mode = mode,
            ActivePlanGuid = ActivePlanGuid,
            IdlePlanGuid = IdlePlanGuid,
            DefaultPlanGuid = defaultPlanGuid,
            Rules = rules?.ToList() ?? new(),
        };
    }

    private static StrategyEvaluationContext CreateContext(
        DateTime now,
        DetectionMode mode = DetectionMode.Both,
        bool? isKeyboardMouseIdle = null,
        bool? isMonitorOff = null
    )
    {
        return new()
        {
            Now = now,
            IsKeyboardMouseDetectionEnabled = mode is DetectionMode.KeyboardMouse or DetectionMode.Both,
            IsMonitorDetectionEnabled = mode is DetectionMode.MonitorSleep or DetectionMode.Both,
            IsKeyboardMouseIdle = isKeyboardMouseIdle,
            IsMonitorOff = isMonitorOff,
        };
    }

    private static StrategyRule CreateRule(
        Guid? id = null,
        string name = "Test Rule",
        StrategyConditionGroup? condition = null,
        Guid? targetPlanGuid = null,
        int priority = 0,
        DateTime? createdAt = null,
        bool isEnabled = true
    )
    {
        return new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Condition = condition ?? StrategyConditionGroup.MatchAll(),
            TargetPlanGuid = targetPlanGuid ?? Guid.NewGuid(),
            Priority = priority,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            IsEnabled = isEnabled,
        };
    }

    private static StrategyCondition Day(DayType dayType)
    {
        return new() { Type = StrategyConditionType.DayType, DayType = dayType };
    }

    private static StrategyCondition TimeRange(TimeOnly start, TimeOnly end)
    {
        return new() { Type = StrategyConditionType.TimeRange, Start = start, End = end };
    }

    private static StrategyCondition KeyboardMouseIdle()
    {
        return new() { Type = StrategyConditionType.KeyboardMouseIdle };
    }

    private static StrategyCondition MonitorOff()
    {
        return new() { Type = StrategyConditionType.MonitorOff };
    }

    private static StrategyConditionGroup Group(
        StrategyConditionGroupOperator @operator,
        IEnumerable<StrategyCondition>? conditions = null,
        IEnumerable<StrategyConditionGroup>? groups = null
    )
    {
        return new()
        {
            Operator = @operator,
            Conditions = conditions?.ToList() ?? new(),
            Groups = groups?.ToList() ?? new(),
        };
    }

    [Fact]
    public void NoRules_NoDefault_ReturnsActiveFallback()
    {
        var result = StrategyEvaluator.Resolve(CreateConfig(), CreateContext(TestMonday));

        Assert.Equal(ActivePlanGuid, result.PlanGuid);
        Assert.True(result.IsFallback);
        Assert.Equal(AppState.Active, result.State);
    }

    [Fact]
    public void DefaultPlan_IsUsedWhenNoRuleMatches()
    {
        var defaultPlanGuid = Guid.NewGuid();
        var rules = new List<StrategyRule>
        {
            CreateRule(condition: Group(StrategyConditionGroupOperator.All, new[] { Day(DayType.Weekend) })),
        };

        var result = StrategyEvaluator.Resolve(
            CreateConfig(rules, defaultPlanGuid: defaultPlanGuid),
            CreateContext(TestMonday)
        );

        Assert.Equal(defaultPlanGuid, result.PlanGuid);
        Assert.True(result.IsDefault);
        Assert.False(result.IsFallback);
    }

    [Fact]
    public void SingleMatchingRule_ReturnsItsPlan()
    {
        var targetGuid = Guid.NewGuid();
        var rules = new List<StrategyRule>
        {
            CreateRule(
                name: "Work Hours",
                targetPlanGuid: targetGuid,
                condition: Group(
                    StrategyConditionGroupOperator.All,
                    new[] { Day(DayType.All), TimeRange(new(9, 0), new(17, 0)) }
                )
            ),
        };

        var result = StrategyEvaluator.Resolve(CreateConfig(rules), CreateContext(TestMonday));

        Assert.Equal(targetGuid, result.PlanGuid);
        Assert.Equal("Rule: Work Hours", result.Source);
        Assert.False(result.IsFallback);
    }

    [Fact]
    public void DisabledRule_IsSkipped()
    {
        var result = StrategyEvaluator.Resolve(
            CreateConfig(
                new List<StrategyRule>
                {
                    CreateRule(isEnabled: false, condition: Group(StrategyConditionGroupOperator.All, new[] { Day(DayType.All) })),
                }
            ),
            CreateContext(TestMonday)
        );

        Assert.Equal(ActivePlanGuid, result.PlanGuid);
        Assert.True(result.IsFallback);
    }

    [Fact]
    public void AnyGroup_MatchesWhenOneChildMatches()
    {
        var targetGuid = Guid.NewGuid();
        var rule = CreateRule(
            targetPlanGuid: targetGuid,
            condition: Group(
                StrategyConditionGroupOperator.Any,
                new[] { Day(DayType.Weekend), KeyboardMouseIdle() }
            )
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(new List<StrategyRule> { rule }),
            CreateContext(TestMonday, isKeyboardMouseIdle: true)
        );

        Assert.Equal(targetGuid, result.PlanGuid);
        Assert.True(result.IsRuntimeDependent);
    }

    [Fact]
    public void AllGroup_RequiresAllChildrenToMatch()
    {
        var targetGuid = Guid.NewGuid();
        var rule = CreateRule(
            targetPlanGuid: targetGuid,
            condition: Group(
                StrategyConditionGroupOperator.All,
                new[] { Day(DayType.Weekday), KeyboardMouseIdle() }
            )
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(new List<StrategyRule> { rule }),
            CreateContext(TestMonday, isKeyboardMouseIdle: true)
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void NoneGroup_MatchesWhenAllChildrenAreFalse()
    {
        var targetGuid = Guid.NewGuid();
        var rule = CreateRule(
            targetPlanGuid: targetGuid,
            condition: Group(
                StrategyConditionGroupOperator.None,
                new[] { Day(DayType.Weekend), MonitorOff() }
            )
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(new List<StrategyRule> { rule }),
            CreateContext(TestMonday, isMonitorOff: false)
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void DisabledDetector_ProducesUnknownAndDoesNotMatchNoneGroup()
    {
        var targetGuid = Guid.NewGuid();
        var rule = CreateRule(
            targetPlanGuid: targetGuid,
            condition: Group(StrategyConditionGroupOperator.None, new[] { MonitorOff() })
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(new List<StrategyRule> { rule }, mode: DetectionMode.KeyboardMouse),
            CreateContext(TestMonday, mode: DetectionMode.KeyboardMouse, isKeyboardMouseIdle: false)
        );

        Assert.Equal(ActivePlanGuid, result.PlanGuid);
        Assert.True(result.IsFallback);
    }

    [Fact]
    public void OvernightTimeRange_MatchesAfterMidnight()
    {
        var targetGuid = Guid.NewGuid();
        var rule = CreateRule(
            targetPlanGuid: targetGuid,
            condition: Group(
                StrategyConditionGroupOperator.All,
                new[] { TimeRange(new(22, 0), new(6, 0)) }
            )
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(new List<StrategyRule> { rule }),
            CreateContext(new DateTime(2025, 6, 2, 2, 0, 0))
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void HigherPriority_Wins()
    {
        var lowPriorityGuid = Guid.NewGuid();
        var highPriorityGuid = Guid.NewGuid();
        var rules = new List<StrategyRule>
        {
            CreateRule(
                targetPlanGuid: lowPriorityGuid,
                priority: 1,
                createdAt: DateTime.UtcNow.AddMinutes(-1)
            ),
            CreateRule(
                targetPlanGuid: highPriorityGuid,
                priority: 10,
                createdAt: DateTime.UtcNow.AddMinutes(-2)
            ),
        };

        var result = StrategyEvaluator.Resolve(CreateConfig(rules), CreateContext(TestMonday));

        Assert.Equal(highPriorityGuid, result.PlanGuid);
    }

    [Fact]
    public void SamePriority_EarlierCreatedAt_Wins()
    {
        var earlierGuid = Guid.NewGuid();
        var laterGuid = Guid.NewGuid();
        var rules = new List<StrategyRule>
        {
            CreateRule(targetPlanGuid: laterGuid, priority: 5, createdAt: DateTime.UtcNow),
            CreateRule(
                targetPlanGuid: earlierGuid,
                priority: 5,
                createdAt: DateTime.UtcNow.AddMinutes(-1)
            ),
        };

        var result = StrategyEvaluator.Resolve(CreateConfig(rules), CreateContext(TestMonday));

        Assert.Equal(earlierGuid, result.PlanGuid);
    }

    [Fact]
    public void SamePriority_SameCreatedAt_GuidSort_Wins()
    {
        var earlierGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var laterGuid = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var now = new DateTime(2025, 6, 2, 12, 0, 0);
        var rules = new List<StrategyRule>
        {
            CreateRule(id: laterGuid, targetPlanGuid: laterGuid, priority: 5, createdAt: now),
            CreateRule(id: earlierGuid, targetPlanGuid: earlierGuid, priority: 5, createdAt: now),
        };

        var result = StrategyEvaluator.Resolve(CreateConfig(rules), CreateContext(TestMonday));

        Assert.Equal(earlierGuid, result.PlanGuid);
    }

    [Fact]
    public void IdleFallback_UsesIdlePlan_WhenEnabledDetectorReportsIdle()
    {
        var result = StrategyEvaluator.Resolve(
            CreateConfig(mode: DetectionMode.Both),
            CreateContext(TestMonday, isKeyboardMouseIdle: true)
        );

        Assert.Equal(IdlePlanGuid, result.PlanGuid);
        Assert.Equal(AppState.Idle, result.State);
    }

    [Fact]
    public void EmptyAllGroup_MatchesTrue()
    {
        var targetGuid = Guid.NewGuid();
        var rule = CreateRule(
            targetPlanGuid: targetGuid,
            condition: Group(StrategyConditionGroupOperator.All)
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(new List<StrategyRule> { rule }),
            CreateContext(TestMonday)
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void EmptyAnyGroup_MatchesFalse()
    {
        var targetGuid = Guid.NewGuid();
        var rule = CreateRule(
            targetPlanGuid: targetGuid,
            condition: Group(StrategyConditionGroupOperator.Any)
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(new List<StrategyRule> { rule }),
            CreateContext(TestMonday)
        );

        Assert.Equal(ActivePlanGuid, result.PlanGuid);
        Assert.True(result.IsFallback);
    }

    [Fact]
    public void EmptyNoneGroup_MatchesTrue()
    {
        var targetGuid = Guid.NewGuid();
        var rule = CreateRule(
            targetPlanGuid: targetGuid,
            condition: Group(StrategyConditionGroupOperator.None)
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(new List<StrategyRule> { rule }),
            CreateContext(TestMonday)
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void NestedGroups_AnyInsideAll_EvaluatesCorrectly()
    {
        var targetGuid = Guid.NewGuid();
        var rule = CreateRule(
            targetPlanGuid: targetGuid,
            condition: Group(
                StrategyConditionGroupOperator.All,
                new[] { Day(DayType.Weekday) },
                new[] { Group(StrategyConditionGroupOperator.Any, new[] { KeyboardMouseIdle() }) }
            )
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(new List<StrategyRule> { rule }),
            CreateContext(TestMonday, isKeyboardMouseIdle: true)
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void NestedGroups_DeepNesting_EvaluatesCorrectly()
    {
        var targetGuid = Guid.NewGuid();
        var deepGroup = Group(
            StrategyConditionGroupOperator.Any,
            new[] { MonitorOff() },
            new[] {
                Group(
                    StrategyConditionGroupOperator.All,
                    new[] { TimeRange(new(9, 0), new(17, 0)) }
                )
            }
        );

        var rule = CreateRule(
            targetPlanGuid: targetGuid,
            condition: Group(
                StrategyConditionGroupOperator.All,
                new[] { Day(DayType.All) },
                new[] { deepGroup }
            )
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(new List<StrategyRule> { rule }),
            CreateContext(TestMonday, isMonitorOff: false)
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void UnknownPropagation_AllGroupWithUnknown_BecomesUnknown()
    {
        var targetGuid = Guid.NewGuid();
        var rule = CreateRule(
            targetPlanGuid: targetGuid,
            condition: Group(
                StrategyConditionGroupOperator.All,
                new[] { Day(DayType.All), KeyboardMouseIdle() }
            )
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(new List<StrategyRule> { rule }, mode: DetectionMode.MonitorSleep),
            CreateContext(TestMonday, mode: DetectionMode.MonitorSleep, isKeyboardMouseIdle: null)
        );

        Assert.Equal(ActivePlanGuid, result.PlanGuid);
        Assert.True(result.IsFallback);
    }

    [Fact]
    public void UnknownPropagation_AnyGroupWithTrueIgnoresUnknown()
    {
        var targetGuid = Guid.NewGuid();
        var rule = CreateRule(
            targetPlanGuid: targetGuid,
            condition: Group(
                StrategyConditionGroupOperator.Any,
                new[] { KeyboardMouseIdle(), Day(DayType.All) }
            )
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(new List<StrategyRule> { rule }, mode: DetectionMode.MonitorSleep),
            CreateContext(TestMonday, mode: DetectionMode.MonitorSleep, isKeyboardMouseIdle: null)
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void NoneGroupWithUnknown_StaysFalseIfAnyTrue()
    {
        var targetGuid = Guid.NewGuid();
        var rule = CreateRule(
            targetPlanGuid: targetGuid,
            condition: Group(
                StrategyConditionGroupOperator.None,
                new[] { Day(DayType.All), KeyboardMouseIdle() }
            )
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(new List<StrategyRule> { rule }, mode: DetectionMode.MonitorSleep),
            CreateContext(TestMonday, mode: DetectionMode.MonitorSleep, isKeyboardMouseIdle: null)
        );

        Assert.Equal(ActivePlanGuid, result.PlanGuid);
        Assert.True(result.IsFallback);
    }

    [Fact]
    public void OrphanGroupRemoved_EmptyGroupNotMatched()
    {
        var targetGuid = Guid.NewGuid();
        var rule = CreateRule(
            targetPlanGuid: targetGuid,
            condition: Group(
                StrategyConditionGroupOperator.All,
                null,
                new[] { Group(StrategyConditionGroupOperator.Any) }
            )
        );

        var config = new AppConfig { Rules = new() { rule } };
        var normalized = ConfigService.Load();

        var result = StrategyEvaluator.Resolve(
            new AppConfig
            {
                ActivePlanGuid = ActivePlanGuid,
                IdlePlanGuid = IdlePlanGuid,
                Rules = config.Rules,
            },
            CreateContext(TestMonday)
        );

        Assert.False(result.IsFallback && result.PlanGuid == targetGuid);
    }
}
