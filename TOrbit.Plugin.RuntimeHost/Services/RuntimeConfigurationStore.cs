using System.Text.Json;
using TOrbit.Plugin.RuntimeHost.Models;

namespace TOrbit.Plugin.RuntimeHost.Services;

public sealed class RuntimeConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _pluginRoot;

    public RuntimeConfigurationStore(string? pluginRoot = null)
    {
        _pluginRoot = pluginRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "T-Orbit",
            "plugins",
            "runtime-host");
    }

    public RuntimeHostSettings Load()
    {
        var path = GetSettingsFilePath();
        if (!File.Exists(path))
            return new RuntimeHostSettings();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<RuntimeHostSettings>(json, JsonOptions) ?? new RuntimeHostSettings();
        }
        catch
        {
            return new RuntimeHostSettings();
        }
    }

    public void Save(RuntimeHostSettings settings)
    {
        Directory.CreateDirectory(_pluginRoot);
        File.WriteAllText(GetSettingsFilePath(), JsonSerializer.Serialize(settings, JsonOptions));
    }

    public string GetAppRoot(string profileId)
        => Path.Combine(_pluginRoot, "apps", profileId);

    public string GetCurrentDeploymentDirectory(string profileId)
        => Path.Combine(GetAppRoot(profileId), "current");

    public string GetPackageDirectory(string profileId)
        => Path.Combine(GetAppRoot(profileId), "package");

    public string GetLogsDirectory(string profileId)
        => Path.Combine(GetAppRoot(profileId), "logs");

    public string GetDeploymentManifestPath(string profileId)
        => Path.Combine(GetAppRoot(profileId), "deployment.json");

    public string GetCurrentLogPath(string profileId)
        => Path.Combine(GetLogsDirectory(profileId), $"{DateTime.Now:yyyyMMdd}.log");

    public RuntimeDeploymentManifest? LoadDeploymentManifest(string profileId)
    {
        var path = GetDeploymentManifestPath(profileId);
        if (!File.Exists(path))
            return null;

        try
        {
            return JsonSerializer.Deserialize<RuntimeDeploymentManifest>(File.ReadAllText(path), JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void SaveDeploymentManifest(string profileId, RuntimeDeploymentManifest manifest)
    {
        Directory.CreateDirectory(GetAppRoot(profileId));
        File.WriteAllText(GetDeploymentManifestPath(profileId), JsonSerializer.Serialize(manifest, JsonOptions));
    }

    public void AppendLog(string profileId, HostedAppLogEntry entry)
    {
        var logPath = GetCurrentLogPath(profileId);
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        var line = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] {entry.Message}";
        File.AppendAllText(logPath, line + Environment.NewLine);
    }

    public string LoadRecentLogs(string profileId, int maxLines = 300)
    {
        var logsDirectory = GetLogsDirectory(profileId);
        if (!Directory.Exists(logsDirectory))
            return string.Empty;

        var files = Directory.GetFiles(logsDirectory, "*.log")
            .OrderByDescending(static path => path, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Reverse()
            .ToList();

        if (files.Count == 0)
            return string.Empty;

        var lines = new Queue<string>();
        foreach (var file in files)
        {
            foreach (var line in File.ReadLines(file))
            {
                lines.Enqueue(line);
                while (lines.Count > maxLines)
                    lines.Dequeue();
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private string GetSettingsFilePath() => Path.Combine(_pluginRoot, "profiles.json");
}
