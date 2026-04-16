using Avalonia.Controls;
using TOrbit.Core.Services;
using TOrbit.Designer.Abstractions;
using TOrbit.Designer.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Settings.ViewModels;
using TOrbit.Plugin.Settings.Views;

namespace TOrbit.Plugin.Settings;

public sealed class SettingsPlugin : BasePlugin, IVisualPlugin, IPluginHeaderActionsProvider
{
    private readonly IAppShellService _shellService;
    private readonly IThemeService _themeService;
    private readonly IAppPreferencesService _preferencesService;
    private readonly IPluginCatalogService _pluginCatalog;
    private readonly IPluginVariableService _variableService;

    private SettingsView? _view;
    private SettingsViewModel? _viewModel;
    private IReadOnlyList<PluginHeaderAction>? _headerActions;

    public SettingsPlugin(
        IAppShellService shellService,
        IThemeService themeService,
        IAppPreferencesService preferencesService,
        IPluginCatalogService pluginCatalog,
        IPluginVariableService variableService)
    {
        _shellService      = shellService;
        _themeService      = themeService;
        _preferencesService = preferencesService;
        _pluginCatalog     = pluginCatalog;
        _variableService   = variableService;
    }

    public override PluginDescriptor Descriptor { get; } = CreateDescriptor<SettingsPlugin>(SettingsPluginMetadata.Instance);

    public override Control GetMainView()
    {
        EnsureView();
        return _view!;
    }

    public IReadOnlyList<PluginHeaderAction> GetHeaderActions()
    {
        EnsureView();
        return _headerActions ?? [];
    }

    private void EnsureView()
    {
        if (_viewModel is null)
        {
            _viewModel = new SettingsViewModel(
                _shellService, _themeService, _preferencesService, _pluginCatalog, _variableService);
            _headerActions =
            [
                new PluginHeaderAction("重置设置", _viewModel.ResetCommand),
                new PluginHeaderAction("保存设置", _viewModel.SaveCommand, IsPrimary: true)
            ];
        }

        _view ??= new SettingsView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        _view = null;
        _viewModel = null;
        _headerActions = null;
        return ValueTask.CompletedTask;
    }
}
