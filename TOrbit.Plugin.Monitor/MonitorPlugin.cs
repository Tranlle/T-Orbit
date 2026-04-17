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

public sealed class MonitorPlugin : BasePlugin, IVisualPlugin, IHomeReportPlugin, IPluginPageHeaderProvider
{
    private readonly IPluginCatalogService _pluginCatalog;
    private readonly IPluginLifecycleService _pluginLifecycleService;
    private readonly IAppDiagnosticsService _diagnosticsService;
    private readonly IDesignerDialogService? _dialogService;

    private MonitorView? _view;
    private MonitorViewModel? _viewModel;

    public event EventHandler? HeaderChanged;

    public MonitorPlugin(
        IPluginCatalogService pluginCatalog,
        IPluginLifecycleService pluginLifecycleService,
        IAppDiagnosticsService diagnosticsService,
        IDesignerDialogService? dialogService = null)
    {
        _pluginCatalog = pluginCatalog;
        _pluginLifecycleService = pluginLifecycleService;
        _diagnosticsService = diagnosticsService;
        _dialogService = dialogService;
    }

    public override PluginDescriptor Descriptor { get; } = CreateDescriptor<MonitorPlugin>(MonitorPluginMetadata.Instance);

    protected override ValueTask OnStartAsync(CancellationToken cancellationToken = default)
    {
        EnsureView();
        return ValueTask.CompletedTask;
    }

    public IEnumerable<IHomeReportProvider> GetHomeReports()
    {
        yield return new BuiltInReportProvider(
            "torbit.monitor:runtime-overview",
            "插件监控概览",
            "展示插件运行状态、故障数量与最近故障插件。",
            "MonitorHeart",
            100,
            Descriptor.Id,
            () => new MonitorHomeReportView
            {
                DataContext = new MonitorHomeReportViewModel(_pluginCatalog, _diagnosticsService)
            });
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

    private void EnsureView()
    {
        if (_viewModel is null)
        {
            _viewModel = new MonitorViewModel(_pluginCatalog, _pluginLifecycleService, _diagnosticsService, _dialogService);
            _viewModel.HeaderSummaryChanged += ViewModelOnHeaderSummaryChanged;
        }

        _view ??= new MonitorView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
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

    private sealed class BuiltInReportProvider : IHomeReportProvider
    {
        private readonly string _id;
        private readonly string _title;
        private readonly string _description;
        private readonly string _icon;
        private readonly int _sort;
        private readonly string _sourcePluginId;
        private readonly Func<Control> _factory;

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
            Category = "Plugin Reports",
            Sort = _sort,
            SourcePluginId = _sourcePluginId,
            IsBuiltIn = false,
            ViewFactory = _ => new ValueTask<object>(_factory())
        };
    }
}
