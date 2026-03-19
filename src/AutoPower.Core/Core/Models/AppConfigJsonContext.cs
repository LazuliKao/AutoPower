using System.Text.Json.Serialization;

namespace AutoPower.Core.Core.Models;

[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(List<StrategyRule>))]
[JsonSerializable(typeof(StrategyRule))]
[JsonSerializable(typeof(StrategyConditionGroup))]
[JsonSerializable(typeof(List<StrategyConditionGroup>))]
[JsonSerializable(typeof(StrategyCondition))]
[JsonSerializable(typeof(List<StrategyCondition>))]
[JsonSerializable(typeof(OverrideState))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
public partial class AppConfigJsonContext : JsonSerializerContext { }
