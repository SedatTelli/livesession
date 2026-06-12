namespace LiveSession.Core.Models;

public sealed class TargetProcess
{
    public string ProcessName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int IntervalMinutes { get; set; } = 4;
    public bool Enabled { get; set; } = true;
}
