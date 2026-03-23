using System.Text.Json.Serialization;

namespace AutoPower.Core.Core.Models;

[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(StrategyConditionGroup))]
[JsonSerializable(typeof(List<StrategyConditionGroup>))]
[JsonSerializable(typeof(StrategyCondition))]
[JsonSerializable(typeof(List<StrategyCondition>))]
[JsonSerializable(typeof(StrategyDecisionNode))]
[JsonSerializable(typeof(List<StrategyDecisionNode>))]
[JsonSerializable(typeof(OverrideState))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
public partial class AppConfigJsonContext : JsonSerializerContext { }
