using Avalonia.Controls;
using TOrbit.Core.Services;
using TOrbit.Designer.Abstractions;
using TOrbit.Designer.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Models;
using TOrbit.Plugin.Home.ViewModels;
using TOrbit.Plugin.Home.Views;

namespace TOrbit.Plugin.Home;

public sealed class HomePlugin : BasePlugin, IVisualPlugin, IHomeReportPlugin, IPluginDisplayInfoProvider
{
    private readonly IHomeReportRegistry _reportRegistry;
    private readonly IPluginCatalogService _pluginCatalog;
    private readonly IAppDiagnosticsService _diagnosticsService;
    private readonly ILocalizationService _localizationService;

    private HomeView? _view;
    private HomeViewModel? _viewModel;
    private readonly PluginDescriptor _descriptor;

    public HomePlugin(
        IHomeReportRegistry reportRegistry,
        IPluginCatalogService pluginCatalog,
        IAppDiagnosticsService diagnosticsService,
        ILocalizationService localizationService)
    {
        _reportRegistry = reportRegistry;
        _pluginCatalog = pluginCatalog;
        _diagnosticsService = diagnosticsService;
        _localizationService = localizationService;
        _descriptor = CreateDescriptor<HomePlugin>(
            HomePluginMetadata.Instance.Id,
            _localizationService.GetString("plugins.home.name"),
            HomePluginMetadata.Instance.Version,
            _localizationService.GetString("plugins.home.description"),
            HomePluginMetadata.Instance.Author,
            HomePluginMetadata.Instance.Icon,
            HomePluginMetadata.Instance.Tags);
    }

    public override PluginDescriptor Descriptor => _descriptor;

    public event EventHandler? DisplayInfoChanged;

    protected override ValueTask OnStartAsync(CancellationToken cancellationToken = default)
    {
        _localizationService.LanguageChanged += LocalizationServiceOnLanguageChanged;
        EnsureView();
        return ValueTask.CompletedTask;
    }

    public string DisplayName => _localizationService.GetString("plugins.home.name");

    public string DisplayDescription => _localizationService.GetString("plugins.home.description");

    public override Control GetMainView()
    {
        EnsureView();
        return _view!;
    }

    public IEnumerable<IHomeReportProvider> GetHomeReports()
    {
        yield return new BuiltInReportProvider(
            "torbit.home:plugin-overview",
            () => _localizationService.GetString("home.builtin.pluginOverview.title"),
            () => _localizationService.GetString("home.builtin.pluginOverview.description"),
            "StatsChart",
            10,
            Descriptor.Id,
            () => new PluginOverviewReportView(_pluginCatalog, _localizationService),
            () => _localizationService.GetString("home.builtin.category"));

        yield return new BuiltInReportProvider(
            "torbit.home:recent-diagnostics",
            () => _localizationService.GetString("home.builtin.recentDiagnostics.title"),
            () => _localizationService.GetString("home.builtin.recentDiagnostics.description"),
            "AlertCircleOutline",
            20,
            Descriptor.Id,
            () => new DiagnosticsReportView(_diagnosticsService, _localizationService),
            () => _localizationService.GetString("home.builtin.category"));
    }

    private void EnsureView()
    {
        _viewModel ??= new HomeViewModel(_reportRegistry, _localizationService);
        _view ??= new HomeView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        _localizationService.LanguageChanged -= LocalizationServiceOnLanguageChanged;
        _viewModel?.Dispose();
        _view = null;
        _viewModel = null;
        return ValueTask.CompletedTask;
    }

    private void LocalizationServiceOnLanguageChanged(object? sender, EventArgs e)
        => DisplayInfoChanged?.Invoke(this, EventArgs.Empty);

    private sealed class BuiltInReportProvider : IHomeReportProvider
    {
        private readonly Func<Control> _factory;
        private readonly string _id;
        private readonly Func<string> _title;
        private readonly Func<string> _description;
        private readonly string _icon;
        private readonly int _sort;
        private readonly string _sourcePluginId;
        private readonly Func<string> _category;

        public BuiltInReportProvider(
            string id,
            Func<string> title,
            Func<string> description,
            string icon,
            int sort,
            string sourcePluginId,
            Func<Control> factory,
            Func<string> category)
        {
            _id = id;
            _title = title;
            _description = description;
            _icon = icon;
            _sort = sort;
            _sourcePluginId = sourcePluginId;
            _factory = factory;
            _category = category;
        }

        public HomeReportDefinition GetDefinition() => new()
        {
            Id = _id,
            Title = _title(),
            Description = _description(),
            Icon = _icon,
            Category = _category(),
            Sort = _sort,
            SourcePluginId = _sourcePluginId,
            IsBuiltIn = true,
            ViewFactory = _ => new ValueTask<object>(_factory())
        };
    }
}
