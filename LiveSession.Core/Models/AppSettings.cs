namespace LiveSession.Core.Models;

public sealed class AppSettings
{
    public int CheckIntervalSeconds { get; set; } = 30;
    public bool StartWithWindows { get; set; } = true;
    public bool EnableMouseMove { get; set; } = false;
    public bool EnableKeyboardEvents { get; set; } = true;
    public bool EnableScrollEvents { get; set; } = true;

    public List<TargetProcess> TargetProcesses { get; set; } =
    [
        new() { ProcessName = "Island",   DisplayName = "Island Browser",    IntervalMinutes = 4, Enabled = true },
        new() { ProcessName = "msedge",   DisplayName = "Microsoft Edge",    IntervalMinutes = 4, Enabled = true },
        new() { ProcessName = "chrome",   DisplayName = "Google Chrome",     IntervalMinutes = 4, Enabled = true }
    ];
}
