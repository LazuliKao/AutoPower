using AutoPower.Core.Core.Models;

namespace AutoPower.Core.Strategy;

public static class PreviewEngine
{
    public sealed record TimelineEntry(
        DateTime Time,
        string PlanName,
        Guid PlanGuid,
        string Source
    );

    public static List<TimelineEntry> GenerateTimeline(
        AppConfig config,
        IReadOnlyList<PowerPlanInfo> plans,
        DateTime from,
        int hours = 24,
        StrategyEvaluationContext? snapshot = null
    )
    {
        from = from.ToUniversalTime();

        var entries = new List<TimelineEntry>();
        var until = from.AddHours(hours);
        var baseSnapshot = snapshot ?? CreateSnapshot(config, from.ToLocalTime());

        string ResolvePlanName(Guid guid)
        {
            foreach (var p in plans)
            {
                if (p.Guid == guid)
                    return p.Name;
            }

            return guid.ToString("B");
        }

        if (config.Override.IsActive && config.Override.PlanGuid.HasValue)
        {
            var overrideEnd = config.Override.ExpiresAt;
            if (overrideEnd is null || overrideEnd.Value > from)
            {
                var effectiveEnd =
                    overrideEnd.HasValue && overrideEnd.Value < until ? overrideEnd.Value : (DateTime?)null;

                entries.Add(
                    new TimelineEntry(
                        from.ToLocalTime(),
                        ResolvePlanName(config.Override.PlanGuid.Value),
                        config.Override.PlanGuid.Value,
                        effectiveEnd.HasValue
                            ? $"Override (until {effectiveEnd.Value.ToLocalTime():g})"
                            : "Override (no expiry)"
                    )
                );

                if (effectiveEnd is null || effectiveEnd >= until)
                    return entries;

                from = effectiveEnd.Value;
            }
        }

        Guid? lastPlanGuid = null;
        string? lastSource = null;
        var fromLocal = from.ToLocalTime();
        var untilLocal = until.ToLocalTime();
        var checkpoints = CollectCheckPoints(config.Rules, fromLocal, untilLocal);

        foreach (var checkpointLocal in checkpoints)
        {
            var decision = StrategyEvaluator.Resolve(
                config,
                baseSnapshot with
                {
                    Now = checkpointLocal,
                }
            );
            var source = decision.IsRuntimeDependent ? $"{decision.Source} (runtime snapshot)" : decision.Source;

            if (decision.PlanGuid != lastPlanGuid || source != lastSource)
            {
                entries.Add(
                    new TimelineEntry(
                        checkpointLocal,
                        ResolvePlanName(decision.PlanGuid),
                        decision.PlanGuid,
                        source
                    )
                );
                lastPlanGuid = decision.PlanGuid;
                lastSource = source;
            }
        }

        return entries;
    }

    private static StrategyEvaluationContext CreateSnapshot(AppConfig config, DateTime now)
    {
        return new()
        {
            Now = now,
            IsKeyboardMouseDetectionEnabled = config.Mode is DetectionMode.KeyboardMouse or DetectionMode.Both,
            IsMonitorDetectionEnabled = config.Mode is DetectionMode.MonitorSleep or DetectionMode.Both,
        };
    }

    private static List<DateTime> CollectCheckPoints(
        IReadOnlyList<StrategyRule> rules,
        DateTime from,
        DateTime until
    )
    {
        var points = new SortedSet<DateTime> { from };
        var day = from.Date;
        var ranges = StrategyEvaluator.CollectTimeRanges(rules);

        while (day <= until.Date)
        {
            var dayStart = day;
            if (dayStart >= from && dayStart <= until)
                points.Add(dayStart);

            var midnight = day.AddDays(1);
            if (midnight >= from && midnight <= until)
                points.Add(midnight);

            foreach (var range in ranges)
            {
                var rangeStart = day.Add(range.Start.ToTimeSpan());
                if (rangeStart >= from && rangeStart <= until)
                    points.Add(rangeStart);

                var beforeStart = rangeStart.AddMinutes(-1);
                if (beforeStart >= from && beforeStart <= until)
                    points.Add(beforeStart);

                var rangeEnd = day.Add(range.End.ToTimeSpan());
                if (rangeEnd >= from && rangeEnd <= until)
                    points.Add(rangeEnd);

                var afterEnd = rangeEnd.AddMinutes(1);
                if (afterEnd >= from && afterEnd <= until)
                    points.Add(afterEnd);
            }

            day = day.AddDays(1);
        }

        return points.ToList();
    }
}
