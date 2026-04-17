using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using TOrbit.Core.Models;
using TOrbit.Core.Services;
using TOrbit.Plugin.Core.Enums;

namespace TOrbit.Plugin.Monitor.ViewModels;

public sealed partial class MonitorHomeReportViewModel : ObservableObject, IDisposable
{
    private readonly IPluginCatalogService _pluginCatalog;
    private readonly IAppDiagnosticsService _diagnosticsService;

    public ObservableCollection<string> FaultedPlugins { get; } = [];

    public int TotalCount => _pluginCatalog.Plugins.Count;
    public int RunningCount => _pluginCatalog.Plugins.Count(x => x.State == PluginState.Running);
    public int FaultedCount => _pluginCatalog.Plugins.Count(x => x.State == PluginState.Faulted);
    public int ErrorDiagnosticCount => _diagnosticsService.Entries.Count(x => x.Severity == AppDiagnosticSeverity.Error);
    public bool HasFaultedPlugins => FaultedPlugins.Count > 0;

    public MonitorHomeReportViewModel(
        IPluginCatalogService pluginCatalog,
        IAppDiagnosticsService diagnosticsService)
    {
        _pluginCatalog = pluginCatalog;
        _diagnosticsService = diagnosticsService;

        if (_pluginCatalog.Plugins is INotifyCollectionChanged observable)
            observable.CollectionChanged += PluginsChanged;

        foreach (var plugin in _pluginCatalog.Plugins)
            plugin.PropertyChanged += PluginChanged;

        _diagnosticsService.EntryRecorded += DiagnosticsEntryRecorded;

        SyncFaultedPlugins();
    }

    private void PluginsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (PluginEntry plugin in e.OldItems)
                plugin.PropertyChanged -= PluginChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (PluginEntry plugin in e.NewItems)
                plugin.PropertyChanged += PluginChanged;
        }

        RaiseSummaryProperties();
    }

    private void PluginChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PluginEntry.State)
            or nameof(PluginEntry.IsEnabled)
            or nameof(PluginEntry.LastError)
            or nameof(PluginEntry.Name))
        {
            RaiseSummaryProperties();
        }
    }

    private void DiagnosticsEntryRecorded(object? sender, AppDiagnosticEntry e)
    {
        OnPropertyChanged(nameof(ErrorDiagnosticCount));
    }

    private void RaiseSummaryProperties()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(RunningCount));
        OnPropertyChanged(nameof(FaultedCount));
        SyncFaultedPlugins();
    }

    private void SyncFaultedPlugins()
    {
        FaultedPlugins.Clear();

        foreach (var pluginName in _pluginCatalog.Plugins
                     .Where(x => x.State == PluginState.Faulted)
                     .OrderBy(x => x.Name)
                     .Select(x => x.Name)
                     .Take(3))
        {
            FaultedPlugins.Add(pluginName);
        }

        OnPropertyChanged(nameof(HasFaultedPlugins));
    }

    public void Dispose()
    {
        if (_pluginCatalog.Plugins is INotifyCollectionChanged observable)
            observable.CollectionChanged -= PluginsChanged;

        foreach (var plugin in _pluginCatalog.Plugins)
            plugin.PropertyChanged -= PluginChanged;

        _diagnosticsService.EntryRecorded -= DiagnosticsEntryRecorded;
    }
}
