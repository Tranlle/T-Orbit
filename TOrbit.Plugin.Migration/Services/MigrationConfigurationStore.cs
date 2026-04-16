using System.Text.Json;
using TOrbit.Plugin.Migration.Models;

namespace TOrbit.Plugin.Migration.Services;

public sealed class MigrationConfigurationStore
{
    private const string ConfigFileName = ".torbit-tools.json";
    private const string LegacyConfigFileName = ".tranbok-tools.json";
    private const string SettingsDirectoryName = "Migration";
    private const string SettingsFileName = "setting.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _pluginRoot;
    private readonly string _legacyPluginRoot;

    public MigrationConfigurationStore(string? pluginRoot = null, string? legacyPluginRoot = null)
    {
        _pluginRoot = pluginRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "T-Orbit",
            "plugins",
            "migration");
        _legacyPluginRoot = legacyPluginRoot ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Plugins", "Migration"));
    }

    public MigrationToolConfig LoadConfig(string? projectPath = null)
    {
        foreach (var candidate in EnumerateConfigCandidates(projectPath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(candidate))
                continue;

            try
            {
                var json = File.ReadAllText(candidate);
                var config = JsonSerializer.Deserialize<MigrationToolConfig>(json, JsonOptions);
                if (config is not null)
                {
                    if (string.IsNullOrWhiteSpace(config.ProjectPath) && !string.IsNullOrWhiteSpace(projectPath))
                        config.ProjectPath = projectPath;

                    return config;
                }
            }
            catch
            {
            }
        }

        return new MigrationToolConfig { ProjectPath = projectPath ?? string.Empty };
    }

    public void SaveConfig(MigrationToolConfig config)
    {
        Directory.CreateDirectory(_pluginRoot);
        File.WriteAllText(GetConfigFilePath(), JsonSerializer.Serialize(config, JsonOptions));
    }

    public MigrationSettings LoadSettings(string? projectPath = null)
    {
        foreach (var candidate in EnumerateSettingsCandidates(projectPath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(candidate))
                continue;

            try
            {
                var json = File.ReadAllText(candidate);
                var settings = JsonSerializer.Deserialize<MigrationSettings>(json, JsonOptions);
                if (settings is not null)
                {
                    if (string.IsNullOrWhiteSpace(settings.ProjectPath) && !string.IsNullOrWhiteSpace(projectPath))
                        settings.ProjectPath = projectPath;

                    return settings;
                }
            }
            catch
            {
            }
        }

        return new MigrationSettings { ProjectPath = projectPath ?? string.Empty };
    }

    public void SaveSettings(MigrationSettings settings)
    {
        Directory.CreateDirectory(_pluginRoot);
        File.WriteAllText(GetSettingsFilePath(), JsonSerializer.Serialize(settings, JsonOptions));
    }

    private IEnumerable<string> EnumerateConfigCandidates(string? projectPath)
    {
        yield return GetConfigFilePath();
        yield return Path.Combine(_legacyPluginRoot, LegacyConfigFileName);

        if (!string.IsNullOrWhiteSpace(projectPath))
        {
            var projectDir = GetProjectDirectory(projectPath);
            yield return Path.Combine(projectDir, ConfigFileName);
            yield return Path.Combine(projectDir, LegacyConfigFileName);
        }
    }

    private IEnumerable<string> EnumerateSettingsCandidates(string? projectPath)
    {
        yield return GetSettingsFilePath();
        yield return Path.Combine(_legacyPluginRoot, SettingsFileName);

        if (!string.IsNullOrWhiteSpace(projectPath))
            yield return Path.Combine(GetProjectDirectory(projectPath), SettingsDirectoryName, SettingsFileName);
    }

    private static string GetProjectDirectory(string projectPath)
        => File.Exists(projectPath)
            ? Path.GetDirectoryName(projectPath) ?? projectPath
            : projectPath;

    private string GetConfigFilePath() => Path.Combine(_pluginRoot, ConfigFileName);

    private string GetSettingsFilePath() => Path.Combine(_pluginRoot, SettingsFileName);
}
