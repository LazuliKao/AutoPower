using System.Runtime.InteropServices;

namespace AutoPower.Core.Infrastructure.Win32;

internal static partial class Kernel32
{
    public const uint ERROR_ALREADY_EXISTS = 183;

    [LibraryImport("kernel32.dll")]
    public static partial uint GetTickCount();

    [LibraryImport("kernel32.dll")]
    public static partial ulong GetTickCount64();

    [LibraryImport("kernel32.dll")]
    public static partial uint GetLastError();

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr CreateMutexW(
        IntPtr lpMutexAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bInitialOwner,
        string lpName
    );
}
