namespace AutoPower.Core.Core.Models;

public sealed record StrategyEvaluationContext
{
    public DateTime Now { get; init; }
    public bool IsKeyboardMouseDetectionEnabled { get; init; }
    public bool IsMonitorDetectionEnabled { get; init; }
    public bool? IsKeyboardMouseIdle { get; init; }
    public bool? IsMonitorOff { get; init; }
}
