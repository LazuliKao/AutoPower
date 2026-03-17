using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using AutoPower.Core.Core.Models;
using AutoPower.Core.Infrastructure.Win32;

namespace AutoPower.Core.Power;

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
