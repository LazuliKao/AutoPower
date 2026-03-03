using System.Diagnostics;

namespace AutoPower.Infrastructure;

internal static class StartupManager
{
    private const string TaskName = "AutoPower";
    private const int ProcessTimeoutMs = 5000;

    internal static bool IsRegistered()
    {
        var exitCode = RunSchtasks($"/Query /TN \"{TaskName}\"");
        return exitCode == 0;
    }

    internal static bool Register()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            return false;
        }

        var exitCode = RunSchtasks(
            $"/Create /TN \"{TaskName}\" /TR \"\\\"{exePath}\\\"\" /SC ONLOGON /RL HIGHEST /F"
        );
        return exitCode == 0;
    }

    internal static bool Unregister()
    {
        var exitCode = RunSchtasks($"/Delete /TN \"{TaskName}\" /F");
        return exitCode == 0;
    }

    private static int RunSchtasks(string arguments)
    {
        using var process = new Process();
        process.StartInfo = new()
        {
            FileName = "schtasks.exe",
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        process.Start();
        var completed = process.WaitForExit(ProcessTimeoutMs);

        if (!completed)
        {
            process.Kill();
            return -1;
        }

        return process.ExitCode;
    }
}
