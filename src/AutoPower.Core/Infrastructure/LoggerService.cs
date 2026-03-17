using System.Globalization;

namespace AutoPower.Core.Infrastructure;

internal static class LoggerService
{
    private static readonly object _lock = new();
    private static readonly string _logDirectory;
    private static readonly string _logFileNameFormat;

    static LoggerService()
    {
        _logDirectory = Path.GetFullPath("logs");
        _logFileNameFormat = "'autopower-'yyyy-MM-dd'.log'";
    }

    internal static void Info(string message)
    {
        Write("INFO", message);
    }

    internal static void Warn(string message)
    {
        Write("WARN", message);
    }

    internal static void Error(string message, Exception? ex = null)
    {
        if (ex != null)
        {
            Write("ERROR", message + Environment.NewLine + ex.ToString());
        }
        else
        {
            Write("ERROR", message);
        }
    }

    private static void Write(string level, string message)
    {
        lock (_lock)
        {
            var logDirectory = _logDirectory;
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            var fileName = DateTime.Now.ToString(_logFileNameFormat, CultureInfo.InvariantCulture);
            var filePath = Path.Combine(logDirectory, fileName);

            var timestamp = DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture
            );
            var logLine = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";
#if DEBUG
            System.Diagnostics.Debug.Print(logLine);
#endif
            File.AppendAllText(filePath, logLine);
        }
    }
}
