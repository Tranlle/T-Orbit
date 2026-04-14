using System.Text.Json;
using Tranbok.Tools.Core.Models;

namespace Tranbok.Tools.Core.Services;

public sealed class AppPreferencesService : IAppPreferencesService
{
    private const string PreferencesFileName = "app-preferences.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static string PreferencesFilePath => Path.Combine(AppContext.BaseDirectory, PreferencesFileName);

    public AppPreferences Load()
    {
        if (!File.Exists(PreferencesFilePath))
            return new AppPreferences();

        try
        {
            var json = File.ReadAllText(PreferencesFilePath);
            return JsonSerializer.Deserialize<AppPreferences>(json, JsonOptions) ?? new AppPreferences();
        }
        catch
        {
            return new AppPreferences();
        }
    }

    public void Save(AppPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        File.WriteAllText(PreferencesFilePath, JsonSerializer.Serialize(preferences, JsonOptions));
    }
}
