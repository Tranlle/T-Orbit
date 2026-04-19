using Avalonia.Controls;
using TOrbit.Core.Services;
using TOrbit.Designer.Abstractions;
using TOrbit.Designer.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Monitor.ViewModels;
using TOrbit.Plugin.Monitor.Views;

namespace TOrbit.Plugin.Monitor;

public sealed class MonitorPlugin : BasePlugin, IVisualPlugin
{
    private MonitorView? _view;
    private MonitorViewModel? _viewModel;
    private readonly IPluginCatalogService _pluginCatalog;
    private readonly IPluginLifecycleService _pluginLifecycleService;
    private readonly IAppDiagnosticsService _diagnosticsService;
    private readonly IDesignerDialogService? _dialogService;

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

    public override Control GetMainView()
    {
        EnsureView();
        return _view!;
    }

    private void EnsureView()
    {
        _viewModel ??= new MonitorViewModel(_pluginCatalog, _pluginLifecycleService, _diagnosticsService, _dialogService);
        _view ??= new MonitorView
        {
            DataContext = _viewModel
        };
    }

    protected override ValueTask OnDisposeAsync()
    {
        _viewModel?.Dispose();
        _view = null;
        _viewModel = null;
        return ValueTask.CompletedTask;
    }
}
