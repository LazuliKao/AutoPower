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
    private bool? _isKeyboardMouseIdle;
    private bool? _isMonitorOff;
    private bool _disposed;

    internal AppState CurrentState { get; private set; } = AppState.Active;
    internal AppConfig Config { get; private set; } = new();

    internal event Action<AppState>? StateChanged;

    internal AppController()
    {
        Config = ConfigService.Load();
        EnsureConfigPlanGuids();
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
            EnsureConfigPlanGuids();
            _idleDetector?.Dispose();
            _monitorStateDetector?.Dispose();
            InitializeDetectors();
            LoggerService.Info("Configuration reloaded");
            EvaluateState();
        }
    }

    private void EnsureConfigPlanGuids()
    {
        var activeMissing = Config.ActivePlanGuid == Guid.Empty;
        var idleMissing = Config.IdlePlanGuid == Guid.Empty;
        if (!activeMissing && !idleMissing)
        {
            return;
        }

        var plans = PowerPlanManager.EnumeratePlans();

        Guid fallbackPlanGuid = Guid.Empty;
        foreach (var plan in plans)
        {
            if (plan.IsActive)
            {
                fallbackPlanGuid = plan.Guid;
                break;
            }

            if (fallbackPlanGuid == Guid.Empty)
            {
                fallbackPlanGuid = plan.Guid;
            }
        }

        if (fallbackPlanGuid == Guid.Empty)
        {
            LoggerService.Warn("No power plans found; cannot auto-fill missing plan GUIDs");
            return;
        }

        var normalizedConfig = Config with
        {
            ActivePlanGuid = activeMissing ? fallbackPlanGuid : Config.ActivePlanGuid,
            IdlePlanGuid = idleMissing ? fallbackPlanGuid : Config.IdlePlanGuid,
        };

        Config = normalizedConfig;
        ConfigService.Save(Config);

        LoggerService.Warn(
            $"Detected empty power plan GUID in config. Auto-filled missing values with {fallbackPlanGuid}"
        );
    }

    private void InitializeDetectors()
    {
        _idleDetector?.Dispose();
        _monitorStateDetector?.Dispose();
        _idleDetector = null;
        _monitorStateDetector = null;
        _isKeyboardMouseIdle = null;
        _isMonitorOff = null;

        switch (Config.Mode)
        {
            case DetectionMode.KeyboardMouse:
                _idleDetector = new(Config.IdleTimeoutMinutes * 60);
                _idleDetector.IdleStateChanged += OnIdleStateChanged;
                _idleDetector.Start();
                break;

            case DetectionMode.MonitorSleep:
                _monitorStateDetector = new();
                _monitorStateDetector.MonitorStateChanged += OnMonitorStateChanged;
                _monitorStateDetector.Start();
                break;

            case DetectionMode.Both:
                _idleDetector = new(Config.IdleTimeoutMinutes * 60);
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
        _isKeyboardMouseIdle = isIdle;
        EvaluateState();
    }

    private void OnMonitorStateChanged(bool isMonitorOff)
    {
        _isMonitorOff = isMonitorOff;
        EvaluateState();
    }

    private void EvaluateState()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            Guid targetPlanGuid;
            AppState newState;
            string source;

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
                    source = "Manual override";
                }
                else
                {
                    var decision = StrategyEvaluator.Resolve(Config, BuildEvaluationContext(DateTime.Now));
                    targetPlanGuid = decision.PlanGuid;
                    newState = decision.State;
                    source = decision.Source;
                }
            }
            else
            {
                var decision = StrategyEvaluator.Resolve(Config, BuildEvaluationContext(DateTime.Now));
                targetPlanGuid = decision.PlanGuid;
                newState = decision.State;
                source = decision.Source;
            }

            if (CurrentState != newState)
            {
                CurrentState = newState;
                StateChanged?.Invoke(CurrentState);
                LoggerService.Info($"State changed to {newState} ({source})");
            }

            if (_currentPlanGuid != targetPlanGuid)
            {
                var success = PowerPlanManager.SetActivePlan(targetPlanGuid);
                if (success)
                {
                    LoggerService.Info($"Power plan switched to {targetPlanGuid} ({source})");
                    _currentPlanGuid = targetPlanGuid;
                }
                else
                {
                    LoggerService.Error($"Failed to switch power plan to {targetPlanGuid} ({source})");
                }
            }
        }
    }

    private StrategyEvaluationContext BuildEvaluationContext(DateTime now)
    {
        return new()
        {
            Now = now,
            IsKeyboardMouseDetectionEnabled = Config.Mode is DetectionMode.KeyboardMouse or DetectionMode.Both,
            IsMonitorDetectionEnabled = Config.Mode is DetectionMode.MonitorSleep or DetectionMode.Both,
            IsKeyboardMouseIdle = _isKeyboardMouseIdle,
            IsMonitorOff = _isMonitorOff,
        };
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
