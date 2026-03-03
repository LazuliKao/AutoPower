using System.Runtime.InteropServices;

namespace AutoPower.Infrastructure.Win32;

internal static partial class PowrProf
{
    public const uint ACCESS_SCHEME = 16;
    public const uint ERROR_SUCCESS = 0;
    public const uint ERROR_NO_MORE_ITEMS = 259;

    public static readonly Guid GUID_HIGH_PERFORMANCE = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
    public static readonly Guid GUID_BALANCED = new("381b4222-f694-41f0-9685-ff5bb260df2e");
    public static readonly Guid GUID_POWER_SAVER = new("a1841308-3541-4fab-bc81-f71556f20b4a");

    [LibraryImport("powrprof.dll")]
    public static partial uint PowerEnumerate(
        IntPtr rootPowerKey,
        IntPtr schemeGuid,
        IntPtr subGroupOfPowerSettingsGuid,
        uint accessFlags,
        uint index,
        ref Guid buffer,
        ref uint bufferSize
    );

    [LibraryImport("powrprof.dll")]
    public static partial uint PowerGetActiveScheme(
        IntPtr userRootPowerKey,
        out IntPtr activePolicyGuid
    );

    [LibraryImport("powrprof.dll")]
    public static partial uint PowerSetActiveScheme(IntPtr userRootPowerKey, ref Guid schemeGuid);

    [LibraryImport("powrprof.dll")]
    public static partial uint PowerReadFriendlyName(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        IntPtr subGroupOfPowerSettingsGuid,
        IntPtr powerSettingGuid,
        byte[] buffer,
        ref uint bufferSize
    );

    [LibraryImport("kernel32.dll")]
    public static partial IntPtr LocalFree(IntPtr hMem);
}
