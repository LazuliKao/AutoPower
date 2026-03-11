using System.Text.Json;
using AutoPower.Core.Models;

namespace AutoPower.Core;

internal static class ConfigService
{
    internal static string ConfigFilePath { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AutoPower",
            "config.json"
        );

    internal static AppConfig Load()
    {
        if (!File.Exists(ConfigFilePath))
            return new();

        try
        {
            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);
            return config ?? new AppConfig();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            Infrastructure.LoggerService.Warn(
                $"Failed to load config, using defaults: {ex.Message}"
            );
            return new();
        }
    }

    internal static void Save(AppConfig config)
    {
        var directory = Path.GetDirectoryName(ConfigFilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        File.WriteAllText(ConfigFilePath, json);
    }
}
