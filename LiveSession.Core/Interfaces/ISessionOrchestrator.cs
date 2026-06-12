using LiveSession.Core.Models;

namespace LiveSession.Core.Interfaces;

public interface ISessionOrchestrator
{
    SessionStatus Status { get; }
    void Pause();
    void Resume();
    void UpdateTargets(List<TargetProcess> targets);
    event EventHandler<KeepAliveResult>? KeepAliveSent;
    event EventHandler<SessionStatus>? StatusChanged;
}
