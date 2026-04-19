using Avalonia.Controls;
using TOrbit.Designer.Abstractions;
using TOrbit.Designer.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Models;
using TOrbit.Plugin.RuntimeHost.Services;
using TOrbit.Plugin.RuntimeHost.ViewModels;
using TOrbit.Plugin.RuntimeHost.Views;

namespace TOrbit.Plugin.RuntimeHost;

public sealed class RuntimeHostPlugin : BasePlugin, IVisualPlugin, IPluginPageHeaderProvider, IPluginHeaderSearchProvider, IPluginHeaderActionsProvider, IPluginDisplayInfoProvider
{
    private RuntimeHostView? _view;
    private RuntimeHostViewModel? _viewModel;
    private readonly RuntimeConfigurationStore _store = new();
    private readonly RuntimeProcessService _processService;
    private RuntimePackageService? _packageService;
    private readonly ILocalizationService _localizationService;
    private readonly PluginDescriptor _descriptor;

    public RuntimeHostPlugin(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        _processService = new RuntimeProcessService(_localizationService);
        _descriptor = CreateDescriptor<RuntimeHostPlugin>(
            RuntimeHostPluginMetadata.Instance.Id,
            _localizationService.GetString("plugins.runtime.name"),
            RuntimeHostPluginMetadata.Instance.Version,
            _localizationService.GetString("plugins.runtime.description"),
            RuntimeHostPluginMetadata.Instance.Author,
            RuntimeHostPluginMetadata.Instance.Icon,
            RuntimeHostPluginMetadata.Instance.Tags);
    }

    public override PluginDescriptor Descriptor => _descriptor;

    public event EventHandler? HeaderChanged;
    public event EventHandler? DisplayInfoChanged;

    public string DisplayName => _localizationService.GetString("plugins.runtime.name");

    public string DisplayDescription => _localizationService.GetString("plugins.runtime.description");

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

    public string SearchPlaceholder => _localizationService.GetString("runtime.searchPlaceholder");

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

    public IReadOnlyList<PluginHeaderAction> GetHeaderActions()
    {
        EnsureView();
        return _viewModel?.GetHeaderActions() ?? [];
    }

    private void EnsureView()
    {
        if (_viewModel is null)
        {
            _packageService ??= new RuntimePackageService(_store);
            var dialogService = Context.GetTool<IDesignerDialogService>();
            _viewModel = new RuntimeHostViewModel(_store, _packageService, _processService, _localizationService, dialogService);
            _viewModel.HeaderSummaryChanged += ViewModelOnHeaderSummaryChanged;
        }

        _view ??= new RuntimeHostView { DataContext = _viewModel };
    }

    protected override async ValueTask OnStopAsync(CancellationToken cancellationToken = default)
    {
        _processService.Dispose();
        await base.OnStopAsync(cancellationToken);
    }

    protected override ValueTask OnDisposeAsync()
    {
        _localizationService.LanguageChanged -= LocalizationServiceOnLanguageChanged;
        if (_viewModel is not null)
        {
            _viewModel.HeaderSummaryChanged -= ViewModelOnHeaderSummaryChanged;
            _viewModel.Dispose();
        }

        _processService.Dispose();
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
