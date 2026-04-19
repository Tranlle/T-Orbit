using TOrbit.Designer.Models;

namespace TOrbit.Designer.Services;

public interface ILocalizationService
{
    event EventHandler? LanguageChanged;

    string CurrentLanguageCode { get; }

    IReadOnlyList<DesignerOptionItem> GetSupportedLanguages();

    string GetString(string key);

    bool SetLanguage(string languageCode);
}
