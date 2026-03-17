using AutoPower.Core.Core.Models;
using AutoPower.Core.Detection;
using AutoPower.Core.Infrastructure;
using AutoPower.Core.Power;
using AutoPower.Core.Strategy;

namespace AutoPower.Core.Core;

internal sealed class AppController : IDisposable
{
    private readonly object _lock = new();
    private IdleDetector? _idleDetector;
    private MonitorStateDetector? _monitorStateDetector;
    private Timer? _periodicTimer;
    private Guid? _currentPlanGuid;
    private bool _isIdleFromIdleDetector;
    private bool _isIdleFromMonitorDetector;
    private bool _disposed;

    internal AppState CurrentState { get; private set; } = AppState.Active;
    internal AppConfig Config { get; private set; } = new();

    internal event Action<AppState>? StateChanged;

    internal AppController()
    {
        Config = ConfigService.Load();
    }

    internal void Start()
    {
        lock (_lock)
        {
            InitializeDetectors();
            _periodicTimer = new(
                _ => EvaluateState(),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30)
            );
            LoggerService.Info("AppController started");
        }
    }

    internal void Stop()
    {
        lock (_lock)
        {
            _periodicTimer?.Dispose();
            _periodicTimer = null;
            _idleDetector?.Stop();
            _monitorStateDetector?.Stop();
            LoggerService.Info("AppController stopped");
        }
    }

    internal void SetManualOverride(Guid planGuid, TimeSpan? ttl = null)
    {
        lock (_lock)
        {
            var plans = PowerPlanManager.EnumeratePlans();
            var found = false;
            foreach (var plan in plans)
            {
                if (plan.Guid == planGuid)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                LoggerService.Warn(
                    $"SetManualOverride: Plan GUID {planGuid} not found in enumerated plans"
                );
            }

            Config = Config with
            {
                Override = new()
                {
                    IsActive = true,
                    PlanGuid = planGuid,
                    ExpiresAt = ttl.HasValue ? DateTime.UtcNow + ttl.Value : null,
                },
            };
            ConfigService.Save(Config);
            LoggerService.Info($"Manual override set: plan={planGuid}, ttl={ttl}");
            EvaluateState();
        }
    }

    internal void ClearManualOverride()
    {
        lock (_lock)
        {
            Config = Config with { Override = new() };
            ConfigService.Save(Config);
            LoggerService.Info("Manual override cleared");
            EvaluateState();
        }
    }

    internal void ReloadConfig()
    {
        lock (_lock)
        {
            Config = ConfigService.Load();
            _idleDetector?.Dispose();
            _monitorStateDetector?.Dispose();
            InitializeDetectors();
            LoggerService.Info("Configuration reloaded");
            EvaluateState();
        }
    }

    private void InitializeDetectors()
    {
        _idleDetector?.Dispose();
        _monitorStateDetector?.Dispose();
        _idleDetector = null;
        _monitorStateDetector = null;

        switch (Config.Mode)
        {
            case DetectionMode.KeyboardMouse:
                _idleDetector = new(Config.IdleTimeoutMinutes);
                _idleDetector.IdleStateChanged += OnIdleStateChanged;
                _idleDetector.Start();
                break;

            case DetectionMode.MonitorSleep:
                _monitorStateDetector = new();
                _monitorStateDetector.MonitorStateChanged += OnMonitorStateChanged;
                _monitorStateDetector.Start();
                break;

            case DetectionMode.Both:
                _idleDetector = new(Config.IdleTimeoutMinutes);
                _idleDetector.IdleStateChanged += OnIdleStateChanged;
                _idleDetector.Start();

                _monitorStateDetector = new();
                _monitorStateDetector.MonitorStateChanged += OnMonitorStateChanged;
                _monitorStateDetector.Start();
                break;
        }
    }

    private void OnIdleStateChanged(bool isIdle)
    {
        _isIdleFromIdleDetector = isIdle;
        EvaluateState();
    }

    private void OnMonitorStateChanged(bool isMonitorOff)
    {
        _isIdleFromMonitorDetector = isMonitorOff;
        EvaluateState();
    }

    private void EvaluateState()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            var isIdle = _isIdleFromIdleDetector || _isIdleFromMonitorDetector;
            Guid targetPlanGuid;
            AppState newState;

            if (Config.Override.IsActive)
            {
                if (
                    Config.Override.ExpiresAt.HasValue
                    && DateTime.UtcNow > Config.Override.ExpiresAt.Value
                )
                {
                    LoggerService.Info("Manual override expired");
                    Config = Config with { Override = new() };
                    ConfigService.Save(Config);
                    EvaluateState();
                    return;
                }

                if (Config.Override.PlanGuid.HasValue)
                {
                    targetPlanGuid = Config.Override.PlanGuid.Value;
                    newState = AppState.ManualOverride;
                }
                else
                {
                    targetPlanGuid = isIdle ? Config.IdlePlanGuid : Config.ActivePlanGuid;
                    newState = isIdle ? AppState.Idle : AppState.Active;
                }
            }
            else
            {
                var matchedRule = StrategyEvaluator.Evaluate(Config.Rules, DateTime.Now);
                if (matchedRule != null)
                {
                    targetPlanGuid = matchedRule.TargetPlanGuid;
                    newState = AppState.Active;
                }
                else
                {
                    targetPlanGuid = isIdle ? Config.IdlePlanGuid : Config.ActivePlanGuid;
                    newState = isIdle ? AppState.Idle : AppState.Active;
                }
            }

            if (CurrentState != newState)
            {
                CurrentState = newState;
                StateChanged?.Invoke(CurrentState);
                LoggerService.Info($"State changed to {newState}");
            }

            if (_currentPlanGuid != targetPlanGuid)
            {
                var success = PowerPlanManager.SetActivePlan(targetPlanGuid);
                if (success)
                {
                    LoggerService.Info($"Power plan switched to {targetPlanGuid}");
                    _currentPlanGuid = targetPlanGuid;
                }
                else
                {
                    LoggerService.Error($"Failed to switch power plan to {targetPlanGuid}");
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        Stop();
        _idleDetector?.Dispose();
        _monitorStateDetector?.Dispose();
        _periodicTimer?.Dispose();
    }
}
