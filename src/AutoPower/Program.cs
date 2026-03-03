using System.Runtime.Versioning;
using AutoPower.Core;
using AutoPower.Infrastructure;
using AutoPower.Infrastructure.Win32;
using AutoPower.Power;
using AutoPower.UI;

[assembly: SupportedOSPlatform("windows")]

var mutex = Kernel32.CreateMutexW(IntPtr.Zero, false, "AutoPower_SingleInstance");
if (mutex != IntPtr.Zero && Kernel32.GetLastError() == Kernel32.ERROR_ALREADY_EXISTS)
{
    return;
}

LoggerService.Info("AutoPower starting...");

try
{
    using var controller = new AppController();
    using var tray = new TrayIcon();
    var settingsWindow = new SettingsWindow();
    var exitSignal = new ManualResetEventSlim(false);

    tray.OnOpenSettings += () =>
        settingsWindow.Show(controller.Config, PowerPlanManager.EnumeratePlans());
    tray.OnExit += () => exitSignal.Set();
    tray.OnClearOverride += () => controller.ClearManualOverride();

    settingsWindow.OnConfigSaved += config =>
    {
        ConfigService.Save(config);
        controller.ReloadConfig();
    };

    controller.StateChanged += state =>
    {
        tray.UpdateState(state);
        tray.UpdateTooltip($"AutoPower - {state}");
    };

    controller.Start();
    tray.Create(IntPtr.Zero);
    tray.UpdateState(controller.CurrentState);

    exitSignal.Wait();

    controller.Stop();
    LoggerService.Info("AutoPower shutting down.");
}
catch (Exception ex)
{
    LoggerService.Error("Unhandled exception in main loop", ex);
}
