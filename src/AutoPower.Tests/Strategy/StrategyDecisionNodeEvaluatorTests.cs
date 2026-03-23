using AutoPower.Core.Core.Models;
using AutoPower.Core.Strategy;

namespace AutoPower.Tests.Strategy;

public class StrategyDecisionNodeEvaluatorTests
{
    private static readonly Guid PlanA = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid PlanB = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid PlanC = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly DateTime TestMonday = new(2025, 6, 2, 12, 0, 0);
    private static readonly DateTime TestSaturday = new(2025, 6, 7, 12, 0, 0);

    private static StrategyEvaluationContext CreateContext(
        DateTime now,
        bool? isKeyboardMouseIdle = null,
        bool? isMonitorOff = null
    ) => new()
    {
        Now = now,
        IsKeyboardMouseDetectionEnabled = true,
        IsMonitorDetectionEnabled = true,
        IsKeyboardMouseIdle = isKeyboardMouseIdle,
        IsMonitorOff = isMonitorOff,
    };

    private static StrategyDecisionNode Leaf(Guid planGuid, bool isEnabled = true) => new()
    {
        PlanGuid = planGuid,
        IsEnabled = isEnabled,
    };

    private static StrategyDecisionNode Branch(
        StrategyConditionGroup? condition,
        StrategyDecisionNode? thenNode = null,
        StrategyDecisionNode? elseNode = null,
        bool isEnabled = true
    ) => new()
    {
        If = condition,
        Then = thenNode,
        Else = elseNode,
        IsEnabled = isEnabled,
    };

    private static StrategyCondition Day(DayType dayType) => new()
    {
        Type = StrategyConditionType.DayType,
        DayType = dayType,
    };

    private static StrategyCondition KeyboardMouseIdle() => new()
    {
        Type = StrategyConditionType.KeyboardMouseIdle,
    };

    private static StrategyConditionGroup All(params StrategyCondition[] conditions) => new()
    {
        Operator = StrategyConditionGroupOperator.All,
        Conditions = conditions.ToList(),
    };

    [Fact]
    public void LeafNode_WithPlanGuid_ReturnsDecision()
    {
        var node = Leaf(PlanA);
        var context = CreateContext(TestMonday);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.NotNull(result);
        Assert.Equal(PlanA, result!.PlanGuid);
        Assert.Equal("Decision Tree", result.Source);
        Assert.False(result.IsRuntimeDependent);
    }

    [Fact]
    public void Node_WithIfTrue_EvaluatesThenBranch()
    {
        var node = Branch(
            condition: All(Day(DayType.Weekday)),
            thenNode: Leaf(PlanA),
            elseNode: Leaf(PlanB)
        );
        var context = CreateContext(TestMonday);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.NotNull(result);
        Assert.Equal(PlanA, result!.PlanGuid);
    }

    [Fact]
    public void Node_WithIfFalse_EvaluatesElseBranch()
    {
        var node = Branch(
            condition: All(Day(DayType.Weekday)),
            thenNode: Leaf(PlanA),
            elseNode: Leaf(PlanB)
        );
        var context = CreateContext(TestSaturday);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.NotNull(result);
        Assert.Equal(PlanB, result!.PlanGuid);
    }

    [Fact]
    public void Node_WithIfUnknown_ReturnsNull()
    {
        var node = Branch(
            condition: All(KeyboardMouseIdle()),
            thenNode: Leaf(PlanA),
            elseNode: Leaf(PlanB)
        );
        var context = CreateContext(TestMonday, isKeyboardMouseIdle: null);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.Null(result);
    }

    [Fact]
    public void Node_WithNullIf_EvaluatesThenDirectly()
    {
        var node = Branch(
            condition: null,
            thenNode: Leaf(PlanA),
            elseNode: Leaf(PlanB)
        );
        var context = CreateContext(TestMonday);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.NotNull(result);
        Assert.Equal(PlanA, result!.PlanGuid);
    }

    [Fact]
    public void NestedIfThenElse_ThreeLevelsDeep_EvaluatesCorrectly()
    {
        var node = Branch(
            condition: All(Day(DayType.Weekday)),
            thenNode: Branch(
                condition: All(Day(DayType.All)),
                thenNode: Leaf(PlanA),
                elseNode: Leaf(PlanB)
            ),
            elseNode: Leaf(PlanC)
        );
        var context = CreateContext(TestMonday);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.NotNull(result);
        Assert.Equal(PlanA, result!.PlanGuid);
    }

    [Fact]
    public void MultipleBranches_WithDifferentPlans_SelectsCorrectPath()
    {
        var node = Branch(
            condition: All(Day(DayType.Weekend)),
            thenNode: Leaf(PlanA),
            elseNode: Branch(
                condition: All(Day(DayType.Weekday)),
                thenNode: Leaf(PlanB),
                elseNode: Leaf(PlanC)
            )
        );
        var context = CreateContext(TestMonday);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.NotNull(result);
        Assert.Equal(PlanB, result!.PlanGuid);
    }

    [Fact]
    public void DisabledNode_ReturnsNull()
    {
        var node = Leaf(PlanA, isEnabled: false);
        var context = CreateContext(TestMonday);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.Null(result);
    }

    [Fact]
    public void NullNode_ReturnsNull()
    {
        var context = CreateContext(TestMonday);

        var result = StrategyDecisionNodeEvaluator.Evaluate(null, context);

        Assert.Null(result);
    }

    [Fact]
    public void BranchWithNullThen_ReturnsNull()
    {
        var node = Branch(
            condition: All(Day(DayType.All)),
            thenNode: null,
            elseNode: Leaf(PlanB)
        );
        var context = CreateContext(TestMonday);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.Null(result);
    }

    [Fact]
    public void BranchWithNullElse_WhenConditionFalse_ReturnsNull()
    {
        var node = Branch(
            condition: All(Day(DayType.Weekend)),
            thenNode: Leaf(PlanA),
            elseNode: null
        );
        var context = CreateContext(TestMonday);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.Null(result);
    }

    [Fact]
    public void DeepNesting_FiveLevels_EvaluatesCorrectly()
    {
        var node = Branch(
            condition: All(Day(DayType.All)),
            thenNode: Branch(
                condition: All(Day(DayType.All)),
                thenNode: Branch(
                    condition: All(Day(DayType.All)),
                    thenNode: Branch(
                        condition: All(Day(DayType.All)),
                        thenNode: Leaf(PlanA),
                        elseNode: Leaf(PlanB)
                    ),
                    elseNode: Leaf(PlanC)
                ),
                elseNode: null
            ),
            elseNode: null
        );
        var context = CreateContext(TestMonday);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.NotNull(result);
        Assert.Equal(PlanA, result!.PlanGuid);
    }

    [Fact]
    public void ComplexNestedConditions_MixedOperators_EvaluatesCorrectly()
    {
        var weekdayAndIdle = new StrategyConditionGroup
        {
            Operator = StrategyConditionGroupOperator.All,
            Conditions = new() { Day(DayType.Weekday), KeyboardMouseIdle() },
        };

        var node = Branch(
            condition: weekdayAndIdle,
            thenNode: Leaf(PlanA),
            elseNode: Leaf(PlanB)
        );
        var context = CreateContext(TestMonday, isKeyboardMouseIdle: true);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.NotNull(result);
        Assert.Equal(PlanA, result!.PlanGuid);
    }

    [Fact]
    public void RuntimeDependentCondition_True_ShortCircuitsToThen()
    {
        var node = Branch(
            condition: All(KeyboardMouseIdle()),
            thenNode: Leaf(PlanA),
            elseNode: Leaf(PlanB)
        );
        var context = CreateContext(TestMonday, isKeyboardMouseIdle: true);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.NotNull(result);
        Assert.Equal(PlanA, result!.PlanGuid);
    }

    [Fact]
    public void RuntimeDependentCondition_False_ShortCircuitsToElse()
    {
        var node = Branch(
            condition: All(KeyboardMouseIdle()),
            thenNode: Leaf(PlanA),
            elseNode: Leaf(PlanB)
        );
        var context = CreateContext(TestMonday, isKeyboardMouseIdle: false);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.NotNull(result);
        Assert.Equal(PlanB, result!.PlanGuid);
    }

    [Fact]
    public void NestedBranchInElse_EvaluatesWhenConditionFalse()
    {
        var node = Branch(
            condition: All(Day(DayType.Weekend)),
            thenNode: Leaf(PlanA),
            elseNode: Branch(
                condition: All(Day(DayType.Weekday)),
                thenNode: Leaf(PlanB),
                elseNode: null
            )
        );
        var context = CreateContext(TestMonday);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.NotNull(result);
        Assert.Equal(PlanB, result!.PlanGuid);
    }

    [Fact]
    public void DisabledBranchNode_ReturnsNullEvenWhenConditionTrue()
    {
        var node = Branch(
            condition: All(Day(DayType.All)),
            thenNode: Leaf(PlanA, isEnabled: false),
            elseNode: Leaf(PlanB)
        );
        var context = CreateContext(TestMonday);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.Null(result);
    }

    [Fact]
    public void AnyOperator_OneTrueCondition_EvaluatesThen()
    {
        var anyCondition = new StrategyConditionGroup
        {
            Operator = StrategyConditionGroupOperator.Any,
            Conditions = new() { Day(DayType.Weekend), Day(DayType.Weekday) },
        };

        var node = Branch(
            condition: anyCondition,
            thenNode: Leaf(PlanA),
            elseNode: Leaf(PlanB)
        );
        var context = CreateContext(TestMonday);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.NotNull(result);
        Assert.Equal(PlanA, result!.PlanGuid);
    }

    [Fact]
    public void NoneOperator_AllFalse_EvaluatesThen()
    {
        var noneCondition = new StrategyConditionGroup
        {
            Operator = StrategyConditionGroupOperator.None,
            Conditions = new() { Day(DayType.Weekend) },
        };

        var node = Branch(
            condition: noneCondition,
            thenNode: Leaf(PlanA),
            elseNode: Leaf(PlanB)
        );
        var context = CreateContext(TestMonday);

        var result = StrategyDecisionNodeEvaluator.Evaluate(node, context);

        Assert.NotNull(result);
        Assert.Equal(PlanA, result!.PlanGuid);
    }
}
