namespace LiveSession.Core.Models;

public sealed class TargetProcessStatus
{
    public string ProcessName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsRunning { get; set; }
    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; }
    public DateTime? LastKeepAlive { get; set; }
}
