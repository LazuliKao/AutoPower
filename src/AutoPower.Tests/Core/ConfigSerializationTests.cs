#nullable enable

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

        // Create a decision tree: IF [condition] THEN [plan1] ELSE [plan2]
        var decisionTree = new StrategyDecisionNode
        {
            If = group,
            Then = new StrategyDecisionNode { PlanGuid = Guid.NewGuid() },
            Else = new StrategyDecisionNode { PlanGuid = Guid.NewGuid() },
        };

        var config = new AppConfig
        {
            SchemaVersion = 3,
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
            DecisionTree = decisionTree,
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

        // Verify decision tree
        Assert.NotNull(deserialized.DecisionTree);
        Assert.Equal(StrategyConditionGroupOperator.All, deserialized.DecisionTree.If!.Operator);
        Assert.Equal(2, deserialized.DecisionTree.If.Conditions.Count);
        Assert.Single(deserialized.DecisionTree.If.Groups);
        Assert.Equal(StrategyConditionGroupOperator.Any, deserialized.DecisionTree.If.Groups[0].Operator);
        Assert.Equal(2, deserialized.DecisionTree.If.Groups[0].Conditions.Count);

        // Verify Then branch
        Assert.NotNull(deserialized.DecisionTree.Then);
        Assert.Equal(config.DecisionTree.Then!.PlanGuid, deserialized.DecisionTree.Then!.PlanGuid);

        // Verify Else branch
        Assert.NotNull(deserialized.DecisionTree.Else);
        Assert.Equal(config.DecisionTree.Else!.PlanGuid, deserialized.DecisionTree.Else!.PlanGuid);
    }

    [Fact]
    public void DefaultConfig_Serializes_WithExpectedDefaults()
    {
        var config = new AppConfig();

        var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        var deserialized = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);

        Assert.NotNull(deserialized);
        Assert.Equal(5, deserialized.SchemaVersion);
        Assert.Equal(DetectionMode.Both, deserialized.Mode);
        Assert.Equal(5, deserialized.IdleTimeoutMinutes);
        Assert.Null(deserialized.DefaultPlanGuid);
        Assert.Null(deserialized.DecisionTree);
        Assert.False(deserialized.AutoStartEnabled);
        Assert.False(deserialized.Override.IsActive);
    }

    [Fact]
    public void SimpleLeafNode_SerializesCorrectly()
    {
        var leafGuid = Guid.NewGuid();
        var config = new AppConfig
        {
            DecisionTree = new StrategyDecisionNode { PlanGuid = leafGuid }
        };

        var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        var deserialized = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);

        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.DecisionTree);
        Assert.Equal(leafGuid, deserialized.DecisionTree.PlanGuid);
        Assert.Null(deserialized.DecisionTree.Then);
        Assert.Null(deserialized.DecisionTree.Else);
        Assert.Null(deserialized.DecisionTree.If);
    }

    [Fact]
    public void NestedTree_ThreeLevels_SerializesCorrectly()
    {
        var level3Guid = Guid.NewGuid();
        var config = new AppConfig
        {
            DecisionTree = new StrategyDecisionNode
            {
                If = new StrategyConditionGroup
                {
                    Operator = StrategyConditionGroupOperator.All,
                    Conditions = new() { new StrategyCondition { Type = StrategyConditionType.DayType, DayType = DayType.Weekday } }
                },
                Then = new StrategyDecisionNode
                {
                    If = new StrategyConditionGroup
                    {
                        Operator = StrategyConditionGroupOperator.All,
                        Conditions = new() { new StrategyCondition { Type = StrategyConditionType.TimeRange, Start = new(9, 0), End = new(17, 0) } }
                    },
                    Then = new StrategyDecisionNode { PlanGuid = level3Guid }
                }
            }
        };

        var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        var deserialized = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);

        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.DecisionTree);
        Assert.NotNull(deserialized.DecisionTree.Then);
        Assert.NotNull(deserialized.DecisionTree.Then.Then);
        Assert.Equal(level3Guid, deserialized.DecisionTree.Then.Then.PlanGuid);
    }

    [Fact]
    public void BranchWithBothThenAndElse_SerializesCorrectly()
    {
        var thenGuid = Guid.NewGuid();
        var elseGuid = Guid.NewGuid();
        var config = new AppConfig
        {
            DecisionTree = new StrategyDecisionNode
            {
                If = new StrategyConditionGroup
                {
                    Operator = StrategyConditionGroupOperator.All,
                    Conditions = new() { new StrategyCondition { Type = StrategyConditionType.KeyboardMouseIdle } }
                },
                Then = new StrategyDecisionNode { PlanGuid = thenGuid },
                Else = new StrategyDecisionNode { PlanGuid = elseGuid }
            }
        };

        var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        var deserialized = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);

        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.DecisionTree);
        Assert.Equal(thenGuid, deserialized.DecisionTree.Then!.PlanGuid);
        Assert.Equal(elseGuid, deserialized.DecisionTree.Else!.PlanGuid);
    }

    [Fact]
    public void DisabledNode_PreservesIsEnabled()
    {
        var config = new AppConfig
        {
            DecisionTree = new StrategyDecisionNode
            {
                IsEnabled = false,
                PlanGuid = Guid.NewGuid()
            }
        };

        var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        var deserialized = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);

        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.DecisionTree);
        Assert.False(deserialized.DecisionTree.IsEnabled);
    }

    [Fact]
    public void NodeId_PreservesGuid()
    {
        var nodeId = Guid.NewGuid();
        var config = new AppConfig
        {
            DecisionTree = new StrategyDecisionNode
            {
                Id = nodeId,
                PlanGuid = Guid.NewGuid()
            }
        };

        var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        var deserialized = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);

        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.DecisionTree);
        Assert.Equal(nodeId, deserialized.DecisionTree.Id);
    }

    [Fact]
    public void NullIfCondition_SerializesAsNull()
    {
        var config = new AppConfig
        {
            DecisionTree = new StrategyDecisionNode
            {
                If = null,  // Always matches
                Then = new StrategyDecisionNode { PlanGuid = Guid.NewGuid() }
            }
        };

        var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        var deserialized = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);

        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.DecisionTree);
        Assert.Null(deserialized.DecisionTree.If);
        Assert.NotNull(deserialized.DecisionTree.Then);
    }
}
