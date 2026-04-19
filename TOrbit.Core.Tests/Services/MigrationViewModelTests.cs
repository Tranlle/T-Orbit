using TOrbit.Plugin.Migration.Models;
using TOrbit.Plugin.Migration.Services;
using TOrbit.Plugin.Migration.ViewModels;
using TOrbit.Designer.Services;
using Xunit;

namespace TOrbit.Core.Tests.Services;

public sealed class MigrationViewModelTests
{
    [Fact]
    public void Constructor_UsesInjectedDefaultConnectionStringForInitialProfile()
    {
        var viewModel = CreateViewModel("Server=.;Database=Initial;");

        var profile = Assert.Single(viewModel.DbProfiles);
        Assert.Equal("Server=.;Database=Initial;", profile.ConnectionString);
    }

    [Fact]
    public void UpdateVariables_FillsEmptyProfileConnectionsWithoutOverwritingExistingValues()
    {
        var viewModel = CreateViewModel(string.Empty);
        var firstProfile = Assert.Single(viewModel.DbProfiles);
        firstProfile.ConnectionString = string.Empty;

        viewModel.AddProfileCommand.Execute(null);
        Assert.Equal(2, viewModel.DbProfiles.Count);
        var secondProfile = viewModel.DbProfiles[1];
        secondProfile.ConnectionString = "Existing";

        viewModel.UpdateVariables(new MigrationVariables
        {
            DefaultConnectionString = "Server=.;Database=Injected;"
        });

        Assert.Equal("Server=.;Database=Injected;", firstProfile.ConnectionString);
        Assert.Equal("Existing", secondProfile.ConnectionString);
    }

    private static MigrationViewModel CreateViewModel(string defaultConnectionString)
    {
        var pluginRoot = CreateTempDirectory();
        var legacyRoot = CreateTempDirectory();

        return new MigrationViewModel(
            new MigrationService(),
            new MigrationConfigurationStore(pluginRoot, legacyRoot),
            new MigrationVariables { DefaultConnectionString = defaultConnectionString },
            new LocalizationService());
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "TOrbit.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
