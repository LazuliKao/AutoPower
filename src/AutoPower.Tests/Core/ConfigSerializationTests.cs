using System.Text.Json;
using AutoPower.Core.Core.Models;
using AppConfigJsonContext = AutoPower.Core.Core.Models.AppConfigJsonContext;

namespace AutoPower.Tests.Core;

public class ConfigSerializationTests
{
    [Fact]
    public void RoundTrip_SerializeDeserialize_PreservesValues()
    {
        var keyboardIdleCondition = new StrategyCondition
        {
            Type = StrategyConditionType.KeyboardMouseIdle,
        };
        var monitorCondition = new StrategyCondition
        {
            Type = StrategyConditionType.MonitorOff,
        };
        var group = new StrategyConditionGroup
        {
            Operator = StrategyConditionGroupOperator.All,
            Conditions = new()
            {
                new() { Type = StrategyConditionType.DayType, DayType = DayType.Weekday },
                new() { Type = StrategyConditionType.TimeRange, Start = new(9, 0), End = new(17, 0) },
            },
            Groups = new()
            {
                new()
                {
                    Operator = StrategyConditionGroupOperator.Any,
                    Conditions = new() { keyboardIdleCondition, monitorCondition },
                },
            },
        };

        var config = new AppConfig
        {
            SchemaVersion = 2,
            Mode = DetectionMode.KeyboardMouse,
            IdleTimeoutMinutes = 10,
            ActivePlanGuid = Guid.NewGuid(),
            IdlePlanGuid = Guid.NewGuid(),
            DefaultPlanGuid = Guid.NewGuid(),
            AutoStartEnabled = true,
            Override = new()
            {
                IsActive = true,
                PlanGuid = Guid.NewGuid(),
                ExpiresAt = DateTime.UtcNow.AddHours(1),
            },
            Rules = new()
            {
                new()
                {
                    Name = "Work Hours",
                    TargetPlanGuid = Guid.NewGuid(),
                    Priority = 1,
                    Condition = group,
                },
            },
        };

        var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        var deserialized = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);

        Assert.NotNull(deserialized);
        Assert.Equal(config.SchemaVersion, deserialized.SchemaVersion);
        Assert.Equal(config.Mode, deserialized.Mode);
        Assert.Equal(config.IdleTimeoutMinutes, deserialized.IdleTimeoutMinutes);
        Assert.Equal(config.ActivePlanGuid, deserialized.ActivePlanGuid);
        Assert.Equal(config.IdlePlanGuid, deserialized.IdlePlanGuid);
        Assert.Equal(config.DefaultPlanGuid, deserialized.DefaultPlanGuid);
        Assert.Equal(config.AutoStartEnabled, deserialized.AutoStartEnabled);
        Assert.Equal(config.Override.IsActive, deserialized.Override.IsActive);
        Assert.Equal(config.Override.PlanGuid, deserialized.Override.PlanGuid);
        Assert.Equal(config.Override.ExpiresAt, deserialized.Override.ExpiresAt);
        Assert.Single(deserialized.Rules);

        var rule = deserialized.Rules[0];
        Assert.Equal("Work Hours", rule.Name);
        Assert.Equal(config.Rules[0].TargetPlanGuid, rule.TargetPlanGuid);
        Assert.Equal(StrategyConditionGroupOperator.All, rule.Condition.Operator);
        Assert.Equal(2, rule.Condition.Conditions.Count);
        Assert.Single(rule.Condition.Groups);
        Assert.Equal(StrategyConditionGroupOperator.Any, rule.Condition.Groups[0].Operator);
        Assert.Equal(2, rule.Condition.Groups[0].Conditions.Count);
    }

    [Fact]
    public void DefaultConfig_Serializes_WithExpectedDefaults()
    {
        var config = new AppConfig();

        var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        var deserialized = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);

        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.SchemaVersion);
        Assert.Equal(DetectionMode.Both, deserialized.Mode);
        Assert.Equal(5, deserialized.IdleTimeoutMinutes);
        Assert.Null(deserialized.DefaultPlanGuid);
        Assert.Empty(deserialized.Rules);
        Assert.False(deserialized.AutoStartEnabled);
        Assert.False(deserialized.Override.IsActive);
    }
}
