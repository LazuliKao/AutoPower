using System.Globalization;

namespace AutoPower.Tests.Infrastructure;

public class LoggerServiceTests : IDisposable
{
    private readonly string _testLogDirectory = Path.Combine(
        Path.GetTempPath(),
        $"AutoPowerLogTest_{Guid.NewGuid():N}"
    );

    [Fact]
    public void Info_WritesToDailyLogFile()
    {
        var testMessage = $"TestInfoMessage_{Guid.NewGuid():N}";
        Directory.CreateDirectory(_testLogDirectory);

        WriteLog("INFO", testMessage);

        var logFilePath = GetLogFilePath(_testLogDirectory);
        Assert.True(File.Exists(logFilePath), "Log file should exist after Info call");

        var content = File.ReadAllText(logFilePath);
        Assert.Contains("[INFO]", content);
        Assert.Contains(testMessage, content);
    }

    [Fact]
    public void Error_WritesToLogFile_WithErrorTag()
    {
        var testMessage = $"TestErrorMessage_{Guid.NewGuid():N}";
        Directory.CreateDirectory(_testLogDirectory);

        WriteLog("ERROR", testMessage);

        var logFilePath = GetLogFilePath(_testLogDirectory);
        Assert.True(File.Exists(logFilePath), "Log file should exist after Error call");

        var content = File.ReadAllText(logFilePath);
        Assert.Contains("[ERROR]", content);
        Assert.Contains(testMessage, content);
    }

    [Fact]
    public void Warn_WritesToLogFile_WithWarnTag()
    {
        var testMessage = $"TestWarnMessage_{Guid.NewGuid():N}";
        Directory.CreateDirectory(_testLogDirectory);

        WriteLog("WARN", testMessage);

        var logFilePath = GetLogFilePath(_testLogDirectory);
        Assert.True(File.Exists(logFilePath), "Log file should exist after Warn call");

        var content = File.ReadAllText(logFilePath);
        Assert.Contains("[WARN]", content);
        Assert.Contains(testMessage, content);
    }

    [Fact]
    public void LogEntry_ContainsTimestamp()
    {
        var testMessage = $"TestTimestamp_{Guid.NewGuid():N}";
        Directory.CreateDirectory(_testLogDirectory);

        WriteLog("INFO", testMessage);

        var logFilePath = GetLogFilePath(_testLogDirectory);
        var content = File.ReadAllText(logFilePath);

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.NotEmpty(lines);

        var lastLine = lines[lines.Length - 1];
        Assert.Matches(@"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\]", lastLine);
    }

    private void WriteLog(string level, string message)
    {
        var logDirectory = _testLogDirectory;
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        var fileName = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var filePath = Path.Combine(logDirectory, $"autopower-{fileName}.log");

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var logLine = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";

        File.AppendAllText(filePath, logLine);
    }

    private static string GetLogFilePath(string logDirectory)
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return Path.Combine(logDirectory, $"autopower-{today}.log");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testLogDirectory))
        {
            try
            {
                Directory.Delete(_testLogDirectory, true);
            }
            catch { }
        }
    }
}
