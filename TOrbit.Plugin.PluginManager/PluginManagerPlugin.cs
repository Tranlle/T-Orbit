using Avalonia.Controls;
using TOrbit.Core.Services;
using TOrbit.Designer.Abstractions;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.PluginManager.ViewModels;
using TOrbit.Plugin.PluginManager.Views;

namespace TOrbit.Plugin.PluginManager;

public sealed class PluginManagerPlugin : BasePlugin, IVisualPlugin
{
    private readonly IPluginCatalogService _pluginCatalog;

    private PluginManagerView? _view;
    private PluginManagerViewModel? _viewModel;

    public PluginManagerPlugin(IPluginCatalogService pluginCatalog)
        => _pluginCatalog = pluginCatalog;

    public override PluginDescriptor Descriptor { get; } = CreateDescriptor<PluginManagerPlugin>(PluginManagerPluginMetadata.Instance);

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
        if (_viewModel is null)
            _viewModel = new PluginManagerViewModel(_pluginCatalog);

        _view ??= new PluginManagerView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        _view = null;
        _viewModel = null;
        return ValueTask.CompletedTask;
    }
}
