using System.Text.Json;
using AutoPower.Core.Core.Models;
using AutoPower.Core.Infrastructure;

namespace AutoPower.Core.Core;

internal static class ConfigService
{
    private const int CurrentSchemaVersion = 2;

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
        var json = JsonSerializer.Serialize(normalized, AppConfigJsonContext.Default.AppConfig);
        File.WriteAllText(ConfigFilePath, json);
    }

    private static AppConfig CreateDefaultConfig()
    {
        return Normalize(new AppConfig());
    }

    private static AppConfig Normalize(AppConfig config)
    {
        var normalizedRules = (config.Rules ?? new())
            .Where(rule => rule.TargetPlanGuid != Guid.Empty)
            .Select(NormalizeRule)
            .OrderByDescending(rule => rule.Priority)
            .ThenBy(rule => rule.CreatedAt)
            .ThenBy(rule => rule.Id.ToString())
            .ToList();

        var validDefaultPlanGuid = ValidateDefaultPlanGuid(config.DefaultPlanGuid, normalizedRules);

        return config with
        {
            SchemaVersion = CurrentSchemaVersion,
            IdleTimeoutMinutes = Math.Max(1, config.IdleTimeoutMinutes),
            DefaultPlanGuid = validDefaultPlanGuid,
            Rules = normalizedRules,
            Override = config.Override ?? new(),
        };
    }

    private static StrategyRule NormalizeRule(StrategyRule rule)
    {
        return rule with
        {
            Id = rule.Id == Guid.Empty ? Guid.NewGuid() : rule.Id,
            Name = string.IsNullOrWhiteSpace(rule.Name) ? "Rule" : rule.Name.Trim(),
            Condition = NormalizeGroupWithCycleDetection(rule.Condition),
            CreatedAt = rule.CreatedAt == default ? DateTime.UtcNow : rule.CreatedAt,
        };
    }

    private static Guid? ValidateDefaultPlanGuid(Guid? defaultPlanGuid, IReadOnlyList<StrategyRule> normalizedRules)
    {
        if (!defaultPlanGuid.HasValue || defaultPlanGuid.Value == Guid.Empty)
        {
            return null;
        }

        var defaultPlan = defaultPlanGuid.Value;
        var isValid = normalizedRules.Any(rule => rule.TargetPlanGuid == defaultPlan);

        if (!isValid)
        {
            LoggerService.Warn($"DefaultPlanGuid {defaultPlan} does not reference any valid rule target. Clearing default.");
            return null;
        }

        return defaultPlan;
    }

    private static StrategyCondition NormalizeCondition(StrategyCondition condition)
    {
        return condition with
        {
            Id = condition.Id == Guid.Empty ? Guid.NewGuid() : condition.Id,
        };
    }

    private static StrategyConditionGroup NormalizeGroupWithCycleDetection(
        StrategyConditionGroup? group,
        HashSet<Guid>? visitedGroupIds = null
    )
    {
        visitedGroupIds ??= new();

        if (group is null)
        {
            return StrategyConditionGroup.MatchAll();
        }

        var groupId = group.Id == Guid.Empty ? Guid.NewGuid() : group.Id;

        if (!visitedGroupIds.Add(groupId))
        {
            LoggerService.Warn($"Cycle detected in condition group {groupId}. Replacing with empty group.");
            return StrategyConditionGroup.MatchAll();
        }

        var normalizedConditions = (group.Conditions ?? new())
            .Select(NormalizeCondition)
            .ToList();

        var normalizedGroups = (group.Groups ?? new())
            .Select(childGroup => NormalizeGroupWithCycleDetection(childGroup, new HashSet<Guid>(visitedGroupIds)))
            .Where(g => !IsEmptyGroup(g))
            .ToList();

        visitedGroupIds.Remove(groupId);

        return group with
        {
            Id = groupId,
            Conditions = normalizedConditions,
            Groups = normalizedGroups,
        };
    }

    private static bool IsEmptyGroup(StrategyConditionGroup group)
    {
        return (group.Conditions ?? new()).Count == 0 && (group.Groups ?? new()).Count == 0;
    }
}
