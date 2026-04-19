using Avalonia.Controls;
using TOrbit.Core.Services;
using TOrbit.Designer.Abstractions;
using TOrbit.Designer.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Models;
using TOrbit.Plugin.Monitor.ViewModels;
using TOrbit.Plugin.Monitor.Views;

namespace TOrbit.Plugin.Monitor;

public sealed class MonitorPlugin : BasePlugin, IVisualPlugin, IPluginPageHeaderProvider, IPluginHeaderSearchProvider, IPluginDisplayInfoProvider
{
    private MonitorView? _view;
    private MonitorViewModel? _viewModel;
    private readonly IPluginCatalogService _pluginCatalog;
    private readonly IPluginLifecycleService _pluginLifecycleService;
    private readonly IAppDiagnosticsService _diagnosticsService;
    private readonly IDesignerDialogService? _dialogService;
    private readonly ILocalizationService _localizationService;
    private readonly PluginDescriptor _descriptor;

    public event EventHandler? HeaderChanged;
    public event EventHandler? DisplayInfoChanged;

    public string DisplayName => _localizationService.GetString("plugins.monitor.name");

    public string DisplayDescription => _localizationService.GetString("plugins.monitor.description");

    public MonitorPlugin(
        IPluginCatalogService pluginCatalog,
        IPluginLifecycleService pluginLifecycleService,
        IAppDiagnosticsService diagnosticsService,
        ILocalizationService localizationService,
        IDesignerDialogService? dialogService = null)
    {
        _pluginCatalog = pluginCatalog;
        _pluginLifecycleService = pluginLifecycleService;
        _diagnosticsService = diagnosticsService;
        _localizationService = localizationService;
        _dialogService = dialogService;
        _descriptor = CreateDescriptor<MonitorPlugin>(
            MonitorPluginMetadata.Instance.Id,
            _localizationService.GetString("plugins.monitor.name"),
            MonitorPluginMetadata.Instance.Version,
            _localizationService.GetString("plugins.monitor.description"),
            MonitorPluginMetadata.Instance.Author,
            MonitorPluginMetadata.Instance.Icon,
            MonitorPluginMetadata.Instance.Tags);
    }

    public override PluginDescriptor Descriptor => _descriptor;

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
        return _viewModel?.CreatePageHeader();
    }

    public string SearchText
    {
        get
        {
            EnsureView();
            return _viewModel?.SearchText ?? string.Empty;
        }
        set
        {
            EnsureView();
            if (_viewModel is not null)
                _viewModel.SearchText = value;
        }
    }

    public string SearchPlaceholder => _localizationService.GetString("monitor.searchPlaceholder");

    private void EnsureView()
    {
        if (_viewModel is null)
        {
            _viewModel = new MonitorViewModel(_pluginCatalog, _pluginLifecycleService, _diagnosticsService, _localizationService, _dialogService);
            _viewModel.HeaderSummaryChanged += ViewModelOnHeaderSummaryChanged;
        }

        _view ??= new MonitorView
        {
            DataContext = _viewModel
        };
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
