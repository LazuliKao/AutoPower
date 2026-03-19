using System.Runtime.Versioning;
using AutoPower.Core.Core;
using AutoPower.Core.Infrastructure;
using AutoPower.Core.Power;
using AutoPower.UI;
using Kernel32 = AutoPower.Core.Infrastructure.Win32.Kernel32;
using User32 = AutoPower.Core.Infrastructure.Win32.User32;

[assembly: SupportedOSPlatform("windows")]


#if !DEBUG
AutoPower.Infrastructure.AdminElevationManager.RequestElevationIfNeeded();
#endif

const string SingleInstanceMutexName = "AutoPower_SingleInstance";
const string OpenSettingsEventName = "AutoPower_OpenSettings_Request";

var mutex = Kernel32.CreateMutexW(IntPtr.Zero, false, SingleInstanceMutexName);
if (mutex != IntPtr.Zero && Kernel32.GetLastError() == Kernel32.ERROR_ALREADY_EXISTS)
{
    try
    {
        User32.AllowSetForegroundWindow(User32.ASFW_ANY);

        using var openSettingsEvent = EventWaitHandle.OpenExisting(OpenSettingsEventName);
        openSettingsEvent.Set();
    }
    catch (WaitHandleCannotBeOpenedException)
    {
    }
    catch (Exception ex)
    {
        LoggerService.Error("Failed to notify existing instance to open settings", ex);
    }

    return;
}

LoggerService.Info("AutoPower starting...");

try
{
    using var controller = new AppController();
    using var tray = new TrayIcon();
    var settingsWindow = new Lazy<SettingsWindow>(() =>
    {
        var w = new SettingsWindow();
        w.OnConfigSaved += config =>
        {
            ConfigService.Save(config);
            controller.ReloadConfig();
        };
        w.OnNotificationRequested += (title, message) =>
        {
            tray.ShowBalloon(title, message);
        };

        return w;
    });
    var exitSignal = new ManualResetEventSlim(false);

    tray.OnOpenSettings += () =>
        settingsWindow.Value.Show(controller.Config, PowerPlanManager.EnumeratePlans());
    tray.OnExit += () => exitSignal.Set();
    tray.OnClearOverride += () => controller.ClearManualOverride();

    controller.StateChanged += state =>
    {
        tray.UpdateState(state);
        tray.UpdateTooltip($"AutoPower - {state}");
    };

    using var openSettingsEvent = new EventWaitHandle(
        false,
        EventResetMode.AutoReset,
        OpenSettingsEventName
    );

    controller.Start();
    tray.Create(IntPtr.Zero);
    tray.UpdateState(controller.CurrentState);

    var openSettingsSignalRegistration = ThreadPool.RegisterWaitForSingleObject(
        openSettingsEvent,
        static (state, timedOut) =>
        {
            if (timedOut)
            {
                return;
            }

            if (state is TrayIcon trayIcon)
            {
                trayIcon.RequestOpenSettings();
            }
        },
        tray,
        Timeout.Infinite,
        false
    );

    exitSignal.Wait();

    openSettingsSignalRegistration.Unregister(null);
    controller.Stop();
    LoggerService.Info("AutoPower shutting down.");
}
catch (Exception ex)
{
    LoggerService.Error("Unhandled exception in main loop", ex);
}
