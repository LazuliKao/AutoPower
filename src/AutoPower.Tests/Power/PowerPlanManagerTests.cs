using AutoPower.Core.Infrastructure.Win32;
using AutoPower.Core.Power;
using Xunit.Abstractions;

namespace AutoPower.Tests.Power;

public class PowerPlanManagerTests(ITestOutputHelper logger)
{
    [Fact]
    public void EnumeratePlans_ShouldReturnAtLeastOnePlan()
    {
        // Act
        var plans = PowerPlanManager.EnumeratePlans();
        foreach (var (guid, name, isActive) in plans)
        {
            // Just print them for debugging purposes
            logger.WriteLine($"Plan: {name} (GUID: {guid}, Active: {isActive})");
        }
        // Assert
        Assert.NotNull(plans);
        Assert.NotEmpty(plans);

        if (OperatingSystem.IsWindows())
        {
            Assert.Contains(
                plans,
                p => p.Guid == PowrProf.GUID_BALANCED || !string.IsNullOrEmpty(p.Name)
            );
            Assert.Contains(
                plans,
                p => p.Guid == PowrProf.GUID_HIGH_PERFORMANCE || !string.IsNullOrEmpty(p.Name)
            );
            return;
        }

        Assert.All(plans, plan => Assert.False(string.IsNullOrWhiteSpace(plan.Name)));
    }

    [Fact]
    public void GetActivePlan_ShouldReturnValidPlan()
    {
        // Act
        var activePlan = PowerPlanManager.GetActivePlan();

        // Assert
        Assert.NotNull(activePlan);
        Assert.NotEqual(Guid.Empty, activePlan.Guid);
        Assert.False(string.IsNullOrEmpty(activePlan.Name));
        Assert.True(activePlan.IsActive);
    }

    [Fact]
    public void EnumeratePlans_ShouldContainExactlyOneActivePlan()
    {
        // Act
        var plans = PowerPlanManager.EnumeratePlans();

        // Assert
        Assert.NotNull(plans);
        Assert.NotEmpty(plans);

        var activePlans = plans.Where(p => p.IsActive).ToList();
        Assert.Single(activePlans);

        var activeFromGet = PowerPlanManager.GetActivePlan();
        Assert.NotNull(activeFromGet);
        Assert.Equal(activeFromGet.Guid, activePlans[0].Guid);
        Assert.Equal(activeFromGet.Name, activePlans[0].Name);
    }

    [Fact]
    public void SetActivePlan_UnknownGuid_ReturnsFalse()
    {
        var result = PowerPlanManager.SetActivePlan(Guid.NewGuid());
        Assert.False(result);
    }
}
