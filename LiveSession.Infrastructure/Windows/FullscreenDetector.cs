using LiveSession.Core.Interfaces;

namespace LiveSession.Infrastructure.Windows;

public sealed class FullscreenDetector : IFullscreenDetector
{
    private string _lastReason = string.Empty;

    public bool IsFullscreenActive()
    {
        if (IsRemoteDesktopSession())
        {
            _lastReason = "Remote Desktop session";
            return true;
        }

        if (IsPresentationMode())
        {
            _lastReason = "Presentation mode";
            return true;
        }

        if (IsForegroundWindowFullscreen())
        {
            _lastReason = "Fullscreen application";
            return true;
        }

        _lastReason = string.Empty;
        return false;
    }

    public string GetSkipReason() => _lastReason;

    private static bool IsRemoteDesktopSession()
        => NativeMethods.GetSystemMetrics(NativeMethods.SM_REMOTESESSION) != 0;

    private static bool IsPresentationMode()
    {
        NativeMethods.SHQueryUserNotificationState(out var state);
        return state is NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_PRESENTATION_MODE
                     or NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_RUNNING_D3D_FULL_SCREEN
                     or NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_BUSY;
    }

    private static bool IsForegroundWindowFullscreen()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return false;

        if (!NativeMethods.GetWindowRect(hwnd, out var rect)) return false;

        var screenWidth = NativeMethods.GetSystemMetrics(0);   // SM_CXSCREEN
        var screenHeight = NativeMethods.GetSystemMetrics(1);  // SM_CYSCREEN

        return rect.Left <= 0
            && rect.Top <= 0
            && rect.Right >= screenWidth
            && rect.Bottom >= screenHeight;
    }
}
