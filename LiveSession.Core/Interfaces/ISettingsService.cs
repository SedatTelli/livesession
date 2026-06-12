using LiveSession.Core.Models;

namespace LiveSession.Core.Interfaces;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}
