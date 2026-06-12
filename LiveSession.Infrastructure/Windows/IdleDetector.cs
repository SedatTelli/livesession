using LiveSession.Core.Interfaces;

namespace LiveSession.Infrastructure.Windows;

public sealed class IdleDetector : IIdleDetector
{
    public TimeSpan GetIdleTime()
    {
        var info = new NativeMethods.LASTINPUTINFO { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.LASTINPUTINFO>() };

        if (!NativeMethods.GetLastInputInfo(ref info))
            return TimeSpan.Zero;

        var idleMs = (uint)Environment.TickCount - info.dwTime;
        return TimeSpan.FromMilliseconds(idleMs);
    }
}
