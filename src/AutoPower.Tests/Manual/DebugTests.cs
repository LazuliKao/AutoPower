using AutoPower.Core.Core;
using AutoPower.Core.Core.Models;
using AutoPower.Core.Detection;
using AutoPower.Core.Strategy;
using Xunit.Abstractions;

namespace AutoPower.Tests.Manual;

public class DebugTests(ITestOutputHelper logger) : IDisposable
{
    private readonly object _logLock = new();

    private void Log(string message)
    {
        lock (_logLock)
            logger.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    [Fact]
    public void TestTimeline()
    {
        var config = ConfigService.Load();

        logger.WriteLine($"Config loaded from: {ConfigService.ConfigFilePath}");
        logger.WriteLine($"  ActivePlanGuid : {config.ActivePlanGuid}");
        logger.WriteLine($"  IdlePlanGuid   : {config.IdlePlanGuid}");
        logger.WriteLine($"  DefaultPlanGuid: {config.DefaultPlanGuid}");
        logger.WriteLine($"  DecisionTree   : {(config.DecisionTree != null ? "Present" : "None")}");
        logger.WriteLine($"  Override active: {config.Override.IsActive}");
        logger.WriteLine("");

        var knownGuids = new HashSet<Guid> { config.ActivePlanGuid, config.IdlePlanGuid };
        if (config.DefaultPlanGuid.HasValue)
            knownGuids.Add(config.DefaultPlanGuid.Value);
        CollectGuidsFromDecisionTree(config.DecisionTree, knownGuids);
        if (config.Override.PlanGuid.HasValue)
            knownGuids.Add(config.Override.PlanGuid.Value);

        var plans = knownGuids
            .Select(g => new PowerPlanInfo(g, $"Plan {g:B}", g == config.ActivePlanGuid))
            .ToList();

        var from = DateTime.Now;
        var timeline = PreviewEngine.GenerateTimeline(
            config,
            plans,
            from,
            hours: 24,
            snapshot: new StrategyEvaluationContext
            {
                Now = from,
                IsKeyboardMouseDetectionEnabled = config.Mode is DetectionMode.KeyboardMouse or DetectionMode.Both,
                IsMonitorDetectionEnabled = config.Mode is DetectionMode.MonitorSleep or DetectionMode.Both,
                IsKeyboardMouseIdle = false,
                IsMonitorOff = false,
            }
        );

        logger.WriteLine(
            $"Timeline from {from:g} (next 24 hours) — {timeline.Count} transition(s):"
        );
        logger.WriteLine(new string('-', 72));

        foreach (var entry in timeline)
        {
            logger.WriteLine(
                $"  {entry.Time:ddd MM/dd HH:mm}  |  {entry.PlanName, -36}  |  {entry.Source}"
            );
        }

        logger.WriteLine(new string('-', 72));

        Assert.NotEmpty(timeline);
    }

    [Fact]
    public void IdleTest()
    {
        //var config = ConfigService.Load();
        var timeoutSeconds = 5;

        Log(
            $"Starting IdleDetector (timeout={timeoutSeconds}s). Watching for 30s — go idle or move mouse..."
        );

        using var detector = new IdleDetector(timeoutSeconds);
        var done = new ManualResetEventSlim(false);

        detector.IdleStateChanged += isIdle =>
        {
            Log($"Idle state changed → {(isIdle ? "IDLE" : "ACTIVE")}");
        };

        detector.Start();
        done.Wait(TimeSpan.FromSeconds(30));
        detector.Stop();

        Log("IdleTest finished.");
    }

    [Fact]
    public void MonitorStateTest()
    {
        Log("Starting MonitorStateDetector. Watching for 60s — turn monitor off/on...");

        using var detector = new MonitorStateDetector();

        detector.MonitorStateChanged += isOff =>
        {
            Log($"Monitor state changed → {(isOff ? "OFF" : "ON")}");
        };

        detector.Start();
        Thread.Sleep(TimeSpan.FromSeconds(60));
        detector.Stop();

        Log("MonitorStateTest finished.");
    }

    public void Dispose() { }

    private static void CollectGuidsFromDecisionTree(StrategyDecisionNode? node, HashSet<Guid> guids)
    {
        if (node == null)
            return;

        if (node.PlanGuid.HasValue)
            guids.Add(node.PlanGuid.Value);

        if (node.Then is not null)
            CollectGuidsFromDecisionTree(node.Then, guids);

        if (node.Else is not null)
            CollectGuidsFromDecisionTree(node.Else, guids);
    }
}
