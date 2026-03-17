using AutoPower.Core.Core.Models;

namespace AutoPower.Core.Strategy;

/// <summary>
/// Computes a preview timeline of upcoming power plan transitions
/// based on the current configuration (override, schedule rules, defaults).
/// </summary>
public static class PreviewEngine
{
    public sealed record TimelineEntry(
        DateTime Time,
        string PlanName,
        Guid PlanGuid,
        string Source
    );

    /// <summary>
    /// Generates a timeline of plan transitions for the next <paramref name="hours"/> hours
    /// starting from <paramref name="from"/>.
    /// </summary>
    public static List<TimelineEntry> GenerateTimeline(
        AppConfig config,
        IReadOnlyList<PowerPlanInfo> plans,
        DateTime from,
        int hours = 24
    )
    {
        var entries = new List<TimelineEntry>();
        var until = from.AddHours(hours);

        string ResolvePlanName(Guid guid)
        {
            foreach (var p in plans)
            {
                if (p.Guid == guid)
                    return p.Name;
            }
            return guid.ToString("B");
        }

        // Phase 1: Override (if active and not expired)
        if (config.Override.IsActive && config.Override.PlanGuid.HasValue)
        {
            var overrideEnd = config.Override.ExpiresAt;
            if (overrideEnd is null || overrideEnd > from)
            {
                var effectiveEnd = overrideEnd.HasValue && overrideEnd.Value < until
                    ? overrideEnd.Value
                    : (DateTime?)null;

                entries.Add(new TimelineEntry(
                    from,
                    ResolvePlanName(config.Override.PlanGuid.Value),
                    config.Override.PlanGuid.Value,
                    effectiveEnd.HasValue
                        ? $"Override (until {effectiveEnd.Value.ToLocalTime():g})"
                        : "Override (no expiry)"
                ));

                // If override covers the entire window, return early
                if (effectiveEnd is null || effectiveEnd >= until)
                    return entries;

                // Shift start to after override ends
                from = effectiveEnd.Value;
            }
        }

        // Phase 2: Walk through time in 1-minute granularity collecting transitions
        // We track the "current" plan and emit an entry whenever it changes.
        var enabledRules = config.Rules
            .Where(r => r.IsEnabled)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ThenBy(r => r.Id.ToString())
            .ToList();

        Guid? lastPlanGuid = null;
        string? lastSource = null;

        // Collect all rule boundary times within the window to minimize iterations
        var checkPoints = CollectCheckPoints(enabledRules, from, until);

        foreach (var checkpoint in checkPoints)
        {
            var (planGuid, source) = EvaluateAt(config, enabledRules, plans, checkpoint, ResolvePlanName);

            if (planGuid != lastPlanGuid || source != lastSource)
            {
                entries.Add(new TimelineEntry(
                    checkpoint,
                    ResolvePlanName(planGuid),
                    planGuid,
                    source
                ));
                lastPlanGuid = planGuid;
                lastSource = source;
            }
        }

        return entries;
    }

    private static (Guid planGuid, string source) EvaluateAt(
        AppConfig config,
        List<StrategyRule> enabledRules,
        IReadOnlyList<PowerPlanInfo> plans,
        DateTime time,
        Func<Guid, string> resolveName)
    {
        var currentTime = TimeOnly.FromDateTime(time);
        var dayOfWeek = time.DayOfWeek;

        foreach (var rule in enabledRules)
        {
            if (!MatchesDayType(rule.DayType, dayOfWeek))
                continue;
            if (!MatchesTimeRange(rule.Start, rule.End, currentTime))
                continue;

            return (rule.TargetPlanGuid, $"Rule: {rule.Name}");
        }

        // No rule matched — use default active plan
        return (config.ActivePlanGuid, "Default (Active plan)");
    }

    /// <summary>
    /// Collects all meaningful time checkpoints (rule start/end boundaries and day transitions)
    /// within the window [from, until], so we don't have to iterate minute-by-minute.
    /// </summary>
    private static List<DateTime> CollectCheckPoints(
        List<StrategyRule> rules,
        DateTime from,
        DateTime until)
    {
        var points = new SortedSet<DateTime> { from };

        // Walk each day in the window
        var day = from.Date;
        while (day <= until.Date)
        {
            // Add day boundaries
            var dayStart = day;
            if (dayStart >= from && dayStart <= until)
                points.Add(dayStart);

            // Midnight boundary for overnight rules
            var midnight = day.AddDays(1);
            if (midnight >= from && midnight <= until)
                points.Add(midnight);

            foreach (var rule in rules)
            {
                // Rule start time on this day
                var ruleStart = day.Add(rule.Start.ToTimeSpan());
                if (ruleStart >= from && ruleStart <= until)
                    points.Add(ruleStart);

                // 1 minute before rule start (transition point)
                var beforeStart = ruleStart.AddMinutes(-1);
                if (beforeStart >= from && beforeStart <= until)
                    points.Add(beforeStart);

                // Rule end time on this day
                var ruleEnd = day.Add(rule.End.ToTimeSpan());
                if (ruleEnd >= from && ruleEnd <= until)
                    points.Add(ruleEnd);

                // 1 minute after rule end (transition out)
                var afterEnd = ruleEnd.AddMinutes(1);
                if (afterEnd >= from && afterEnd <= until)
                    points.Add(afterEnd);
            }

            day = day.AddDays(1);
        }

        return points.ToList();
    }

    private static bool MatchesDayType(DayType dayType, DayOfWeek dayOfWeek)
    {
        return dayType switch
        {
            DayType.All => true,
            DayType.Weekday => dayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday,
            DayType.Weekend => dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
            _ => false,
        };
    }

    private static bool MatchesTimeRange(TimeOnly start, TimeOnly end, TimeOnly current)
    {
        if (start <= end)
        {
            return current >= start && current <= end;
        }

        // Overnight range (e.g., 22:00 → 06:00)
        return current >= start || current <= end;
    }
}
