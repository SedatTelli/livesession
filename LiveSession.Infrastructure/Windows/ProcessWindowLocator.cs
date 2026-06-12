using System.Diagnostics;
using System.Text;

namespace LiveSession.Infrastructure.Windows;

internal static class ProcessWindowLocator
{
    private const string ChromiumRendererClass = "Chrome_RenderWidgetHostHWND";
    private const string ChromiumTopLevelClass  = "Chrome_WidgetWin_1";

    internal static List<IntPtr> FindWindows(string processName)
    {
        var pids = GetPids(processName);
        if (pids.Count == 0) return [];

        var topLevel = FindTopLevelWindows(pids);
        var result   = new List<IntPtr>();

        foreach (var hwnd in topLevel)
        {
            // Chromium tabanlı tarayıcılar için renderer penceresini bul
            var renderer = FindChildByClass(hwnd, ChromiumRendererClass);
            result.Add(renderer != IntPtr.Zero ? renderer : hwnd);
        }

        return result;
    }

    // Sadece görünür penceresi olan process'leri "çalışıyor" sayar
    internal static bool IsProcessRunning(string processName)
        => FindWindows(processName).Count > 0;

    private static HashSet<uint> GetPids(string processName)
    {
        var pids = new HashSet<uint>();
        try
        {
            foreach (var p in Process.GetProcessesByName(processName))
                pids.Add((uint)p.Id);
        }
        catch { }
        return pids;
    }

    private static List<IntPtr> FindTopLevelWindows(HashSet<uint> targetPids)
    {
        var result = new List<IntPtr>();
        var sb     = new StringBuilder(256);

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;
            NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
            if (!targetPids.Contains(pid)) return true;

            NativeMethods.GetClassName(hWnd, sb, sb.Capacity);
            var cls = sb.ToString();

            // Chromium top-level veya herhangi görünür pencere
            if (cls == ChromiumTopLevelClass || HasWindowTitle(hWnd))
                result.Add(hWnd);

            return true;
        }, IntPtr.Zero);

        return result;
    }

    private static bool HasWindowTitle(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
        return sb.Length > 0;
    }

    private static IntPtr FindChildByClass(IntPtr parent, string targetClass)
    {
        IntPtr found = IntPtr.Zero;
        var sb = new StringBuilder(256);

        NativeMethods.EnumChildWindows(parent, (hWnd, _) =>
        {
            NativeMethods.GetClassName(hWnd, sb, sb.Capacity);
            if (sb.ToString() != targetClass) return true;
            found = hWnd;
            return false;
        }, IntPtr.Zero);

        return found;
    }
}
