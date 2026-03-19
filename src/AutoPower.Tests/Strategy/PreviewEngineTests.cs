using AutoPower.Core.Core.Models;
using AutoPower.Core.Strategy;

namespace AutoPower.Tests.Strategy;

public class PreviewEngineTests
{
    [Fact]
    public void GenerateTimeline_NoRules_UsesDefaultPlan()
    {
        var defaultPlanGuid = Guid.NewGuid();
        var config = new AppConfig
        {
            ActivePlanGuid = Guid.NewGuid(),
            IdlePlanGuid = Guid.NewGuid(),
            DefaultPlanGuid = defaultPlanGuid,
        };
        var plans = new List<PowerPlanInfo>
        {
            new(defaultPlanGuid, "Default", false),
        };

        var timeline = PreviewEngine.GenerateTimeline(config, plans, new DateTime(2025, 6, 2, 8, 0, 0));

        Assert.Single(timeline);
        Assert.Equal(defaultPlanGuid, timeline[0].PlanGuid);
        Assert.Equal("Default plan", timeline[0].Source);
    }

    [Fact]
    public void GenerateTimeline_RuntimeCondition_UsesSnapshotAndMarksSource()
    {
        var planGuid = Guid.NewGuid();
        var config = new AppConfig
        {
            Mode = DetectionMode.KeyboardMouse,
            ActivePlanGuid = Guid.NewGuid(),
            IdlePlanGuid = Guid.NewGuid(),
            Rules = new()
            {
                new()
                {
                    Name = "Idle Focus",
                    TargetPlanGuid = planGuid,
                    Condition = new()
                    {
                        Operator = StrategyConditionGroupOperator.All,
                        Conditions = new()
                        {
                            new() { Type = StrategyConditionType.KeyboardMouseIdle },
                        },
                    },
                },
            },
        };
        var plans = new List<PowerPlanInfo>
        {
            new(planGuid, "Idle Focus", false),
        };
        var snapshot = new StrategyEvaluationContext
        {
            Now = new DateTime(2025, 6, 2, 8, 0, 0),
            IsKeyboardMouseDetectionEnabled = true,
            IsKeyboardMouseIdle = true,
        };

        var timeline = PreviewEngine.GenerateTimeline(
            config,
            plans,
            new DateTime(2025, 6, 2, 8, 0, 0),
            snapshot: snapshot
        );

        Assert.Single(timeline);
        Assert.Equal(planGuid, timeline[0].PlanGuid);
        Assert.Contains("runtime snapshot", timeline[0].Source);
    }

    [Fact]
    public void GenerateTimeline_ScheduleRule_EmitsTransitionBoundaries()
    {
        var defaultPlanGuid = Guid.NewGuid();
        var rulePlanGuid = Guid.NewGuid();
        var config = new AppConfig
        {
            ActivePlanGuid = Guid.NewGuid(),
            IdlePlanGuid = Guid.NewGuid(),
            DefaultPlanGuid = defaultPlanGuid,
            Rules = new()
            {
                new()
                {
                    Name = "Work Hours",
                    TargetPlanGuid = rulePlanGuid,
                    Condition = StrategyConditionGroup.ForSchedule(DayType.All, new(9, 0), new(17, 0)),
                },
            },
        };
        var plans = new List<PowerPlanInfo>
        {
            new(defaultPlanGuid, "Default", false),
            new(rulePlanGuid, "Work", false),
        };

        var timeline = PreviewEngine.GenerateTimeline(config, plans, new DateTime(2025, 6, 2, 8, 0, 0), hours: 12);

        Assert.True(timeline.Count >= 3);
        Assert.Equal(defaultPlanGuid, timeline[0].PlanGuid);
        Assert.Equal(rulePlanGuid, timeline[1].PlanGuid);
        Assert.Equal(new DateTime(2025, 6, 2, 9, 0, 0), timeline[1].Time);
        Assert.Equal(defaultPlanGuid, timeline[2].PlanGuid);
    }
}
