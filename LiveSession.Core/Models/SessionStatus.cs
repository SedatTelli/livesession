namespace LiveSession.Core.Models;

public sealed class SessionStatus
{
    public bool IsProtectionEnabled { get; set; } = true;
    public bool IsPaused { get; set; }
    public DateTime? LastKeepAliveTime { get; set; }
    public KeepAliveAction LastAction { get; set; }
    public string LastActionTarget { get; set; } = "—";
    public TimeSpan CurrentIdleTime { get; set; }
    public int TodayActionsCount { get; set; }
    public List<TargetProcessStatus> Targets { get; set; } = [];
    public string StatusText => IsPaused ? "Paused" : IsProtectionEnabled ? "Protected" : "Stopped";
}
