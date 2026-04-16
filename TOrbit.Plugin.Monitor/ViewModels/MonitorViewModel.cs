using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using TOrbit.Core.Models;
using TOrbit.Core.Services;
using TOrbit.Plugin.Core.Enums;

namespace TOrbit.Plugin.Monitor.ViewModels;

public sealed partial class MonitorViewModel : ObservableObject, IDisposable
{
    private readonly IPluginCatalogService _pluginCatalog;
    private readonly IPluginLifecycleService _pluginLifecycleService;
    private readonly IAppDiagnosticsService _diagnosticsService;
    private readonly Dictionary<string, PluginMonitorItemViewModel> _itemIndex = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedMonitorItem))]
    private PluginMonitorItemViewModel? selectedMonitorItem;

    public ObservableCollection<PluginMonitorItemViewModel> MonitorItems { get; } = [];
    public ObservableCollection<AppDiagnosticEntry> Diagnostics { get; } = [];
    public bool HasSelectedMonitorItem => SelectedMonitorItem is not null;
    public bool HasDiagnostics => Diagnostics.Count > 0;
    public int TotalCount => MonitorItems.Count;
    public int RunningCount => MonitorItems.Count(x => x.State == PluginState.Running);
    public int FaultedCount => MonitorItems.Count(x => x.State == PluginState.Faulted);
    public int DisabledCount => MonitorItems.Count(x => !x.IsEnabled);
    public int ServiceCount => MonitorItems.Count(x => x.Kind == PluginKind.Service);
    public int FrontendCount => MonitorItems.Count(x => x.Kind == PluginKind.Visual);
    public int DiagnosticCount => Diagnostics.Count;
    public int ErrorDiagnosticCount => Diagnostics.Count(x => x.Severity == AppDiagnosticSeverity.Error);

    public MonitorViewModel(
        IPluginCatalogService pluginCatalog,
        IPluginLifecycleService pluginLifecycleService,
        IAppDiagnosticsService diagnosticsService)
    {
        _pluginCatalog = pluginCatalog;
        _pluginLifecycleService = pluginLifecycleService;
        _diagnosticsService = diagnosticsService;

        foreach (var entry in diagnosticsService.Entries.OrderByDescending(x => x.Timestamp))
            Diagnostics.Add(entry);

        Diagnostics.CollectionChanged += DiagnosticsChanged;
        _diagnosticsService.EntryRecorded += DiagnosticsEntryRecorded;

        SyncPlugins();

        if (_pluginCatalog.Plugins is INotifyCollectionChanged observable)
            observable.CollectionChanged += CatalogPluginsChanged;

        foreach (var plugin in _pluginCatalog.Plugins)
            plugin.PropertyChanged += PluginChanged;
    }

    private void CatalogPluginsChanged(object? sender, NotifyCollectionChangedEventArgs e)
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

        SyncPlugins();
    }

    private void PluginChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PluginEntry.Sort)
            or nameof(PluginEntry.IsEnabled)
            or nameof(PluginEntry.LastError)
            or nameof(PluginEntry.State)
            or nameof(PluginEntry.StateChangedAt)
            or nameof(PluginEntry.Name))
        {
            SyncPlugins();
        }
    }

    private void SyncPlugins()
    {
        var selectedMonitorId = SelectedMonitorItem?.Id;
        var orderedPlugins = _pluginCatalog.Plugins.OrderBy(x => x.Sort).ThenBy(x => x.Name).ToList();
        var activeIds = orderedPlugins.Select(plugin => plugin.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var staleId in _itemIndex.Keys.Where(id => !activeIds.Contains(id)).ToList())
        {
            var staleItem = _itemIndex[staleId];
            _itemIndex.Remove(staleId);
            MonitorItems.Remove(staleItem);
            staleItem.Dispose();
        }

        for (var index = 0; index < orderedPlugins.Count; index++)
        {
            var plugin = orderedPlugins[index];
            if (!_itemIndex.TryGetValue(plugin.Id, out var item))
            {
                item = new PluginMonitorItemViewModel(plugin, _pluginLifecycleService);
                _itemIndex[plugin.Id] = item;
                MonitorItems.Insert(index, item);
                continue;
            }

            var currentIndex = MonitorItems.IndexOf(item);
            if (currentIndex >= 0 && currentIndex != index)
                MonitorItems.Move(currentIndex, index);
        }

        SelectedMonitorItem = MonitorItems.FirstOrDefault(x => x.Id == selectedMonitorId) ?? MonitorItems.FirstOrDefault();
        RaiseSummaryProperties();
    }

    private void DiagnosticsEntryRecorded(object? sender, AppDiagnosticEntry entry)
    {
        Diagnostics.Insert(0, entry);
    }

    private void DiagnosticsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasDiagnostics));
        OnPropertyChanged(nameof(DiagnosticCount));
        OnPropertyChanged(nameof(ErrorDiagnosticCount));
    }

    private void RaiseSummaryProperties()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(RunningCount));
        OnPropertyChanged(nameof(FaultedCount));
        OnPropertyChanged(nameof(DisabledCount));
        OnPropertyChanged(nameof(ServiceCount));
        OnPropertyChanged(nameof(FrontendCount));
    }

    public void Dispose()
    {
        Diagnostics.CollectionChanged -= DiagnosticsChanged;
        _diagnosticsService.EntryRecorded -= DiagnosticsEntryRecorded;

        if (_pluginCatalog.Plugins is INotifyCollectionChanged observable)
            observable.CollectionChanged -= CatalogPluginsChanged;

        foreach (var plugin in _pluginCatalog.Plugins)
            plugin.PropertyChanged -= PluginChanged;

        foreach (var item in _itemIndex.Values)
            item.Dispose();

        _itemIndex.Clear();
    }
}
