#nullable enable

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
        StrategyDecisionNode? decisionTree = null,
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
            DecisionTree = decisionTree,
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

    // Helper: Create a leaf node with a plan
    private static StrategyDecisionNode Leaf(Guid planGuid, bool isEnabled = true)
    {
        return new() { PlanGuid = planGuid, IsEnabled = isEnabled };
    }

    // Helper: Create a branch node with IF-THEN-ELSE
    private static StrategyDecisionNode Branch(
        StrategyConditionGroup? condition,
        StrategyDecisionNode? thenNode = null,
        StrategyDecisionNode? elseNode = null,
        bool isEnabled = true
    )
    {
        return new() { If = condition, Then = thenNode, Else = elseNode, IsEnabled = isEnabled };
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
    public void NoTree_NoDefault_ReturnsActiveFallback()
    {
        var result = StrategyEvaluator.Resolve(CreateConfig(), CreateContext(TestMonday));

        Assert.Equal(ActivePlanGuid, result.PlanGuid);
        Assert.True(result.IsFallback);
        Assert.Equal(AppState.Active, result.State);
    }

    [Fact]
    public void DefaultPlan_IsUsedWhenNoTreeMatches()
    {
        var defaultPlanGuid = Guid.NewGuid();
        var tree = Branch(
            Group(StrategyConditionGroupOperator.All, new[] { Day(DayType.Weekend) }),
            thenNode: Leaf(Guid.NewGuid())
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree, defaultPlanGuid: defaultPlanGuid),
            CreateContext(TestMonday)
        );

        Assert.Equal(defaultPlanGuid, result.PlanGuid);
        Assert.True(result.IsDefault);
        Assert.False(result.IsFallback);
    }

    [Fact]
    public void SingleMatchingCondition_ReturnsItsPlan()
    {
        var targetGuid = Guid.NewGuid();
        var tree = Branch(
            Group(
                StrategyConditionGroupOperator.All,
                new[] { Day(DayType.All), TimeRange(new(9, 0), new(17, 0)) }
            ),
            thenNode: Leaf(targetGuid)
        );

        var result = StrategyEvaluator.Resolve(CreateConfig(tree), CreateContext(TestMonday));

        Assert.Equal(targetGuid, result.PlanGuid);
        Assert.Equal("Decision Tree", result.Source);
        Assert.False(result.IsFallback);
    }

    [Fact]
    public void DisabledNode_IsSkipped()
    {
        var targetGuid = Guid.NewGuid();
        var tree = Branch(
            Group(StrategyConditionGroupOperator.All, new[] { Day(DayType.All) }),
            thenNode: Leaf(targetGuid),
            isEnabled: false
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday)
        );

        Assert.Equal(ActivePlanGuid, result.PlanGuid);
        Assert.True(result.IsFallback);
    }

    [Fact]
    public void AnyGroup_MatchesWhenOneChildMatches()
    {
        var targetGuid = Guid.NewGuid();
        var tree = Branch(
            Group(
                StrategyConditionGroupOperator.Any,
                new[] { Day(DayType.Weekend), KeyboardMouseIdle() }
            ),
            thenNode: Leaf(targetGuid)
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday, isKeyboardMouseIdle: true)
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void AllGroup_RequiresAllChildrenToMatch()
    {
        var targetGuid = Guid.NewGuid();
        var tree = Branch(
            Group(
                StrategyConditionGroupOperator.All,
                new[] { Day(DayType.Weekday), KeyboardMouseIdle() }
            ),
            thenNode: Leaf(targetGuid)
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday, isKeyboardMouseIdle: true)
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void NoneGroup_MatchesWhenAllChildrenAreFalse()
    {
        var targetGuid = Guid.NewGuid();
        var tree = Branch(
            Group(
                StrategyConditionGroupOperator.None,
                new[] { Day(DayType.Weekend), MonitorOff() }
            ),
            thenNode: Leaf(targetGuid)
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday, isMonitorOff: false)
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void DisabledDetector_ProducesUnknownAndDoesNotMatchNoneGroup()
    {
        var targetGuid = Guid.NewGuid();
        var tree = Branch(
            Group(StrategyConditionGroupOperator.None, new[] { MonitorOff() }),
            thenNode: Leaf(targetGuid)
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree, mode: DetectionMode.KeyboardMouse),
            CreateContext(TestMonday, mode: DetectionMode.KeyboardMouse, isKeyboardMouseIdle: false)
        );

        Assert.Equal(ActivePlanGuid, result.PlanGuid);
        Assert.True(result.IsFallback);
    }

    [Fact]
    public void OvernightTimeRange_MatchesAfterMidnight()
    {
        var targetGuid = Guid.NewGuid();
        var tree = Branch(
            Group(
                StrategyConditionGroupOperator.All,
                new[] { TimeRange(new(22, 0), new(6, 0)) }
            ),
            thenNode: Leaf(targetGuid)
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(new DateTime(2025, 6, 2, 2, 0, 0))
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void IfThenElse_ConditionFalse_EvaluatesElse()
    {
        var trueGuid = Guid.NewGuid();
        var falseGuid = Guid.NewGuid();
        var tree = Branch(
            Group(StrategyConditionGroupOperator.All, new[] { Day(DayType.Weekend) }),
            thenNode: Leaf(trueGuid),
            elseNode: Leaf(falseGuid)
        );

        var result = StrategyEvaluator.Resolve(CreateConfig(tree), CreateContext(TestMonday));

        Assert.Equal(falseGuid, result.PlanGuid);
    }

    [Fact]
    public void IfThenElse_ConditionTrue_EvaluatesThen()
    {
        var trueGuid = Guid.NewGuid();
        var falseGuid = Guid.NewGuid();
        var tree = Branch(
            Group(StrategyConditionGroupOperator.All, new[] { Day(DayType.Weekday) }),
            thenNode: Leaf(trueGuid),
            elseNode: Leaf(falseGuid)
        );

        var result = StrategyEvaluator.Resolve(CreateConfig(tree), CreateContext(TestMonday));

        Assert.Equal(trueGuid, result.PlanGuid);
    }

    [Fact]
    public void NestedBranch_ThreeLevelsDeep_EvaluatesCorrectly()
    {
        var deepestGuid = Guid.NewGuid();
        var tree = Branch(
            Group(StrategyConditionGroupOperator.All, new[] { Day(DayType.Weekday) }),
            thenNode: Branch(
                Group(StrategyConditionGroupOperator.All, new[] { TimeRange(new(9, 0), new(17, 0)) }),
                thenNode: Branch(
                    Group(StrategyConditionGroupOperator.All, new[] { KeyboardMouseIdle() }),
                    thenNode: Leaf(deepestGuid)
                )
            )
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday, isKeyboardMouseIdle: true)
        );

        Assert.Equal(deepestGuid, result.PlanGuid);
    }

    [Fact]
    public void NestedBranch_WithElsePath_EvaluatesCorrectly()
    {
        var weekdayIdleGuid = Guid.NewGuid();
        var weekdayActiveGuid = Guid.NewGuid();
        var weekendGuid = Guid.NewGuid();

        var tree = Branch(
            Group(StrategyConditionGroupOperator.All, new[] { Day(DayType.Weekday) }),
            thenNode: Branch(
                Group(StrategyConditionGroupOperator.All, new[] { KeyboardMouseIdle() }),
                thenNode: Leaf(weekdayIdleGuid),
                elseNode: Leaf(weekdayActiveGuid)
            ),
            elseNode: Leaf(weekendGuid)
        );

        // Weekday + Idle -> weekdayIdleGuid
        var result1 = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday, isKeyboardMouseIdle: true)
        );
        Assert.Equal(weekdayIdleGuid, result1.PlanGuid);

        // Weekday + Not Idle -> weekdayActiveGuid
        var result2 = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday, isKeyboardMouseIdle: false)
        );
        Assert.Equal(weekdayActiveGuid, result2.PlanGuid);

        // Weekend -> weekendGuid
        var result3 = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestSaturday)
        );
        Assert.Equal(weekendGuid, result3.PlanGuid);
    }

    [Fact]
    public void IdleFallback_UsesIdlePlan_WhenEnabledDetectorReportsIdle()
    {
        var result = StrategyEvaluator.Resolve(
            CreateConfig(),
            CreateContext(TestMonday, isKeyboardMouseIdle: true)
        );

        Assert.Equal(IdlePlanGuid, result.PlanGuid);
        Assert.Equal(AppState.Idle, result.State);
    }

    [Fact]
    public void EmptyAllGroup_MatchesTrue()
    {
        var targetGuid = Guid.NewGuid();
        var tree = Branch(
            Group(StrategyConditionGroupOperator.All),
            thenNode: Leaf(targetGuid)
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday)
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void EmptyAnyGroup_MatchesFalse()
    {
        var targetGuid = Guid.NewGuid();
        var tree = Branch(
            Group(StrategyConditionGroupOperator.Any),
            thenNode: Leaf(targetGuid)
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday)
        );

        // Any with no conditions is False -> no Then, no Else -> fallback
        Assert.Equal(ActivePlanGuid, result.PlanGuid);
        Assert.True(result.IsFallback);
    }

    [Fact]
    public void EmptyNoneGroup_MatchesTrue()
    {
        var targetGuid = Guid.NewGuid();
        var tree = Branch(
            Group(StrategyConditionGroupOperator.None),
            thenNode: Leaf(targetGuid)
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday)
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void NestedGroups_AnyInsideAll_EvaluatesCorrectly()
    {
        var targetGuid = Guid.NewGuid();
        var tree = Branch(
            Group(
                StrategyConditionGroupOperator.All,
                new[] { Day(DayType.Weekday) },
                new[] { Group(StrategyConditionGroupOperator.Any, new[] { KeyboardMouseIdle() }) }
            ),
            thenNode: Leaf(targetGuid)
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree),
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

        var tree = Branch(
            Group(
                StrategyConditionGroupOperator.All,
                new[] { Day(DayType.All) },
                new[] { deepGroup }
            ),
            thenNode: Leaf(targetGuid)
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday, isMonitorOff: false)
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void UnknownPropagation_AllGroupWithUnknown_BecomesUnknown()
    {
        var targetGuid = Guid.NewGuid();
        var tree = Branch(
            Group(
                StrategyConditionGroupOperator.All,
                new[] { Day(DayType.All), KeyboardMouseIdle() }
            ),
            thenNode: Leaf(targetGuid)
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree, mode: DetectionMode.MonitorSleep),
            CreateContext(TestMonday, mode: DetectionMode.MonitorSleep, isKeyboardMouseIdle: null)
        );

        Assert.Equal(ActivePlanGuid, result.PlanGuid);
        Assert.True(result.IsFallback);
    }

    [Fact]
    public void UnknownPropagation_AnyGroupWithTrueIgnoresUnknown()
    {
        var targetGuid = Guid.NewGuid();
        var tree = Branch(
            Group(
                StrategyConditionGroupOperator.Any,
                new[] { KeyboardMouseIdle(), Day(DayType.All) }
            ),
            thenNode: Leaf(targetGuid)
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree, mode: DetectionMode.MonitorSleep),
            CreateContext(TestMonday, mode: DetectionMode.MonitorSleep, isKeyboardMouseIdle: null)
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void NoneGroupWithUnknown_StaysFalseIfAnyTrue()
    {
        var targetGuid = Guid.NewGuid();
        var tree = Branch(
            Group(
                StrategyConditionGroupOperator.None,
                new[] { Day(DayType.All), KeyboardMouseIdle() }
            ),
            thenNode: Leaf(targetGuid)
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree, mode: DetectionMode.MonitorSleep),
            CreateContext(TestMonday, mode: DetectionMode.MonitorSleep, isKeyboardMouseIdle: null)
        );

        Assert.Equal(ActivePlanGuid, result.PlanGuid);
        Assert.True(result.IsFallback);
    }

    [Fact]
    public void NullCondition_AlwaysMatches()
    {
        var targetGuid = Guid.NewGuid();
        var tree = new StrategyDecisionNode
        {
            If = null,  // No condition = always match
            Then = Leaf(targetGuid)
        };

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday)
        );

        Assert.Equal(targetGuid, result.PlanGuid);
    }

    [Fact]
    public void BranchWithNoThen_ReturnsFallback()
    {
        var tree = Branch(
            Group(StrategyConditionGroupOperator.All, new[] { Day(DayType.All) }),
            thenNode: null  // No Then branch
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday)
        );

        Assert.Equal(ActivePlanGuid, result.PlanGuid);
        Assert.True(result.IsFallback);
    }

    [Fact]
    public void ConditionFalse_NoElse_ReturnsFallback()
    {
        var targetGuid = Guid.NewGuid();
        var tree = Branch(
            Group(StrategyConditionGroupOperator.All, new[] { Day(DayType.Weekend) }),
            thenNode: Leaf(targetGuid),
            elseNode: null  // No Else branch
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday)  // Monday, not weekend
        );

        Assert.Equal(ActivePlanGuid, result.PlanGuid);
        Assert.True(result.IsFallback);
    }

    [Fact]
    public void DeepNestedTree_FiveLevels_EvaluatesCorrectly()
    {
        var finalGuid = Guid.NewGuid();
        var tree = Branch(
            Group(StrategyConditionGroupOperator.All, new[] { Day(DayType.All) }),
            thenNode: Branch(
                Group(StrategyConditionGroupOperator.All, new[] { TimeRange(new(0, 0), new(23, 59)) }),
                thenNode: Branch(
                    Group(StrategyConditionGroupOperator.All, new[] { Day(DayType.All) }),
                    thenNode: Branch(
                        Group(StrategyConditionGroupOperator.All, new[] { TimeRange(new(0, 0), new(23, 59)) }),
                        thenNode: Branch(
                            Group(StrategyConditionGroupOperator.All, new[] { Day(DayType.All) }),
                            thenNode: Leaf(finalGuid)
                        )
                    )
                )
            )
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday)
        );

        Assert.Equal(finalGuid, result.PlanGuid);
    }

    [Fact]
    public void ChainedIfElse_MultipleConditions_SelectsCorrectPath()
    {
        var morningGuid = Guid.NewGuid();
        var afternoonGuid = Guid.NewGuid();
        var eveningGuid = Guid.NewGuid();
        var defaultGuid = Guid.NewGuid();

        // Decision tree: IF morning -> morningPlan, ELSE IF afternoon -> afternoonPlan, ELSE IF evening -> eveningPlan, ELSE -> defaultPlan
        var tree = Branch(
            Group(StrategyConditionGroupOperator.All, new[] { TimeRange(new(6, 0), new(12, 0)) }),
            thenNode: Leaf(morningGuid),
            elseNode: Branch(
                Group(StrategyConditionGroupOperator.All, new[] { TimeRange(new(12, 0), new(18, 0)) }),
                thenNode: Leaf(afternoonGuid),
                elseNode: Branch(
                    Group(StrategyConditionGroupOperator.All, new[] { TimeRange(new(18, 0), new(22, 0)) }),
                    thenNode: Leaf(eveningGuid),
                    elseNode: Leaf(defaultGuid)
                )
            )
        );

        // Morning (9 AM)
        var result1 = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(new DateTime(2025, 6, 2, 9, 0, 0))
        );
        Assert.Equal(morningGuid, result1.PlanGuid);

        // Afternoon (2 PM)
        var result2 = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(new DateTime(2025, 6, 2, 14, 0, 0))
        );
        Assert.Equal(afternoonGuid, result2.PlanGuid);

        // Evening (8 PM)
        var result3 = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(new DateTime(2025, 6, 2, 20, 0, 0))
        );
        Assert.Equal(eveningGuid, result3.PlanGuid);

        // Night (2 AM)
        var result4 = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(new DateTime(2025, 6, 2, 2, 0, 0))
        );
        Assert.Equal(defaultGuid, result4.PlanGuid);
    }

    [Fact]
    public void TreeWithMonitorOffCondition_EvaluatesCorrectly()
    {
        var monitorOffGuid = Guid.NewGuid();
        var activeGuid = Guid.NewGuid();

        var tree = Branch(
            Group(StrategyConditionGroupOperator.All, new[] { MonitorOff() }),
            thenNode: Leaf(monitorOffGuid),
            elseNode: Leaf(activeGuid)
        );

        // Monitor off
        var result1 = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday, isMonitorOff: true)
        );
        Assert.Equal(monitorOffGuid, result1.PlanGuid);

        // Monitor on
        var result2 = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday, isMonitorOff: false)
        );
        Assert.Equal(activeGuid, result2.PlanGuid);
    }

    [Fact]
    public void ComplexTree_WeekdayWorkHours_EvaluatesCorrectly()
    {
        var workPlanGuid = Guid.NewGuid();
        var idlePlanGuid = Guid.NewGuid();
        var defaultPlanGuid = Guid.NewGuid();

        // IF weekday AND work hours AND idle -> idlePlan
        // ELSE IF weekday AND work hours -> workPlan
        // ELSE -> defaultPlan
        var tree = Branch(
            Group(
                StrategyConditionGroupOperator.All,
                new[] { Day(DayType.Weekday), TimeRange(new(9, 0), new(17, 0)) }
            ),
            thenNode: Branch(
                Group(StrategyConditionGroupOperator.All, new[] { KeyboardMouseIdle() }),
                thenNode: Leaf(idlePlanGuid),
                elseNode: Leaf(workPlanGuid)
            ),
            elseNode: Leaf(defaultPlanGuid)
        );

        // Weekday + Work hours + Idle
        var result1 = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday, isKeyboardMouseIdle: true)
        );
        Assert.Equal(idlePlanGuid, result1.PlanGuid);

        // Weekday + Work hours + Not idle
        var result2 = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestMonday, isKeyboardMouseIdle: false)
        );
        Assert.Equal(workPlanGuid, result2.PlanGuid);

        // Weekend
        var result3 = StrategyEvaluator.Resolve(
            CreateConfig(tree),
            CreateContext(TestSaturday)
        );
        Assert.Equal(defaultPlanGuid, result3.PlanGuid);
    }

    [Fact]
    public void TreeIsNull_FallsBackToDefault()
    {
        var defaultPlanGuid = Guid.NewGuid();
        var result = StrategyEvaluator.Resolve(
            CreateConfig(decisionTree: null, defaultPlanGuid: defaultPlanGuid),
            CreateContext(TestMonday)
        );

        Assert.Equal(defaultPlanGuid, result.PlanGuid);
        Assert.True(result.IsDefault);
    }

    [Fact]
    public void TreeReturnsNull_FallsBackToDefault()
    {
        var defaultPlanGuid = Guid.NewGuid();
        // Disabled node returns null from evaluator
        var tree = Branch(
            Group(StrategyConditionGroupOperator.All, new[] { Day(DayType.All) }),
            thenNode: Leaf(Guid.NewGuid()),
            isEnabled: false
        );

        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree, defaultPlanGuid: defaultPlanGuid),
            CreateContext(TestMonday)
        );

        Assert.Equal(defaultPlanGuid, result.PlanGuid);
        Assert.True(result.IsDefault);
    }

    [Fact]
    public void UnknownCondition_ReturnsNullAndFallsBack()
    {
        var defaultPlanGuid = Guid.NewGuid();
        var tree = Branch(
            Group(StrategyConditionGroupOperator.All, new[] { KeyboardMouseIdle() }),
            thenNode: Leaf(Guid.NewGuid()),
            elseNode: Leaf(Guid.NewGuid())
        );

        // KeyboardMouseIdle is Unknown because detection is disabled
        var result = StrategyEvaluator.Resolve(
            CreateConfig(tree, mode: DetectionMode.MonitorSleep, defaultPlanGuid: defaultPlanGuid),
            CreateContext(TestMonday, mode: DetectionMode.MonitorSleep)
        );

        Assert.Equal(defaultPlanGuid, result.PlanGuid);
    }
}
