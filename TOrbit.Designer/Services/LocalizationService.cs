using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using TOrbit.Designer.Models;

namespace TOrbit.Designer.Services;

public sealed class LocalizationService : ObservableObject, ILocalizationService
{
    private const string DefaultLanguageCode = "zh-CN";
    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _resources = new(StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyList<DesignerOptionItem> _supportedLanguages;
    private string _currentLanguageCode = DefaultLanguageCode;

    public static LocalizationService? Current { get; private set; }

    public static LocalizationService Shared => Current ??= new LocalizationService();

    public event EventHandler? LanguageChanged;

    public LocalizationService()
    {
        Current ??= this;
        _supportedLanguages =
        [
            new DesignerOptionItem
            {
                Key = "zh-CN",
                Label = "简体中文",
                Description = "Chinese (Simplified)"
            },
            new DesignerOptionItem
            {
                Key = "en-US",
                Label = "English",
                Description = "English (United States)"
            }
        ];
    }

    public string CurrentLanguageCode => _currentLanguageCode;

    public string this[string key] => GetString(key);

    public IReadOnlyList<DesignerOptionItem> GetSupportedLanguages() => _supportedLanguages;

    public bool SetLanguage(string languageCode)
    {
        var normalized = NormalizeLanguageCode(languageCode);
        if (string.Equals(_currentLanguageCode, normalized, StringComparison.OrdinalIgnoreCase))
            return false;

        _currentLanguageCode = normalized;
        OnPropertyChanged(nameof(CurrentLanguageCode));
        OnPropertyChanged("Item");
        OnPropertyChanged("Item[]");
        OnPropertyChanged(string.Empty);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public string GetString(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        var current = LoadResource(_currentLanguageCode);
        if (current.TryGetValue(key, out var localized) && !string.IsNullOrWhiteSpace(localized))
            return localized;

        var fallback = LoadResource(DefaultLanguageCode);
        if (fallback.TryGetValue(key, out localized) && !string.IsNullOrWhiteSpace(localized))
            return localized;

        return key;
    }

    private IReadOnlyDictionary<string, string> LoadResource(string languageCode)
    {
        if (_resources.TryGetValue(languageCode, out var cached))
            return cached;

        var assembly = typeof(LocalizationService).Assembly;
        var resourceName = $"{typeof(LocalizationService).Namespace!.Replace(".Services", ".Localization")}.{languageCode}.json";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return _resources[languageCode] = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

        using var document = JsonDocument.Parse(stream);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        FlattenJson(result, string.Empty, document.RootElement);
        return _resources[languageCode] = new ReadOnlyDictionary<string, string>(result);
    }

    private static void FlattenJson(IDictionary<string, string> target, string prefix, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var nextPrefix = string.IsNullOrWhiteSpace(prefix) ? property.Name : $"{prefix}.{property.Name}";
                FlattenJson(target, nextPrefix, property.Value);
            }

            return;
        }

        target[prefix] = element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? string.Empty
            : element.ToString();
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return DefaultLanguageCode;

        return languageCode.Trim() switch
        {
            "en" => "en-US",
            "zh" => "zh-CN",
            var value => value
        };
    }
}
