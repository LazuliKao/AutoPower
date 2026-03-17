using AutoPower.Core.Core;
using AutoPower.Core.Core.Models;
using AutoPower.Core.Strategy;
using Xunit.Abstractions;

namespace AutoPower.Tests.Manual;

public class DebugTests(ITestOutputHelper logger) : IDisposable
{
    [Fact]
    public void TestTimeline()
    {
        var config = ConfigService.Load();

        logger.WriteLine($"Config loaded from: {ConfigService.ConfigFilePath}");
        logger.WriteLine($"  ActivePlanGuid : {config.ActivePlanGuid}");
        logger.WriteLine($"  IdlePlanGuid   : {config.IdlePlanGuid}");
        logger.WriteLine($"  Rules          : {config.Rules.Count}");
        logger.WriteLine($"  Override active: {config.Override.IsActive}");
        logger.WriteLine("");

        var knownGuids = new HashSet<Guid> { config.ActivePlanGuid, config.IdlePlanGuid };
        foreach (var rule in config.Rules)
            knownGuids.Add(rule.TargetPlanGuid);
        if (config.Override.PlanGuid.HasValue)
            knownGuids.Add(config.Override.PlanGuid.Value);

        var plans = knownGuids
            .Select(g => new PowerPlanInfo(g, $"Plan {g:B}", g == config.ActivePlanGuid))
            .ToList();

        var from = DateTime.Now;
        var timeline = PreviewEngine.GenerateTimeline(config, plans, from, hours: 24);

        logger.WriteLine($"Timeline from {from:g} (next 24 hours) — {timeline.Count} transition(s):");
        logger.WriteLine(new string('-', 72));

        foreach (var entry in timeline)
        {
            logger.WriteLine($"  {entry.Time:ddd MM/dd HH:mm}  |  {entry.PlanName,-36}  |  {entry.Source}");
        }

        logger.WriteLine(new string('-', 72));

        Assert.NotEmpty(timeline);
    }

    public void Dispose() { }
}
