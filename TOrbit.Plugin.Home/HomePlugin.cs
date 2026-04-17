using Avalonia.Controls;
using TOrbit.Core.Services;
using TOrbit.Designer.Abstractions;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Models;
using TOrbit.Plugin.Home.ViewModels;
using TOrbit.Plugin.Home.Views;

namespace TOrbit.Plugin.Home;

public sealed class HomePlugin : BasePlugin, IVisualPlugin, IHomeReportPlugin
{
    private readonly IHomeReportRegistry _reportRegistry;
    private readonly IPluginCatalogService _pluginCatalog;
    private readonly IAppDiagnosticsService _diagnosticsService;

    private HomeView? _view;
    private HomeViewModel? _viewModel;

    public HomePlugin(
        IHomeReportRegistry reportRegistry,
        IPluginCatalogService pluginCatalog,
        IAppDiagnosticsService diagnosticsService)
    {
        _reportRegistry = reportRegistry;
        _pluginCatalog = pluginCatalog;
        _diagnosticsService = diagnosticsService;
    }

    public override PluginDescriptor Descriptor { get; } = CreateDescriptor<HomePlugin>(HomePluginMetadata.Instance);

    protected override ValueTask OnStartAsync(CancellationToken cancellationToken = default)
    {
        EnsureView();
        return ValueTask.CompletedTask;
    }

    public override Control GetMainView()
    {
        EnsureView();
        return _view!;
    }

    public IEnumerable<IHomeReportProvider> GetHomeReports()
    {
        yield return new BuiltInReportProvider(
            "torbit.home:plugin-overview",
            "插件概览",
            "展示当前插件目录的启用、运行与故障概况。",
            "StatsChart",
            10,
            Descriptor.Id,
            () => new PluginOverviewReportView(_pluginCatalog));

        yield return new BuiltInReportProvider(
            "torbit.home:recent-diagnostics",
            "最近诊断",
            "展示宿主最近记录的错误与警告信息。",
            "AlertCircleOutline",
            20,
            Descriptor.Id,
            () => new DiagnosticsReportView(_diagnosticsService));
    }

    private void EnsureView()
    {
        _viewModel ??= new HomeViewModel(_reportRegistry);
        _view ??= new HomeView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        _viewModel?.Dispose();
        _view = null;
        _viewModel = null;
        return ValueTask.CompletedTask;
    }

    private sealed class BuiltInReportProvider : IHomeReportProvider
    {
        private readonly Func<Control> _factory;
        private readonly string _id;
        private readonly string _title;
        private readonly string _description;
        private readonly string _icon;
        private readonly int _sort;
        private readonly string _sourcePluginId;

        public BuiltInReportProvider(
            string id,
            string title,
            string description,
            string icon,
            int sort,
            string sourcePluginId,
            Func<Control> factory)
        {
            _id = id;
            _title = title;
            _description = description;
            _icon = icon;
            _sort = sort;
            _sourcePluginId = sourcePluginId;
            _factory = factory;
        }

        public HomeReportDefinition GetDefinition() => new()
        {
            Id = _id,
            Title = _title,
            Description = _description,
            Icon = _icon,
            Category = "概览",
            Sort = _sort,
            SourcePluginId = _sourcePluginId,
            IsBuiltIn = true,
            ViewFactory = _ => new ValueTask<object>(_factory())
        };
    }
}
