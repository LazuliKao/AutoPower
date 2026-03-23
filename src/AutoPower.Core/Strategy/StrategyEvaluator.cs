using AutoPower.Core.Core.Models;

namespace AutoPower.Core.Strategy;

internal static class StrategyEvaluator
{
    internal static StrategyEvaluationContext BuildLiveContext(
        AppConfig config,
        DateTime now,
        bool? isKeyboardMouseIdle,
        bool? isMonitorOff
    )
    {
        return new()
        {
            Now = now,
            IsKeyboardMouseDetectionEnabled = config.Mode is DetectionMode.KeyboardMouse or DetectionMode.Both,
            IsMonitorDetectionEnabled = config.Mode is DetectionMode.MonitorSleep or DetectionMode.Both,
            IsKeyboardMouseIdle = isKeyboardMouseIdle,
            IsMonitorOff = isMonitorOff,
        };
    }

    internal static StrategyEvaluationContext BuildPreviewSnapshot(AppConfig config, DateTime now)
    {
        return new()
        {
            Now = now,
            IsKeyboardMouseDetectionEnabled = config.Mode is DetectionMode.KeyboardMouse or DetectionMode.Both,
            IsMonitorDetectionEnabled = config.Mode is DetectionMode.MonitorSleep or DetectionMode.Both,
        };
    }

    internal enum ConditionMatchResult
    {
        False,
        True,
        Unknown,
    }

    internal static StrategyDecision Resolve(AppConfig config, StrategyEvaluationContext context)
    {
        if (config.DecisionTree is not null)
        {
            var decision = StrategyDecisionNodeEvaluator.Evaluate(config.DecisionTree, context);
            if (decision is not null)
            {
                return decision;
            }
        }

        if (config.DefaultPlanGuid.HasValue && config.DefaultPlanGuid.Value != Guid.Empty)
        {
            return new()
            {
                PlanGuid = config.DefaultPlanGuid.Value,
                State = AppState.Active,
                Source = "Default plan",
                IsDefault = true,
            };
        }

        var isIdle = ResolveFallbackIdle(config.Mode, context);
        return new()
        {
            PlanGuid = isIdle ? config.IdlePlanGuid : config.ActivePlanGuid,
            State = isIdle ? AppState.Idle : AppState.Active,
            Source = isIdle ? "Fallback: Idle plan" : "Fallback: Active plan",
            IsFallback = true,
            IsRuntimeDependent = true,
        };
    }

    private static bool ResolveFallbackIdle(DetectionMode mode, StrategyEvaluationContext context)
    {
        var keyboardMouseIdle = mode is DetectionMode.KeyboardMouse or DetectionMode.Both
            && context.IsKeyboardMouseIdle == true;
        var monitorIdle = mode is DetectionMode.MonitorSleep or DetectionMode.Both
            && context.IsMonitorOff == true;

        return keyboardMouseIdle || monitorIdle;
    }

    internal static (ConditionMatchResult Result, bool IsRuntimeDependent) EvaluateGroup(
        StrategyConditionGroup? group,
        StrategyEvaluationContext context
    )
    {
        if (group is null)
        {
            return (ConditionMatchResult.True, false);
        }

        var results = new List<ConditionMatchResult>();
        var isRuntimeDependent = false;

        foreach (var condition in group.Conditions)
        {
            var evaluation = EvaluateCondition(condition, context);
            results.Add(evaluation.Result);
            isRuntimeDependent |= evaluation.IsRuntimeDependent;
        }

        foreach (var childGroup in group.Groups)
        {
            var evaluation = EvaluateGroup(childGroup, context);
            results.Add(evaluation.Result);
            isRuntimeDependent |= evaluation.IsRuntimeDependent;
        }

        return (Combine(group.Operator, results), isRuntimeDependent);
    }

    private static (ConditionMatchResult Result, bool IsRuntimeDependent) EvaluateCondition(
        StrategyCondition condition,
        StrategyEvaluationContext context
    )
    {
        return condition.Type switch
        {
            StrategyConditionType.DayType =>
                (MatchesDayType(condition.DayType, context.Now.DayOfWeek)
                    ? ConditionMatchResult.True
                    : ConditionMatchResult.False, false),
            StrategyConditionType.TimeRange =>
                (MatchesTimeRange(condition.Start, condition.End, TimeOnly.FromDateTime(context.Now))
                    ? ConditionMatchResult.True
                    : ConditionMatchResult.False, false),
            StrategyConditionType.KeyboardMouseIdle =>
                EvaluateRuntimeCondition(
                    context.IsKeyboardMouseDetectionEnabled,
                    context.IsKeyboardMouseIdle
                ),
            StrategyConditionType.MonitorOff =>
                EvaluateRuntimeCondition(context.IsMonitorDetectionEnabled, context.IsMonitorOff),
            _ => (ConditionMatchResult.False, false),
        };
    }

    private static (ConditionMatchResult Result, bool IsRuntimeDependent) EvaluateRuntimeCondition(
        bool isEnabled,
        bool? state
    )
    {
        if (!isEnabled || !state.HasValue)
        {
            return (ConditionMatchResult.Unknown, true);
        }

        return (state.Value ? ConditionMatchResult.True : ConditionMatchResult.False, true);
    }

    private static ConditionMatchResult Combine(
        StrategyConditionGroupOperator groupOperator,
        List<ConditionMatchResult> results
    )
    {
        if (results.Count == 0)
        {
            return groupOperator switch
            {
                StrategyConditionGroupOperator.Any => ConditionMatchResult.False,
                StrategyConditionGroupOperator.All => ConditionMatchResult.True,
                StrategyConditionGroupOperator.None => ConditionMatchResult.True,
                _ => ConditionMatchResult.False,
            };
        }

        return groupOperator switch
        {
            StrategyConditionGroupOperator.All => CombineAll(results),
            StrategyConditionGroupOperator.Any => CombineAny(results),
            StrategyConditionGroupOperator.None => CombineNone(results),
            _ => ConditionMatchResult.False,
        };
    }

    private static ConditionMatchResult CombineAll(List<ConditionMatchResult> results)
    {
        var hasUnknown = false;
        foreach (var result in results)
        {
            if (result == ConditionMatchResult.False)
            {
                return ConditionMatchResult.False;
            }

            hasUnknown |= result == ConditionMatchResult.Unknown;
        }

        return hasUnknown ? ConditionMatchResult.Unknown : ConditionMatchResult.True;
    }

    private static ConditionMatchResult CombineAny(List<ConditionMatchResult> results)
    {
        var hasUnknown = false;
        foreach (var result in results)
        {
            if (result == ConditionMatchResult.True)
            {
                return ConditionMatchResult.True;
            }

            hasUnknown |= result == ConditionMatchResult.Unknown;
        }

        return hasUnknown ? ConditionMatchResult.Unknown : ConditionMatchResult.False;
    }

    private static ConditionMatchResult CombineNone(List<ConditionMatchResult> results)
    {
        var hasUnknown = false;
        foreach (var result in results)
        {
            if (result == ConditionMatchResult.True)
            {
                return ConditionMatchResult.False;
            }

            hasUnknown |= result == ConditionMatchResult.Unknown;
        }

        return hasUnknown ? ConditionMatchResult.Unknown : ConditionMatchResult.True;
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

        return current >= start || current <= end;
    }
}
