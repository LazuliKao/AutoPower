using AutoPower.Core.Models;

namespace AutoPower.Strategy;

internal static class StrategyEvaluator
{
    internal static StrategyRule? Evaluate(IReadOnlyList<StrategyRule> rules, DateTime now)
    {
        var currentTime = TimeOnly.FromDateTime(now);
        var dayOfWeek = now.DayOfWeek;

        var matches = rules
            .Where(r => r.IsEnabled)
            .Where(r => MatchesDayType(r.DayType, dayOfWeek))
            .Where(r => MatchesTimeRange(r.Start, r.End, currentTime))
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ThenBy(r => r.Id.ToString())
            .ToList();

        return matches.Count > 0 ? matches[0] : null;
    }

    private static bool MatchesDayType(Core.Models.DayType dayType, DayOfWeek dayOfWeek)
    {
        return dayType switch
        {
            Core.Models.DayType.All => true,
            Core.Models.DayType.Weekday => dayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday,
            Core.Models.DayType.Weekend => dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
            _ => false
        };
    }

    private static bool MatchesTimeRange(TimeOnly start, TimeOnly end, TimeOnly current)
    {
        if (start <= end)
        {
            return current >= start && current <= end;
        }

        return current >= start || current <= end;
    }
}
