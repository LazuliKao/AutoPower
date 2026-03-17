using AutoPower.Core.Core.Models;
using AutoPower.Core.Strategy;

namespace AutoPower.Tests.Strategy;

public class StrategyEvaluatorTests
{
    private static readonly DateTime TestMonday = new(2025, 6, 2, 12, 0, 0);
    private static readonly DateTime TestSaturday = new(2025, 6, 7, 12, 0, 0);
    private static readonly DateTime TestSunday = new(2025, 6, 8, 12, 0, 0);

    private static StrategyRule CreateRule(
        Guid? id = null,
        string name = "Test Rule",
        DayType dayType = DayType.All,
        TimeOnly? start = null,
        TimeOnly? end = null,
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
            DayType = dayType,
            Start = start ?? new TimeOnly(0, 0),
            End = end ?? new TimeOnly(23, 59),
            TargetPlanGuid = targetPlanGuid ?? Guid.NewGuid(),
            Priority = priority,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            IsEnabled = isEnabled,
        };
    }

    [Fact]
    public void NoRules_ReturnsNull()
    {
        var rules = new List<StrategyRule>();

        var result = StrategyEvaluator.Evaluate(rules, TestMonday);

        Assert.Null(result);
    }

    [Fact]
    public void NoMatchingRules_ReturnsNull()
    {
        var rules = new List<StrategyRule>
        {
            CreateRule(
                dayType: DayType.Weekday,
                start: new TimeOnly(9, 0),
                end: new TimeOnly(17, 0)
            ),
        };

        var result = StrategyEvaluator.Evaluate(rules, TestSaturday);

        Assert.Null(result);
    }

    [Fact]
    public void SingleMatchingRule_ReturnsIt()
    {
        var targetGuid = Guid.NewGuid();
        var rules = new List<StrategyRule>
        {
            CreateRule(
                id: Guid.NewGuid(),
                name: "Work Hours",
                dayType: DayType.All,
                start: new TimeOnly(0, 0),
                end: new TimeOnly(23, 59),
                targetPlanGuid: targetGuid,
                priority: 1,
                isEnabled: true
            ),
        };

        var result = StrategyEvaluator.Evaluate(rules, TestMonday);

        Assert.NotNull(result);
        Assert.Equal(targetGuid, result.TargetPlanGuid);
    }

    [Fact]
    public void DisabledRule_IsSkipped()
    {
        var rules = new List<StrategyRule> { CreateRule(isEnabled: false) };

        var result = StrategyEvaluator.Evaluate(rules, TestMonday);

        Assert.Null(result);
    }

    [Fact]
    public void DayType_Weekday_MatchesMonday()
    {
        var rules = new List<StrategyRule> { CreateRule(dayType: DayType.Weekday) };

        var result = StrategyEvaluator.Evaluate(rules, TestMonday);

        Assert.NotNull(result);
    }

    [Fact]
    public void DayType_Weekday_DoesNotMatchSaturday()
    {
        var rules = new List<StrategyRule> { CreateRule(dayType: DayType.Weekday) };

        var result = StrategyEvaluator.Evaluate(rules, TestSaturday);

        Assert.Null(result);
    }

    [Fact]
    public void DayType_Weekend_MatchesSaturday()
    {
        var rules = new List<StrategyRule> { CreateRule(dayType: DayType.Weekend) };

        var result = StrategyEvaluator.Evaluate(rules, TestSaturday);

        Assert.NotNull(result);
    }

    [Fact]
    public void DayType_All_MatchesAnyDay()
    {
        var rules = new List<StrategyRule> { CreateRule(dayType: DayType.All) };

        var weekdayResult = StrategyEvaluator.Evaluate(rules, TestMonday);
        var saturdayResult = StrategyEvaluator.Evaluate(rules, TestSaturday);
        var sundayResult = StrategyEvaluator.Evaluate(rules, TestSunday);

        Assert.NotNull(weekdayResult);
        Assert.NotNull(saturdayResult);
        Assert.NotNull(sundayResult);
    }

    [Fact]
    public void TimeRange_MatchesWithinRange()
    {
        var rules = new List<StrategyRule>
        {
            CreateRule(start: new TimeOnly(9, 0), end: new TimeOnly(17, 0)),
        };

        var result = StrategyEvaluator.Evaluate(rules, new(2025, 6, 2, 12, 0, 0));

        Assert.NotNull(result);
    }

    [Fact]
    public void TimeRange_DoesNotMatchOutsideRange()
    {
        var rules = new List<StrategyRule>
        {
            CreateRule(start: new TimeOnly(9, 0), end: new TimeOnly(17, 0)),
        };

        var result = StrategyEvaluator.Evaluate(rules, new(2025, 6, 2, 20, 0, 0));

        Assert.Null(result);
    }

    [Fact]
    public void OvernightRange_MatchesAfterMidnight()
    {
        var rules = new List<StrategyRule>
        {
            CreateRule(start: new TimeOnly(22, 0), end: new TimeOnly(6, 0)),
        };

        var result = StrategyEvaluator.Evaluate(rules, new(2025, 6, 2, 2, 0, 0));

        Assert.NotNull(result);
    }

    [Fact]
    public void OvernightRange_MatchesBeforeMidnight()
    {
        var rules = new List<StrategyRule>
        {
            CreateRule(start: new TimeOnly(22, 0), end: new TimeOnly(6, 0)),
        };

        var result = StrategyEvaluator.Evaluate(rules, new(2025, 6, 2, 23, 0, 0));

        Assert.NotNull(result);
    }

    [Fact]
    public void HigherPriority_Wins()
    {
        var lowPriorityGuid = Guid.NewGuid();
        var highPriorityGuid = Guid.NewGuid();
        var rules = new List<StrategyRule>
        {
            CreateRule(
                id: Guid.NewGuid(),
                targetPlanGuid: lowPriorityGuid,
                priority: 1,
                createdAt: DateTime.UtcNow.AddMinutes(-1)
            ),
            CreateRule(
                id: Guid.NewGuid(),
                targetPlanGuid: highPriorityGuid,
                priority: 10,
                createdAt: DateTime.UtcNow.AddMinutes(-2)
            ),
        };

        var result = StrategyEvaluator.Evaluate(rules, TestMonday);

        Assert.NotNull(result);
        Assert.Equal(highPriorityGuid, result.TargetPlanGuid);
    }

    [Fact]
    public void SamePriority_EarlierCreatedAt_Wins()
    {
        var earlierGuid = Guid.NewGuid();
        var laterGuid = Guid.NewGuid();
        var rules = new List<StrategyRule>
        {
            CreateRule(
                id: Guid.NewGuid(),
                targetPlanGuid: laterGuid,
                priority: 5,
                createdAt: DateTime.UtcNow
            ),
            CreateRule(
                id: Guid.NewGuid(),
                targetPlanGuid: earlierGuid,
                priority: 5,
                createdAt: DateTime.UtcNow.AddMinutes(-1)
            ),
        };

        var result = StrategyEvaluator.Evaluate(rules, TestMonday);

        Assert.NotNull(result);
        Assert.Equal(earlierGuid, result.TargetPlanGuid);
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

        var result = StrategyEvaluator.Evaluate(rules, TestMonday);

        Assert.NotNull(result);
        Assert.Equal(earlierGuid, result.TargetPlanGuid);
    }
}
