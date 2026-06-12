using LiveSession.Core.Models;

namespace LiveSession.Core.Interfaces;

public interface IProcessScanner
{
    List<RunningProcessInfo> GetWindowedProcesses();
}
