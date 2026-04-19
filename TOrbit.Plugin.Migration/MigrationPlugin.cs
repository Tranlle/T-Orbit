using Avalonia.Controls;
using TOrbit.Designer.Abstractions;
using TOrbit.Designer.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Models;
using TOrbit.Plugin.Migration.Models;
using TOrbit.Plugin.Migration.Services;
using TOrbit.Plugin.Migration.ViewModels;
using TOrbit.Plugin.Migration.Views;

namespace TOrbit.Plugin.Migration;

public sealed class MigrationPlugin : BasePlugin, IVisualPlugin, IPluginVariableReceiver, IPluginPageHeaderProvider, IPluginDisplayInfoProvider
{
    private MigrationView? _view;
    private MigrationViewModel? _viewModel;
    private readonly MigrationService _service = new();
    private readonly MigrationConfigurationStore _configurationStore = new();
    private readonly ILocalizationService _localizationService;
    private MigrationVariables _variables = new();
    private readonly PluginDescriptor _descriptor;

    public MigrationPlugin(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        _descriptor = CreateDescriptor<MigrationPlugin>(
            MigrationPluginMetadata.Instance.Id,
            _localizationService.GetString("plugins.migration.name"),
            MigrationPluginMetadata.Instance.Version,
            _localizationService.GetString("plugins.migration.description"),
            MigrationPluginMetadata.Instance.Author,
            MigrationPluginMetadata.Instance.Icon,
            MigrationPluginMetadata.Instance.Tags,
            variableDefinitions:
            [
                new PluginVariableDefinition(
                    Key: "TORBIT_DB_CONNECTION",
                    DefaultValue: string.Empty,
                    DisplayName: _localizationService.GetString("plugins.migration.variables.connection.name"),
                    Description: _localizationService.GetString("plugins.migration.variables.connection.description"),
                    IsRequired: true)
            ],
            capabilities: MigrationPluginMetadata.Instance.Capabilities);
    }

    public override PluginDescriptor Descriptor => _descriptor;

    public event EventHandler? HeaderChanged;
    public event EventHandler? DisplayInfoChanged;

    public string DisplayName => _localizationService.GetString("plugins.migration.name");

    public string DisplayDescription => _localizationService.GetString("plugins.migration.description");

    protected override ValueTask OnStartAsync(CancellationToken cancellationToken = default)
    {
        _localizationService.LanguageChanged += LocalizationServiceOnLanguageChanged;
        EnsureView();
        return ValueTask.CompletedTask;
    }

    public override Control GetMainView()
    {
        EnsureView();
        return _view!;
    }

    public PluginPageHeaderModel? GetPageHeader()
    {
        EnsureView();
        if (_viewModel is null)
            return null;

        var context = _viewModel.StatusMessage;
        if (string.IsNullOrWhiteSpace(context) || string.Equals(context, _localizationService.GetString("migration.messages.ready"), StringComparison.OrdinalIgnoreCase))
            context = _viewModel.HasSelectedProfile ? string.Empty : _localizationService.GetString("migration.selectProfileFirst");

        return new PluginPageHeaderModel
        {
            Context = context
        };
    }

    public void OnVariablesInjected(IReadOnlyDictionary<string, string> rawValues)
    {
        _variables = PluginVariableBinder.Bind<MigrationVariables>(rawValues);
        _viewModel?.UpdateVariables(_variables);
    }

    private void EnsureView()
    {
        if (_viewModel is null)
        {
            var dialogService = Context.GetTool<IDesignerDialogService>();
            _viewModel = new MigrationViewModel(_service, _configurationStore, _variables, _localizationService, dialogService);
            _viewModel.HeaderSummaryChanged += ViewModelOnHeaderSummaryChanged;
        }

        _view ??= new MigrationView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        _localizationService.LanguageChanged -= LocalizationServiceOnLanguageChanged;
        if (_viewModel is not null)
        {
            _viewModel.HeaderSummaryChanged -= ViewModelOnHeaderSummaryChanged;
            _viewModel.Dispose();
        }

        _view = null;
        _viewModel = null;
        return ValueTask.CompletedTask;
    }

    private void ViewModelOnHeaderSummaryChanged(object? sender, EventArgs e)
        => HeaderChanged?.Invoke(this, EventArgs.Empty);

    private void LocalizationServiceOnLanguageChanged(object? sender, EventArgs e)
    {
        DisplayInfoChanged?.Invoke(this, EventArgs.Empty);
        HeaderChanged?.Invoke(this, EventArgs.Empty);
    }
}
