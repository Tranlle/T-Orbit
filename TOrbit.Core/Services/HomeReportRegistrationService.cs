using System.Collections.Specialized;
using System.ComponentModel;
using TOrbit.Core.Models;
using TOrbit.Plugin.Core.Abstractions;

namespace TOrbit.Core.Services;

public sealed class HomeReportRegistrationService : IHomeReportRegistrationService, IDisposable
{
    private readonly IPluginCatalogService _catalog;
    private readonly IHomeReportRegistry _registry;
    private bool _initialized;

    public HomeReportRegistrationService(IPluginCatalogService catalog, IHomeReportRegistry registry)
    {
        _catalog = catalog;
        _registry = registry;
    }

    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        if (_catalog.Plugins is INotifyCollectionChanged observable)
            observable.CollectionChanged += PluginsChanged;

        foreach (var plugin in _catalog.Plugins)
            plugin.PropertyChanged += PluginChanged;

        SyncAll();
    }

    private void PluginsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (PluginEntry plugin in e.OldItems)
            {
                plugin.PropertyChanged -= PluginChanged;
                _registry.RemoveBySource(plugin.Id);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (PluginEntry plugin in e.NewItems)
                plugin.PropertyChanged += PluginChanged;
        }

        SyncAll();
    }

    private void PluginChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not PluginEntry plugin)
            return;

        if (e.PropertyName is nameof(PluginEntry.IsEnabled)
            or nameof(PluginEntry.Name)
            or nameof(PluginEntry.StateChangedAt))
        {
            SyncPlugin(plugin);
        }
    }

    private void SyncAll()
    {
        foreach (var plugin in _catalog.Plugins)
            SyncPlugin(plugin);
    }

    private void SyncPlugin(PluginEntry plugin)
    {
        _registry.RemoveBySource(plugin.Id);

        if (!plugin.IsEnabled || plugin.Plugin is not IHomeReportPlugin reportPlugin)
            return;

        _registry.RegisterRange(
            reportPlugin.GetHomeReports()
                .Select(provider => provider.GetDefinition())
                .Where(definition => definition.ViewFactory is not null));
    }

    public void Dispose()
    {
        if (_catalog.Plugins is INotifyCollectionChanged observable)
            observable.CollectionChanged -= PluginsChanged;

        foreach (var plugin in _catalog.Plugins)
            plugin.PropertyChanged -= PluginChanged;
    }
}
