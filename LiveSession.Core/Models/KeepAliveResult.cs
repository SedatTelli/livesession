namespace LiveSession.Core.Models;

public sealed record KeepAliveResult(
    KeepAliveAction Action,
    TimeSpan IdleTime,
    string? SkipReason = null)
{
    public bool WasSent => Action != KeepAliveAction.None && Action != KeepAliveAction.Skipped;
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
