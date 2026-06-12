namespace LiveSession.Core.Interfaces;

public interface IStartupRegistrar
{
    void Enable();
    void Disable();
    bool IsEnabled();
}
