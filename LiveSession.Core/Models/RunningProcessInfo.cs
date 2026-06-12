namespace LiveSession.Core.Models;

public sealed class RunningProcessInfo
{
    public string ProcessName { get; set; } = "";
    public string WindowTitle { get; set; } = "";
    public int Pid { get; set; }
}
