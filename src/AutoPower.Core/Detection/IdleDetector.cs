using System.Runtime.InteropServices;
using AutoPower.Core.Infrastructure.Win32;

namespace AutoPower.Core.Detection;

internal sealed class IdleDetector : IDisposable
{
    private readonly Timer _timer;
    private readonly ulong _idleTimeoutMs;
    private bool _isIdle;
    private bool _disposed;

    public event Action<bool>? IdleStateChanged;

    public IdleDetector(int idleTimeoutSeconds)
    {
        _idleTimeoutMs = (ulong)idleTimeoutSeconds * 1000;
        _timer = new(OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    private void OnTimerTick(object? state)
    {
        if (_disposed)
            return;

        var lastInputInfo = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };

        if (!User32.GetLastInputInfo(ref lastInputInfo))
            return;
        var idleTime = Kernel32.GetTickCount64() - lastInputInfo.dwTime;

        var isCurrentlyIdle = idleTime >= _idleTimeoutMs;

        if (isCurrentlyIdle != _isIdle)
        {
            _isIdle = isCurrentlyIdle;
            IdleStateChanged?.Invoke(_isIdle);
        }
    }

    internal void Start()
    {
        _timer.Change(0, 1000);
    }

    internal void Stop()
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Stop();
        _timer.Dispose();
    }
}
