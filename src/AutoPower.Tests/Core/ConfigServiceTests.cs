#nullable enable

using System.Text.Json;
using AutoPower.Core.Core;
using AutoPower.Core.Core.Models;

namespace AutoPower.Tests.Core;

public class ConfigServiceTests : IDisposable
{
    private readonly string _backupPath;

    public ConfigServiceTests()
    {
        _backupPath = Path.Combine(
            Path.GetTempPath(),
            $"AutoPowerTestBackup_{Guid.NewGuid():N}.json"
        );
    }

    [Fact]
    public void Load_NonExistentFile_ReturnsDefaultConfig()
    {
        if (File.Exists(ConfigService.ConfigFilePath))
            File.Move(ConfigService.ConfigFilePath, _backupPath, true);

        try
        {
            var result = ConfigService.Load();

            Assert.Equal(5, result.SchemaVersion);
            Assert.Equal(5, result.IdleTimeoutMinutes);
            Assert.Equal(DetectionMode.Both, result.Mode);
            Assert.Null(result.DefaultPlanGuid);
            Assert.Null(result.DecisionTree);
        }
        finally
        {
            if (File.Exists(_backupPath))
                File.Move(_backupPath, ConfigService.ConfigFilePath, true);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesAllFields()
    {
        if (File.Exists(ConfigService.ConfigFilePath))
            File.Move(ConfigService.ConfigFilePath, _backupPath, true);

        try
        {
            var defaultPlanGuid = Guid.NewGuid();
            var config = new AppConfig
            {
                SchemaVersion = 5,
                Mode = DetectionMode.KeyboardMouse,
                IdleTimeoutMinutes = 15,
                ActivePlanGuid = Guid.NewGuid(),
                IdlePlanGuid = Guid.NewGuid(),
                DefaultPlanGuid = defaultPlanGuid,
                AutoStartEnabled = true,
                Override = new()
                {
                    IsActive = true,
                    PlanGuid = Guid.NewGuid(),
                    ExpiresAt = DateTime.UtcNow.AddHours(2),
                },
                DecisionTree = new StrategyDecisionNode
                {
                    If = new StrategyConditionGroup
                    {
                        Operator = StrategyConditionGroupOperator.All,
                        Conditions = new()
                        {
                            new() { Type = StrategyConditionType.DayType, DayType = DayType.Weekend },
                            new() { Type = StrategyConditionType.TimeRange, Start = new(10, 30), End = new(18, 0) },
                        },
                        Groups = new()
                        {
                            new()
                            {
                                Operator = StrategyConditionGroupOperator.Any,
                                Conditions = new()
                                {
                                    new() { Type = StrategyConditionType.KeyboardMouseIdle },
                                    new() { Type = StrategyConditionType.MonitorOff },
                                },
                            },
                        },
                    },
                    Then = new StrategyDecisionNode { PlanGuid = Guid.NewGuid() },
                    Else = new StrategyDecisionNode { PlanGuid = defaultPlanGuid },
                },
            };

            ConfigService.Save(config);
            var loaded = ConfigService.Load();

            Assert.Equal(config.SchemaVersion, loaded.SchemaVersion);
            Assert.Equal(config.Mode, loaded.Mode);
            Assert.Equal(config.IdleTimeoutMinutes, loaded.IdleTimeoutMinutes);
            Assert.Equal(config.ActivePlanGuid, loaded.ActivePlanGuid);
            Assert.Equal(config.IdlePlanGuid, loaded.IdlePlanGuid);
            Assert.Equal(config.DefaultPlanGuid, loaded.DefaultPlanGuid);
            Assert.Equal(config.AutoStartEnabled, loaded.AutoStartEnabled);
            Assert.Equal(config.Override.IsActive, loaded.Override.IsActive);
            Assert.Equal(config.Override.PlanGuid, loaded.Override.PlanGuid);

            // Verify decision tree
            Assert.NotNull(loaded.DecisionTree);
            Assert.Equal(StrategyConditionGroupOperator.All, loaded.DecisionTree.If!.Operator);
            Assert.Equal(2, loaded.DecisionTree.If.Conditions.Count);
            Assert.Single(loaded.DecisionTree.If.Groups);
            Assert.Equal(StrategyConditionGroupOperator.Any, loaded.DecisionTree.If.Groups[0].Operator);
            Assert.Equal(2, loaded.DecisionTree.If.Groups[0].Conditions.Count);
            Assert.NotNull(loaded.DecisionTree.Then);
            Assert.NotNull(loaded.DecisionTree.Else);
        }
        finally
        {
            if (File.Exists(ConfigService.ConfigFilePath))
                File.Delete(ConfigService.ConfigFilePath);
            if (File.Exists(_backupPath))
                File.Move(_backupPath, ConfigService.ConfigFilePath, true);
        }
    }

    [Fact]
    public void Save_CreatesDirectoryIfNotExists()
    {
        var testDir = Path.GetDirectoryName(ConfigService.ConfigFilePath)!;
        var parentDir = Path.GetDirectoryName(testDir)!;
        var backupDir = parentDir + "_backup";

        if (Directory.Exists(testDir))
        {
            if (Directory.Exists(backupDir))
                Directory.Delete(backupDir, true);
            Directory.Move(testDir, backupDir);
        }

        try
        {
            var config = new AppConfig { SchemaVersion = 3 };
            ConfigService.Save(config);

            Assert.True(File.Exists(ConfigService.ConfigFilePath));
        }
        finally
        {
            if (Directory.Exists(testDir))
                Directory.Delete(testDir, true);
            if (Directory.Exists(backupDir))
                Directory.Move(backupDir, testDir);
        }
    }

    [Fact]
    public void Load_CorruptedJson_ReturnsDefaultConfig()
    {
        if (File.Exists(ConfigService.ConfigFilePath))
            File.Move(ConfigService.ConfigFilePath, _backupPath, true);

        try
        {
            var dir = Path.GetDirectoryName(ConfigService.ConfigFilePath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(ConfigService.ConfigFilePath, "this is not valid json {{{");

            var config = ConfigService.Load();
            var defaultConfig = new AppConfig();
            Assert.Equal(defaultConfig.SchemaVersion, config.SchemaVersion);
            Assert.Equal(defaultConfig.Mode, config.Mode);
            Assert.Equal(defaultConfig.IdleTimeoutMinutes, config.IdleTimeoutMinutes);
        }
        finally
        {
            if (File.Exists(ConfigService.ConfigFilePath))
                File.Delete(ConfigService.ConfigFilePath);
            if (File.Exists(_backupPath))
                File.Move(_backupPath, ConfigService.ConfigFilePath, true);
        }
    }

    [Fact]
    public void Load_UnsupportedSchemaVersion_ReturnsDefaultConfig()
    {
        if (File.Exists(ConfigService.ConfigFilePath))
            File.Move(ConfigService.ConfigFilePath, _backupPath, true);

        try
        {
            var dir = Path.GetDirectoryName(ConfigService.ConfigFilePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(new AppConfig { SchemaVersion = 2 }, AppConfigJsonContext.Default.AppConfig);
            File.WriteAllText(ConfigService.ConfigFilePath, json);

            var config = ConfigService.Load();

            Assert.Equal(5, config.SchemaVersion);
            Assert.Null(config.DecisionTree);
            Assert.Null(config.DefaultPlanGuid);
        }
        finally
        {
            if (File.Exists(ConfigService.ConfigFilePath))
                File.Delete(ConfigService.ConfigFilePath);
            if (File.Exists(_backupPath))
                File.Move(_backupPath, ConfigService.ConfigFilePath, true);
        }
    }

    [Fact]
    public void Save_OverwritesExistingFile()
    {
        if (File.Exists(ConfigService.ConfigFilePath))
            File.Move(ConfigService.ConfigFilePath, _backupPath, true);

        try
        {
            var config1 = new AppConfig { SchemaVersion = 3, IdleTimeoutMinutes = 5 };
            ConfigService.Save(config1);

            var config2 = new AppConfig { SchemaVersion = 3, IdleTimeoutMinutes = 30 };
            ConfigService.Save(config2);

            var loaded = ConfigService.Load();
            Assert.Equal(5, loaded.SchemaVersion);
            Assert.Equal(30, loaded.IdleTimeoutMinutes);
        }
        finally
        {
            if (File.Exists(ConfigService.ConfigFilePath))
                File.Delete(ConfigService.ConfigFilePath);
            if (File.Exists(_backupPath))
                File.Move(_backupPath, ConfigService.ConfigFilePath, true);
        }
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_backupPath) && !File.Exists(ConfigService.ConfigFilePath))
                File.Move(_backupPath, ConfigService.ConfigFilePath, true);
            else if (File.Exists(_backupPath))
                File.Delete(_backupPath);
        }
        catch { }
    }

    [Fact]
    public void SaveAndLoad_DefaultPlanGuid_IsPreserved()
    {
        if (File.Exists(ConfigService.ConfigFilePath))
            File.Move(ConfigService.ConfigFilePath, _backupPath, true);

        try
        {
            var defaultPlanGuid = Guid.NewGuid();
            var config = new AppConfig
            {
                DefaultPlanGuid = defaultPlanGuid,
                DecisionTree = new StrategyDecisionNode
                {
                    If = new StrategyConditionGroup
                    {
                        Operator = StrategyConditionGroupOperator.All,
                        Conditions = new() { new StrategyCondition { Type = StrategyConditionType.DayType, DayType = DayType.All } }
                    },
                    Then = new StrategyDecisionNode { PlanGuid = Guid.NewGuid() }
                }
            };

            ConfigService.Save(config);
            var loaded = ConfigService.Load();

            Assert.NotNull(loaded.DefaultPlanGuid);
            Assert.Equal(defaultPlanGuid, loaded.DefaultPlanGuid);
        }
        finally
        {
            if (File.Exists(ConfigService.ConfigFilePath))
                File.Delete(ConfigService.ConfigFilePath);
            if (File.Exists(_backupPath))
                File.Move(_backupPath, ConfigService.ConfigFilePath, true);
        }
    }

    [Fact]
    public void SaveAndLoad_ValidDefaultPlanGuid_IsPreserved()
    {
        if (File.Exists(ConfigService.ConfigFilePath))
            File.Move(ConfigService.ConfigFilePath, _backupPath, true);

        try
        {
            var defaultPlanGuid = Guid.NewGuid();
            var config = new AppConfig
            {
                DefaultPlanGuid = defaultPlanGuid,
                DecisionTree = new StrategyDecisionNode
                {
                    If = new StrategyConditionGroup
                    {
                        Operator = StrategyConditionGroupOperator.All,
                        Conditions = new() { new StrategyCondition { Type = StrategyConditionType.DayType, DayType = DayType.All } }
                    },
                    Then = new StrategyDecisionNode { PlanGuid = defaultPlanGuid }
                }
            };

            ConfigService.Save(config);
            var loaded = ConfigService.Load();

            Assert.Equal(defaultPlanGuid, loaded.DefaultPlanGuid);
        }
        finally
        {
            if (File.Exists(ConfigService.ConfigFilePath))
                File.Delete(ConfigService.ConfigFilePath);
            if (File.Exists(_backupPath))
                File.Move(_backupPath, ConfigService.ConfigFilePath, true);
        }
    }

    [Fact]
    public void SaveAndLoad_NullDecisionTree_IsPreserved()
    {
        if (File.Exists(ConfigService.ConfigFilePath))
            File.Move(ConfigService.ConfigFilePath, _backupPath, true);

        try
        {
            var config = new AppConfig
            {
                DecisionTree = null,
                DefaultPlanGuid = Guid.NewGuid()
            };

            ConfigService.Save(config);
            var loaded = ConfigService.Load();

            Assert.Null(loaded.DecisionTree);
        }
        finally
        {
            if (File.Exists(ConfigService.ConfigFilePath))
                File.Delete(ConfigService.ConfigFilePath);
            if (File.Exists(_backupPath))
                File.Move(_backupPath, ConfigService.ConfigFilePath, true);
        }
    }

    [Fact]
    public void SaveAndLoad_SimpleLeafDecisionTree_IsPreserved()
    {
        if (File.Exists(ConfigService.ConfigFilePath))
            File.Move(ConfigService.ConfigFilePath, _backupPath, true);

        try
        {
            var planGuid = Guid.NewGuid();
            var config = new AppConfig
            {
                DecisionTree = new StrategyDecisionNode { PlanGuid = planGuid }
            };

            ConfigService.Save(config);
            var loaded = ConfigService.Load();

            Assert.NotNull(loaded.DecisionTree);
            Assert.Equal(planGuid, loaded.DecisionTree.PlanGuid);
            Assert.Null(loaded.DecisionTree.Then);
            Assert.Null(loaded.DecisionTree.Else);
            Assert.Null(loaded.DecisionTree.If);
        }
        finally
        {
            if (File.Exists(ConfigService.ConfigFilePath))
                File.Delete(ConfigService.ConfigFilePath);
            if (File.Exists(_backupPath))
                File.Move(_backupPath, ConfigService.ConfigFilePath, true);
        }
    }

    [Fact]
    public void SaveAndLoad_NestedDecisionTree_IsPreserved()
    {
        if (File.Exists(ConfigService.ConfigFilePath))
            File.Move(ConfigService.ConfigFilePath, _backupPath, true);

        try
        {
            var leafGuid = Guid.NewGuid();
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
                        Then = new StrategyDecisionNode { PlanGuid = leafGuid }
                    },
                    Else = new StrategyDecisionNode { PlanGuid = Guid.NewGuid() }
                }
            };

            ConfigService.Save(config);
            var loaded = ConfigService.Load();

            Assert.NotNull(loaded.DecisionTree);
            Assert.NotNull(loaded.DecisionTree.Then);
            Assert.NotNull(loaded.DecisionTree.Else);
            Assert.Equal(leafGuid, loaded.DecisionTree.Then.Then!.PlanGuid);
        }
        finally
        {
            if (File.Exists(ConfigService.ConfigFilePath))
                File.Delete(ConfigService.ConfigFilePath);
            if (File.Exists(_backupPath))
                File.Move(_backupPath, ConfigService.ConfigFilePath, true);
        }
    }

    [Fact]
    public void Save_InvalidDecisionTree_ThrowsException()
    {
        if (File.Exists(ConfigService.ConfigFilePath))
            File.Move(ConfigService.ConfigFilePath, _backupPath, true);

        try
        {
            // Create an invalid tree where a node has both PlanGuid AND Then
            var config = new AppConfig
            {
                DecisionTree = new StrategyDecisionNode
                {
                    PlanGuid = Guid.NewGuid(),
                    Then = new StrategyDecisionNode { PlanGuid = Guid.NewGuid() }
                }
            };

            Assert.Throws<InvalidOperationException>(() => ConfigService.Save(config));
        }
        finally
        {
            if (File.Exists(ConfigService.ConfigFilePath))
                File.Delete(ConfigService.ConfigFilePath);
            if (File.Exists(_backupPath))
                File.Move(_backupPath, ConfigService.ConfigFilePath, true);
        }
    }

    [Fact]
    public void Save_DisabledNode_IsPreserved()
    {
        if (File.Exists(ConfigService.ConfigFilePath))
            File.Move(ConfigService.ConfigFilePath, _backupPath, true);

        try
        {
            var config = new AppConfig
            {
                DecisionTree = new StrategyDecisionNode
                {
                    IsEnabled = false,
                    PlanGuid = Guid.NewGuid()
                }
            };

            ConfigService.Save(config);
            var loaded = ConfigService.Load();

            Assert.NotNull(loaded.DecisionTree);
            Assert.False(loaded.DecisionTree.IsEnabled);
        }
        finally
        {
            if (File.Exists(ConfigService.ConfigFilePath))
                File.Delete(ConfigService.ConfigFilePath);
            if (File.Exists(_backupPath))
                File.Move(_backupPath, ConfigService.ConfigFilePath, true);
        }
    }
}
