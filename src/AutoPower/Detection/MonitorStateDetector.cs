using System.Runtime.InteropServices;
using AutoPower.Infrastructure.Win32;

namespace AutoPower.Detection;

internal sealed class MonitorStateDetector : IDisposable
{
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private readonly object _lock = new();
    private Thread? _thread;
    private IntPtr _hwnd;
    private IntPtr _hPowerNotify;
    private bool _disposed;

    private static WndProcDelegate? _wndProcDelegate;
    private static IntPtr _wndProcPointer;
    private static volatile MonitorStateDetector? _instance;

    public event Action<bool>? MonitorStateChanged;

    internal void Start()
    {
        lock (_lock)
        {
            if (_thread != null)
                return;

            _instance = this;

            _thread = new(MessageLoop) { IsBackground = true, Name = "MonitorStateDetector" };
            _thread.Start();
        }
    }

    internal void Stop()
    {
        if (_hwnd != IntPtr.Zero)
        {
            User32.PostMessageW(_hwnd, User32.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }
    }

    private void MessageLoop()
    {
        _wndProcDelegate = WndProc;
        _wndProcPointer = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);

        var classNamePtr = Marshal.StringToHGlobalUni("AutoPower_MonitorStateDetector");
        try
        {
            var wc = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                lpfnWndProc = _wndProcPointer,
                hInstance = IntPtr.Zero,
                lpszClassName = classNamePtr,
            };

            if (User32.RegisterClassExW(ref wc) == 0)
            {
                return;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(classNamePtr);
        }

        _hwnd = User32.CreateWindowExW(
            User32.WS_EX_NOACTIVATE,
            "AutoPower_MonitorStateDetector",
            "AutoPower_MonitorStateDetector",
            0,
            0,
            0,
            0,
            0,
            User32.HWND_MESSAGE,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero
        );

        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        var guid = User32.GUID_CONSOLE_DISPLAY_STATE;
        _hPowerNotify = User32.RegisterPowerSettingNotification(
            _hwnd,
            ref guid,
            User32.DEVICE_NOTIFY_WINDOW_HANDLE
        );

        while (User32.GetMessageW(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            User32.TranslateMessage(ref msg);
            User32.DispatchMessageW(ref msg);
        }
    }

    private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        var instance = _instance;
        if (instance == null)
            return User32.DefWindowProcW(hWnd, msg, (UIntPtr)wParam, lParam);

        if (msg == User32.WM_POWERBROADCAST && wParam == (IntPtr)User32.PBT_POWERSETTINGCHANGE)
        {
            var setting = Marshal.PtrToStructure<POWERBROADCAST_SETTING>(lParam);
            if (setting.PowerSetting == User32.GUID_CONSOLE_DISPLAY_STATE)
            {
                var isMonitorOff = setting.Data == 0;
                instance.MonitorStateChanged?.Invoke(isMonitorOff);
            }
        }
        return User32.DefWindowProcW(hWnd, msg, (UIntPtr)wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_hwnd != IntPtr.Zero && _hPowerNotify != IntPtr.Zero)
        {
            User32.UnregisterPowerSettingNotification(_hPowerNotify);
            _hPowerNotify = IntPtr.Zero;
        }

        if (_hwnd != IntPtr.Zero)
        {
            User32.PostMessageW(_hwnd, User32.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            _hwnd = IntPtr.Zero;
        }

        _wndProcDelegate = null;
    }
}
