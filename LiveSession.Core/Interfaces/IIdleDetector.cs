namespace LiveSession.Core.Interfaces;

public interface IIdleDetector
{
    TimeSpan GetIdleTime();
}
