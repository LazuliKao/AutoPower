using System.Text.Json.Serialization;
namespace AutoPower.Core.Models;

[JsonSerializable(typeof(AppConfig))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class AppConfigJsonContext : JsonSerializerContext { }
