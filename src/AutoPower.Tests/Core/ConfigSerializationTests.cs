using System.Text.Json;
using AutoPower.Core.Models;

namespace AutoPower.Tests.Core;

public class ConfigSerializationTests
{
    [Fact]
    public void RoundTrip_SerializeDeserialize_PreservesValues()
    {
        // Arrange
        var config = new AppConfig
        {
            SchemaVersion = 2,
            Mode = DetectionMode.KeyboardMouse,
            IdleTimeoutMinutes = 10,
            ActivePlanGuid = Guid.NewGuid(),
            IdlePlanGuid = Guid.NewGuid(),
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
                    DayType = DayType.Weekday,
                    Start = new(9, 0),
                    End = new(17, 0),
                    TargetPlanGuid = Guid.NewGuid(),
                    Priority = 1,
                },
            },
        };

        // Act
        var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        var deserialized = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(config.SchemaVersion, deserialized.SchemaVersion);
        Assert.Equal(config.Mode, deserialized.Mode);
        Assert.Equal(config.IdleTimeoutMinutes, deserialized.IdleTimeoutMinutes);
        Assert.Equal(config.ActivePlanGuid, deserialized.ActivePlanGuid);
        Assert.Equal(config.IdlePlanGuid, deserialized.IdlePlanGuid);
        Assert.Equal(config.AutoStartEnabled, deserialized.AutoStartEnabled);

        Assert.Equal(config.Override.IsActive, deserialized.Override.IsActive);
        Assert.Equal(config.Override.PlanGuid, deserialized.Override.PlanGuid);
        Assert.Equal(config.Override.ExpiresAt, deserialized.Override.ExpiresAt);

        Assert.Single(deserialized.Rules);
        var rule = config.Rules[0];
        var deserializedRule = deserialized.Rules[0];
        Assert.Equal(rule.Name, deserializedRule.Name);
        Assert.Equal(rule.DayType, deserializedRule.DayType);
        Assert.Equal(rule.Start, deserializedRule.Start);
        Assert.Equal(rule.End, deserializedRule.End);
        Assert.Equal(rule.TargetPlanGuid, deserializedRule.TargetPlanGuid);
        Assert.Equal(rule.Priority, deserializedRule.Priority);
    }

    [Fact]
    public void DefaultConfig_Serializes_WithExpectedDefaults()
    {
        // Arrange
        var config = new AppConfig();

        // Act
        var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        var deserialized = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(1, deserialized.SchemaVersion);
        Assert.Equal(DetectionMode.Both, deserialized.Mode);
        Assert.Equal(5, deserialized.IdleTimeoutMinutes);
        Assert.Empty(deserialized.Rules);
        Assert.False(deserialized.AutoStartEnabled);
        Assert.False(deserialized.Override.IsActive);
    }
}
