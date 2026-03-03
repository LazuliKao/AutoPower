using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using AutoPower.Core.Models;
using AutoPower.Infrastructure.Win32;

namespace AutoPower.UI;

[SupportedOSPlatform("windows")]
internal sealed class TrayIcon : IDisposable
{
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const int TrayIconId = 1;
    private const int MsgTrayCallback = (int)Shell32.WM_TRAYICON;

    private const int CmdStatus = 0;
    private const int CmdSettings = 1001;
    private const int CmdClearOverride = 1002;
    private const int CmdExit = 1003;

    private static readonly IntPtr IdiApplication = new(32512);

    private static WndProcDelegate? _wndProcDelegate;
    private static IntPtr _wndProcPointer;
    private static volatile TrayIcon? _instance;

    private IntPtr _windowHandle;
    private IntPtr _iconHandle;
    private bool _disposed;
    private AppState _currentState;

    internal event Action? OnOpenSettings;
    internal event Action? OnExit;
    internal event Action? OnClearOverride;

    internal void Create(IntPtr ownerHwnd)
    {
        var thread = new Thread(() => MessageLoop(ownerHwnd))
        {
            IsBackground = true,
            Name = "TrayIconMessageLoop",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    private void MessageLoop(IntPtr ownerHwnd)
    {
        _instance = this;
        _wndProcDelegate = WndProc;
        _wndProcPointer = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);

        _iconHandle = User32.LoadIconW(IntPtr.Zero, IdiApplication);

        var classNamePtr = Marshal.StringToHGlobalUni("AutoPowerTrayIconClass");
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

        _windowHandle = User32.CreateWindowExW(
            User32.WS_EX_NOACTIVATE,
            "AutoPowerTrayIconClass",
            "AutoPower Tray Icon",
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

        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        var notifyData = CreateNotifyIconData();
        if (!Shell32.Shell_NotifyIconW(Shell32.NIM_ADD, ref notifyData))
        {
            return;
        }

        notifyData.uVersionOrTimeout = Shell32.NOTIFYICON_VERSION_4;
        Shell32.Shell_NotifyIconW(Shell32.NIM_SETVERSION, ref notifyData);

        MSG msg;
        while (User32.GetMessageW(out msg, IntPtr.Zero, 0, 0) != 0)
        {
            User32.TranslateMessage(ref msg);
            User32.DispatchMessageW(ref msg);
        }

        Shell_NotifyIconDelete();
    }

    private NOTIFYICONDATA CreateNotifyIconData()
    {
        var notifyData = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _windowHandle,
            uID = TrayIconId,
            uFlags = Shell32.NIF_MESSAGE | Shell32.NIF_ICON | Shell32.NIF_TIP,
            uCallbackMessage = (uint)MsgTrayCallback,
            hIcon = _iconHandle,
        };

        var tip = "AutoPower";
        unsafe
        {
            fixed (char* pTip = tip)
            {
                for (var i = 0; i < tip.Length && i < 127; i++)
                {
                    notifyData.szTip[i] = pTip[i];
                }
                notifyData.szTip[Math.Min(tip.Length, 127)] = '\0';
            }
        }

        return notifyData;
    }

    private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        var instance = _instance;
        if (instance == null)
            return User32.DefWindowProcW(hWnd, msg, (UIntPtr)wParam, lParam);
        return instance.HandleWndProc(hWnd, msg, wParam, lParam);
    }

    private IntPtr HandleWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case MsgTrayCallback:
                var eventId = (ushort)(lParam.ToInt32() & 0xFFFF);
                var iconId = (ushort)((lParam.ToInt32() >> 16) & 0xFFFF);
                if (iconId == TrayIconId)
                {
                    if (eventId == Shell32.NIN_SELECT || eventId == Shell32.WM_CONTEXTMENU)
                    {
                        ShowContextMenu();
                    }
                }
                break;

            case User32.WM_COMMAND:
                var commandId = wParam.ToInt32() & 0xFFFF;
                HandleMenuCommand(commandId);
                break;

            case User32.WM_CLOSE:
                User32.PostQuitMessage(0);
                break;
        }

        return User32.DefWindowProcW(hWnd, msg, (UIntPtr)wParam, lParam);
    }

    private void ShowContextMenu()
    {
        User32.SetForegroundWindow(_windowHandle);

        var menu = User32.CreatePopupMenu();
        if (menu == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var statusText = _currentState switch
            {
                AppState.Active => "Status: Active",
                AppState.Idle => "Status: Idle",
                AppState.ManualOverride => "Status: Override",
                _ => "Status: Unknown",
            };

            User32.AppendMenuW(
                menu,
                User32.MF_STRING | User32.MF_GRAYED,
                (nuint)CmdStatus,
                statusText
            );

            User32.AppendMenuW(menu, User32.MF_SEPARATOR, 0, string.Empty);

            User32.AppendMenuW(menu, User32.MF_STRING, (nuint)CmdSettings, "Settings...");

            User32.AppendMenuW(menu, User32.MF_SEPARATOR, 0, string.Empty);

            if (_currentState == AppState.ManualOverride)
            {
                User32.AppendMenuW(
                    menu,
                    User32.MF_STRING,
                    (nuint)CmdClearOverride,
                    "Clear Override"
                );
            }
            else
            {
                User32.AppendMenuW(
                    menu,
                    User32.MF_STRING | User32.MF_GRAYED,
                    (nuint)CmdClearOverride,
                    "Clear Override"
                );
            }

            User32.AppendMenuW(menu, User32.MF_SEPARATOR, 0, string.Empty);

            User32.AppendMenuW(menu, User32.MF_STRING, (nuint)CmdExit, "Exit");

            User32.GetCursorPos(out var pt);

            User32.TrackPopupMenu(
                menu,
                User32.TPM_LEFTALIGN | User32.TPM_BOTTOMALIGN,
                pt.x,
                pt.y,
                0,
                _windowHandle,
                IntPtr.Zero
            );
        }
        finally
        {
            User32.DestroyMenu(menu);
        }
    }

    private void HandleMenuCommand(int commandId)
    {
        switch (commandId)
        {
            case CmdSettings:
                OnOpenSettings?.Invoke();
                break;

            case CmdClearOverride:
                OnClearOverride?.Invoke();
                break;

            case CmdExit:
                OnExit?.Invoke();
                break;
        }
    }

    internal void UpdateTooltip(string text)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        var notifyData = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _windowHandle,
            uID = TrayIconId,
            uFlags = Shell32.NIF_TIP,
        };

        unsafe
        {
            fixed (char* pText = text)
            {
                for (var i = 0; i < text.Length && i < 127; i++)
                {
                    notifyData.szTip[i] = pText[i];
                }
                notifyData.szTip[Math.Min(text.Length, 127)] = '\0';
            }
        }

        Shell32.Shell_NotifyIconW(Shell32.NIM_MODIFY, ref notifyData);
    }

    internal void ShowBalloon(string title, string message)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        var notifyData = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _windowHandle,
            uID = TrayIconId,
            uFlags = Shell32.NIF_INFO,
            dwInfoFlags = Shell32.NIIF_INFO,
        };

        unsafe
        {
            fixed (char* pTitle = title)
            {
                for (var i = 0; i < title.Length && i < 63; i++)
                {
                    notifyData.szInfoTitle[i] = pTitle[i];
                }
                notifyData.szInfoTitle[Math.Min(title.Length, 63)] = '\0';
            }

            fixed (char* pMessage = message)
            {
                for (var i = 0; i < message.Length && i < 255; i++)
                {
                    notifyData.szInfo[i] = pMessage[i];
                }
                notifyData.szInfo[Math.Min(message.Length, 255)] = '\0';
            }
        }

        Shell32.Shell_NotifyIconW(Shell32.NIM_MODIFY, ref notifyData);
    }

    internal void UpdateState(AppState state)
    {
        _currentState = state;
    }

    private void Shell_NotifyIconDelete()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        var notifyData = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _windowHandle,
            uID = TrayIconId,
        };

        Shell32.Shell_NotifyIconW(Shell32.NIM_DELETE, ref notifyData);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_windowHandle != IntPtr.Zero)
        {
            User32.PostMessageW(_windowHandle, User32.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
