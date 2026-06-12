using LiveSession.Core.Models;

namespace LiveSession.Core.Interfaces;

public interface IKeepAliveEngine
{
    KeepAliveResult Execute(string processName);
}
