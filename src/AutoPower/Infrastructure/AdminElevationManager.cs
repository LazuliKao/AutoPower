using System.Diagnostics;
using System.Runtime.Versioning;
using AutoPower.Core.Infrastructure;
using AutoPower.Core.Infrastructure.Win32;
using AutoPower.Infrastructure.Win32;

namespace AutoPower.Infrastructure;

[SupportedOSPlatform("windows")]
internal static class AdminElevationManager
{
    private const int SW_NORMAL = 1;

    internal static bool IsRunningAsAdmin()
    {
        try
        {
            return Shell32.IsUserAnAdmin();
        }
        catch (Exception ex)
        {
            LoggerService.Error("Failed to check admin status", ex);
            return false;
        }
    }

    internal static void RequestElevationIfNeeded()
    {
        if (IsRunningAsAdmin())
        {
            LoggerService.Info("Already running as administrator.");
            return;
        }

        LoggerService.Info("Not running as administrator. Showing elevation prompt...");

        User32.MessageBoxW(
            IntPtr.Zero,
            "AutoPower requires administrator privileges to manage Windows power plans.\n\nYou will be prompted by Windows to allow elevated access.",
            "Administrator Privileges Required",
            User32.MB_OK | User32.MB_ICONINFORMATION
        );

        LoggerService.Info("Requesting elevation via ShellExecute...");

        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
            {
                LoggerService.Error("Cannot determine current executable path.");
                throw new InvalidOperationException("Unable to determine executable path for elevation.");
            }

            Shell32.ShellExecuteW(
                IntPtr.Zero,
                "runas",
                exePath,
                null,
                null,
                SW_NORMAL
            );

            LoggerService.Info("Elevation request submitted. Exiting current process.");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            LoggerService.Error("Failed to request elevation", ex);
            throw;
        }
    }
}
