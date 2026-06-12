using LiveSession.Core.Interfaces;
using Microsoft.Win32;

namespace LiveSession.Infrastructure.Configuration;

public sealed class StartupRegistrar : IStartupRegistrar
{
    private const string AppName = "LiveSession";
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.SetValue(AppName, $"\"{Environment.ProcessPath}\"");
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(AppName) is not null;
    }
}
