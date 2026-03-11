using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using AutoPower.Core.Models;
using AutoPower.Infrastructure.Win32;

namespace AutoPower.Power;

internal static class PowerPlanManager
{
    internal static List<PowerPlanInfo> EnumeratePlans()
    {
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
        var result = PowrProf.PowerSetActiveScheme(IntPtr.Zero, ref planGuid);
        return result == PowrProf.ERROR_SUCCESS;
    }

    private static string GetPlanName(Guid schemeGuid)
    {
        var buffer = new byte[512];
        var bufferSize = (uint)buffer.Length;

        var result = PowrProf.PowerReadFriendlyName(
            IntPtr.Zero,
            ref schemeGuid,
            IntPtr.Zero,
            IntPtr.Zero,
            buffer,
            ref bufferSize
        );

        if (result != PowrProf.ERROR_SUCCESS || bufferSize == 0)
            return string.Empty;

        return Encoding.Unicode.GetString(buffer, 0, (int)bufferSize).TrimEnd('\0');
    }
}
