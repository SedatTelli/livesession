namespace LiveSession.Core.Interfaces;

public interface IFullscreenDetector
{
    bool IsFullscreenActive();
    string GetSkipReason();
}
