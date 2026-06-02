using System.Text.Json;
using AutoPower.Core.Core.Models;
using AutoPower.Core.Infrastructure;

namespace AutoPower.Core.Core;

internal static class ConfigService
{
     private const int CurrentSchemaVersion = 5;

    internal static string ConfigFilePath { get; } =
        Path.Combine(AppContext.BaseDirectory, "data", "config.json");

    internal static AppConfig Load()
    {
        if (!File.Exists(ConfigFilePath))
            return CreateDefaultConfig();
        LoggerService.Info($"Loading config from {ConfigFilePath}");
        try
        {
            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);
            if (config is null)
            {
                return CreateDefaultConfig();
            }

            if (config.SchemaVersion < CurrentSchemaVersion)
            {
                LoggerService.Warn(
                    $"Unsupported config schema version {config.SchemaVersion}. Resetting to defaults."
                );
                return CreateDefaultConfig();
            }

            return Normalize(config);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            LoggerService.Warn($"Failed to load config, using defaults: {ex.Message}");
            return CreateDefaultConfig();
        }
    }

    internal static void Save(AppConfig config)
    {
        var directory = Path.GetDirectoryName(ConfigFilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var normalized = Normalize(config);

        if (normalized.DecisionTree is not null)
        {
            ValidateDecisionTree(normalized.DecisionTree);
        }

        var json = JsonSerializer.Serialize(normalized, AppConfigJsonContext.Default.AppConfig);
        File.WriteAllText(ConfigFilePath, json);
    }

    /// <summary>
    /// Recursively validates a decision tree node and all its children.
    /// </summary>
    /// <param name="node">The node to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when a node has both PlanGuid and Then.</exception>
    private static void ValidateDecisionTree(StrategyDecisionNode node)
    {
        node.Validate();
        if (node.Then is not null)
            ValidateDecisionTree(node.Then);
        if (node.Else is not null)
            ValidateDecisionTree(node.Else);
    }

    private static AppConfig CreateDefaultConfig()
    {
        return Normalize(new AppConfig());
    }

     private static AppConfig Normalize(AppConfig config)
     {
         return config with
         {
             SchemaVersion = CurrentSchemaVersion,
             IdleTimeoutMinutes = Math.Max(1, config.IdleTimeoutMinutes),
             DefaultPlanGuid = config.DefaultPlanGuid,
             DecisionTree = config.DecisionTree,
             Override = config.Override ?? new(),
         };
     }
}
