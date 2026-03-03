using AutoPower.Core;
using AutoPower.Core.Models;

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

            Assert.Equal(1, result.SchemaVersion);
            Assert.Equal(5, result.IdleTimeoutMinutes);
            Assert.Equal(DetectionMode.Both, result.Mode);
            Assert.Empty(result.Rules);
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
            var config = new AppConfig
            {
                SchemaVersion = 2,
                Mode = DetectionMode.KeyboardMouse,
                IdleTimeoutMinutes = 15,
                ActivePlanGuid = Guid.NewGuid(),
                IdlePlanGuid = Guid.NewGuid(),
                AutoStartEnabled = true,
                Override = new()
                {
                    IsActive = true,
                    PlanGuid = Guid.NewGuid(),
                    ExpiresAt = DateTime.UtcNow.AddHours(2),
                },
                Rules = new()
                {
                    new()
                    {
                        Name = "Test Rule",
                        DayType = DayType.Weekend,
                        Start = new(10, 30),
                        End = new(18, 0),
                        TargetPlanGuid = Guid.NewGuid(),
                        Priority = 5,
                    },
                },
            };

            ConfigService.Save(config);
            var loaded = ConfigService.Load();

            Assert.Equal(config.SchemaVersion, loaded.SchemaVersion);
            Assert.Equal(config.Mode, loaded.Mode);
            Assert.Equal(config.IdleTimeoutMinutes, loaded.IdleTimeoutMinutes);
            Assert.Equal(config.ActivePlanGuid, loaded.ActivePlanGuid);
            Assert.Equal(config.IdlePlanGuid, loaded.IdlePlanGuid);
            Assert.Equal(config.AutoStartEnabled, loaded.AutoStartEnabled);
            Assert.Equal(config.Override.IsActive, loaded.Override.IsActive);
            Assert.Equal(config.Override.PlanGuid, loaded.Override.PlanGuid);
            Assert.Single(loaded.Rules);
            Assert.Equal(config.Rules[0].Name, loaded.Rules[0].Name);
            Assert.Equal(config.Rules[0].DayType, loaded.Rules[0].DayType);
            Assert.Equal(config.Rules[0].Start, loaded.Rules[0].Start);
            Assert.Equal(config.Rules[0].End, loaded.Rules[0].End);
            Assert.Equal(config.Rules[0].TargetPlanGuid, loaded.Rules[0].TargetPlanGuid);
            Assert.Equal(config.Rules[0].Priority, loaded.Rules[0].Priority);
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
            var config = new AppConfig { SchemaVersion = 1 };
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
    public void Save_OverwritesExistingFile()
    {
        if (File.Exists(ConfigService.ConfigFilePath))
            File.Move(ConfigService.ConfigFilePath, _backupPath, true);

        try
        {
            var config1 = new AppConfig { SchemaVersion = 1, IdleTimeoutMinutes = 5 };
            ConfigService.Save(config1);

            var config2 = new AppConfig { SchemaVersion = 2, IdleTimeoutMinutes = 30 };
            ConfigService.Save(config2);

            var loaded = ConfigService.Load();
            Assert.Equal(2, loaded.SchemaVersion);
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
}
