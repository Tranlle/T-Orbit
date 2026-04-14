using Tranbok.Tools.Core.Models;

namespace Tranbok.Tools.Core.Services;

public interface IAppPreferencesService
{
    AppPreferences Load();
    void Save(AppPreferences preferences);
}
