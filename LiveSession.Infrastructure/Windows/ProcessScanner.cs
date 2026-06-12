using LiveSession.Core.Interfaces;
using LiveSession.Core.Models;
using System.Diagnostics;
using System.Text;

namespace LiveSession.Infrastructure.Windows;

public sealed class ProcessScanner : IProcessScanner
{
    private static readonly HashSet<string> _exclude = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer", "taskmgr", "SearchHost", "StartMenuExperienceHost",
        "ShellExperienceHost", "SystemSettings", "ApplicationFrameHost",
        "TextInputHost", "dwm", "winlogon", "csrss", "services", "svchost",
        "RuntimeBroker", "dllhost", "conhost", "WUDFHost", "MsMpEng",
        "LiveSession"
    };

    public List<RunningProcessInfo> GetWindowedProcesses()
    {
        var seen   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<RunningProcessInfo>();
        var sb     = new StringBuilder(512);

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;

            NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString();
            if (string.IsNullOrWhiteSpace(title)) return true;

            NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);

            try
            {
                using var proc = Process.GetProcessById((int)pid);
                var name = proc.ProcessName;

                if (_exclude.Contains(name)) return true;

                if (seen.Add(name))
                {
                    result.Add(new RunningProcessInfo
                    {
                        ProcessName = name,
                        WindowTitle = title,
                        Pid         = (int)pid
                    });
                }
            }
            catch { }

            return true;
        }, IntPtr.Zero);

        return [.. result.OrderBy(p => p.ProcessName)];
    }
}
