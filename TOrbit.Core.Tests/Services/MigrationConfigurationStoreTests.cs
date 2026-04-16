using TOrbit.Plugin.Migration.Models;
using TOrbit.Plugin.Migration.Services;
using Xunit;

namespace TOrbit.Core.Tests.Services;

public sealed class MigrationConfigurationStoreTests
{
    [Fact]
    public void LoadConfig_FallsBackToLegacyConfigFile()
    {
        var pluginRoot = CreateTempDirectory();
        var legacyRoot = CreateTempDirectory();

        try
        {
            var store = new MigrationConfigurationStore(pluginRoot, legacyRoot);
            File.WriteAllText(Path.Combine(legacyRoot, ".tranbok-tools.json"), """
            {
              "projectPath": "Legacy.Domain.csproj",
              "activeProfileId": "legacy-profile"
            }
            """);

            var config = store.LoadConfig();

            Assert.Equal("Legacy.Domain.csproj", config.ProjectPath);
            Assert.Equal("legacy-profile", config.ActiveProfileId);
        }
        finally
        {
            Directory.Delete(pluginRoot, true);
            Directory.Delete(legacyRoot, true);
        }
    }

    [Fact]
    public void SaveSettings_WritesCurrentSettingsFile()
    {
        var pluginRoot = CreateTempDirectory();
        var legacyRoot = CreateTempDirectory();

        try
        {
            var store = new MigrationConfigurationStore(pluginRoot, legacyRoot);
            store.SaveSettings(new MigrationSettings
            {
                ProjectPath = "Domain.csproj",
                ActiveProfileId = "profile-1"
            });

            var savedPath = Path.Combine(pluginRoot, "setting.json");
            Assert.True(File.Exists(savedPath));

            var json = File.ReadAllText(savedPath);
            Assert.Contains("Domain.csproj", json, StringComparison.Ordinal);
            Assert.Contains("profile-1", json, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(pluginRoot, true);
            Directory.Delete(legacyRoot, true);
        }
    }

    [Fact]
    public void LoadSettings_PrefersCurrentSettingsOverLegacyFile()
    {
        var pluginRoot = CreateTempDirectory();
        var legacyRoot = CreateTempDirectory();

        try
        {
            Directory.CreateDirectory(pluginRoot);
            Directory.CreateDirectory(legacyRoot);

            File.WriteAllText(Path.Combine(pluginRoot, "setting.json"), """
            {
              "projectPath": "Current.Domain.csproj",
              "activeProfileId": "current-profile"
            }
            """);
            File.WriteAllText(Path.Combine(legacyRoot, "setting.json"), """
            {
              "projectPath": "Legacy.Domain.csproj",
              "activeProfileId": "legacy-profile"
            }
            """);

            var store = new MigrationConfigurationStore(pluginRoot, legacyRoot);
            var settings = store.LoadSettings();

            Assert.Equal("Current.Domain.csproj", settings.ProjectPath);
            Assert.Equal("current-profile", settings.ActiveProfileId);
        }
        finally
        {
            Directory.Delete(pluginRoot, true);
            Directory.Delete(legacyRoot, true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "TOrbit.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
