using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using AutoPower.Core.Core.Models;
using AutoPower.Core.Infrastructure;
using AutoPower.Core.Infrastructure.Win32;

namespace AutoPower.Core.Power;

internal static class PowerPlanManager
{
    private const string GSettingsCommand = "gsettings";
    private const string GnomePowerSchema = "org.gnome.SettingsDaemon.plugins.power";
    private const string GnomePowerKey = "power-profile";

    private static readonly Guid LinuxPowerSaverGuid = new("a1841308-3541-4fab-bc81-f71556f20b4a");
    private static readonly Guid LinuxBalancedGuid = new("381b4222-f694-41f0-9685-ff5bb260df2e");
    private static readonly Guid LinuxPerformanceGuid = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

    internal static List<PowerPlanInfo> EnumeratePlans()
    {
        if (OperatingSystem.IsLinux())
            return EnumerateLinuxPlans();

        if (!OperatingSystem.IsWindows())
            return new();

        var plans = new List<PowerPlanInfo>();
        var activeGuid = GetActivePlan()?.Guid;

        uint index = 0;
        while (true)
        {
            var schemeGuid = Guid.Empty;
            var bufferSize = (uint)Unsafe.SizeOf<Guid>();

            var result = PowrProf.PowerEnumerate(
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                PowrProf.ACCESS_SCHEME,
                index,
                ref schemeGuid,
                ref bufferSize
            );

            if (result == PowrProf.ERROR_NO_MORE_ITEMS)
                break;

            if (result != PowrProf.ERROR_SUCCESS)
                break;

            var name = GetPlanName(schemeGuid);
            var isActive = activeGuid.HasValue && schemeGuid == activeGuid.Value;

            plans.Add(new(schemeGuid, name, isActive));
            index++;
        }

        return plans;
    }

    internal static PowerPlanInfo? GetActivePlan()
    {
        if (OperatingSystem.IsLinux())
            return GetActiveLinuxPlan();

        if (!OperatingSystem.IsWindows())
            return null;

        var result = PowrProf.PowerGetActiveScheme(IntPtr.Zero, out var activeGuidPtr);

        if (result != PowrProf.ERROR_SUCCESS || activeGuidPtr == IntPtr.Zero)
            return null;

        try
        {
            var activeGuid = Marshal.PtrToStructure<Guid>(activeGuidPtr);
            var name = GetPlanName(activeGuid);
            return new(activeGuid, name, true);
        }
        finally
        {
            PowrProf.LocalFree(activeGuidPtr);
        }
    }

    internal static bool SetActivePlan(Guid planGuid)
    {
        if (OperatingSystem.IsLinux())
            return SetActiveLinuxPlan(planGuid);

        if (!OperatingSystem.IsWindows())
            return false;

        var result = PowrProf.PowerSetActiveScheme(IntPtr.Zero, ref planGuid);
        return result == PowrProf.ERROR_SUCCESS;
    }

    private static List<PowerPlanInfo> EnumerateLinuxPlans()
    {
        var activePlan = GetActiveLinuxPlan();
        var activeGuid = activePlan?.Guid ?? LinuxBalancedGuid;
        return new()
        {
            new(LinuxPowerSaverGuid, "Power Saver", activeGuid == LinuxPowerSaverGuid),
            new(LinuxBalancedGuid, "Balanced", activeGuid == LinuxBalancedGuid),
            new(LinuxPerformanceGuid, "Performance", activeGuid == LinuxPerformanceGuid),
        };
    }

    private static PowerPlanInfo GetActiveLinuxPlan()
    {
        if (
            TryRunCommand(
                GSettingsCommand,
                $"get {GnomePowerSchema} {GnomePowerKey}",
                out var output
            )
            && TryParseGnomeProfile(output, out var profile)
        )
        {
            return profile;
        }

        LoggerService.Warn("Unable to read GNOME power profile. Falling back to Balanced.");
        return new(LinuxBalancedGuid, "Balanced", true);
    }

    private static bool SetActiveLinuxPlan(Guid planGuid)
    {
        if (!TryMapGuidToLinuxProfile(planGuid, out var profileName))
        {
            LoggerService.Warn($"Unsupported Linux power plan guid: {planGuid}");
            return false;
        }

        var setCommand = $"set {GnomePowerSchema} {GnomePowerKey} '{profileName}'";
        if (TryRunCommand(GSettingsCommand, setCommand, out _))
            return true;

        LoggerService.Error($"Failed to set GNOME power profile to {profileName}");
        return false;
    }

    private static bool TryMapGuidToLinuxProfile(Guid planGuid, out string profileName)
    {
        if (planGuid == LinuxPowerSaverGuid)
        {
            profileName = "power-saver";
            return true;
        }

        if (planGuid == LinuxBalancedGuid)
        {
            profileName = "balanced";
            return true;
        }

        if (planGuid == LinuxPerformanceGuid)
        {
            profileName = "performance";
            return true;
        }

        profileName = string.Empty;
        return false;
    }

    private static bool TryParseGnomeProfile(string output, out PowerPlanInfo planInfo)
    {
        var normalized = output.Trim().Trim('\'', '"');

        if (string.Equals(normalized, "power-saver", StringComparison.Ordinal))
        {
            planInfo = new(LinuxPowerSaverGuid, "Power Saver", true);
            return true;
        }

        if (string.Equals(normalized, "balanced", StringComparison.Ordinal))
        {
            planInfo = new(LinuxBalancedGuid, "Balanced", true);
            return true;
        }

        if (string.Equals(normalized, "performance", StringComparison.Ordinal))
        {
            planInfo = new(LinuxPerformanceGuid, "Performance", true);
            return true;
        }

        planInfo = new(LinuxBalancedGuid, "Balanced", true);
        return false;
    }

    private static bool TryRunCommand(string fileName, string arguments, out string output)
    {
        output = string.Empty;

        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo = new()
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            if (!process.Start())
                return false;

            var stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                return false;

            output = stdout;
            return true;
        }
        catch (Exception ex)
        {
            LoggerService.Warn($"Failed to run command '{fileName} {arguments}': {ex.Message}");
            return false;
        }
    }

    private static string GetPlanName(Guid schemeGuid)
    {
        // First call with zero-sized buffer to determine required size (in bytes)
        var bufferSize = 0u;
        var result = PowrProf.PowerReadFriendlyName(
            IntPtr.Zero,
            ref schemeGuid,
            IntPtr.Zero,
            IntPtr.Zero,
            Array.Empty<byte>(),
            ref bufferSize
        );

        if (result == PowrProf.ERROR_NO_MORE_ITEMS || bufferSize == 0)
            return string.Empty;

        // If the API indicates more data is required or returned size > 0, allocate buffer
        if (result == PowrProf.ERROR_MORE_DATA || bufferSize > 0)
        {
            var buffer = new byte[bufferSize];
            var secondCallSize = bufferSize;
            var secondResult = PowrProf.PowerReadFriendlyName(
                IntPtr.Zero,
                ref schemeGuid,
                IntPtr.Zero,
                IntPtr.Zero,
                buffer,
                ref secondCallSize
            );

            if (secondResult != PowrProf.ERROR_SUCCESS || secondCallSize == 0)
                return string.Empty;

            return Encoding.Unicode.GetString(buffer, 0, (int)secondCallSize).TrimEnd('\0');
        }

        return string.Empty;
    }
}
