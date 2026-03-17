namespace AutoPower.Core.Core.Models;

public sealed record OverrideState
{
    public bool IsActive { get; init; }
    public Guid? PlanGuid { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
