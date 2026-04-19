using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using TOrbit.Core.Models;
using TOrbit.Core.Services;
using TOrbit.Designer.Models;
using TOrbit.Designer.Services;
using TOrbit.Designer.ViewModels.Dialogs;
using TOrbit.Plugin.Core.Enums;
using TOrbit.Plugin.Core.Models;
using TOrbit.Plugin.Monitor.Views;

namespace TOrbit.Plugin.Monitor.ViewModels;

public sealed partial class MonitorViewModel : ObservableObject, IDisposable
{
    private readonly IPluginCatalogService _pluginCatalog;
    private readonly IPluginLifecycleService _pluginLifecycleService;
    private readonly IAppDiagnosticsService _diagnosticsService;
    private readonly IDesignerDialogService? _dialogService;
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

    public event EventHandler? HeaderSummaryChanged;

    public MonitorViewModel(
        IPluginCatalogService pluginCatalog,
        IPluginLifecycleService pluginLifecycleService,
        IAppDiagnosticsService diagnosticsService,
        IDesignerDialogService? dialogService = null)
    {
        _pluginCatalog = pluginCatalog;
        _pluginLifecycleService = pluginLifecycleService;
        _diagnosticsService = diagnosticsService;
        _dialogService = dialogService;

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

    public async Task ShowDetailsAsync(PluginMonitorItemViewModel item)
    {
        SelectedMonitorItem = item;

        if (_dialogService is null || TryGetOwnerWindow() is not { } owner)
            return;

        var content = new PluginMonitorDetailsView
        {
            DataContext = new PluginMonitorDetailsViewModel(item, GetPluginDiagnostics(item))
        };

        await _dialogService.ShowSheetAsync(owner, new DesignerSheetViewModel
        {
            Title = item.Name,
            Description = item.Description,
            Content = content,
            ConfirmText = "Close",
            CancelText = string.Empty,
            Icon = DesignerDialogIcon.Info,
            BaseFontSize = 13,
            DialogWidth = 900,
            DialogHeight = 680,
            LockSize = true,
            HideSystemDecorations = true
        });
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

        SelectedMonitorItem = MonitorItems.FirstOrDefault(x => x.Id == selectedMonitorId);
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
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseSummaryProperties()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(RunningCount));
        OnPropertyChanged(nameof(FaultedCount));
        OnPropertyChanged(nameof(DisabledCount));
        OnPropertyChanged(nameof(ServiceCount));
        OnPropertyChanged(nameof(FrontendCount));
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    public PluginPageHeaderModel CreatePageHeader()
    {
        var badges = new List<PluginPageHeaderBadge>
        {
            new()
            {
                Text = $"Visual {FrontendCount}",
                Tone = PluginPageHeaderTone.Neutral
            },
            new()
            {
                Text = $"Service {ServiceCount}",
                Tone = PluginPageHeaderTone.Neutral
            }
        };

        if (DisabledCount > 0)
        {
            badges.Add(new PluginPageHeaderBadge
            {
                Text = $"Disabled {DisabledCount}",
                Tone = PluginPageHeaderTone.Warning
            });
        }

        badges.Add(new PluginPageHeaderBadge
        {
            Text = ErrorDiagnosticCount > 0 ? $"Diagnostics {DiagnosticCount}" : "Healthy",
            Tone = ErrorDiagnosticCount > 0 ? PluginPageHeaderTone.Danger : PluginPageHeaderTone.Success
        });

        return new PluginPageHeaderModel
        {
            Context = ErrorDiagnosticCount > 0
                ? "Global diagnostics are mapped to the current plugin runtime."
                : "Runtime health is stable across the current plugin set.",
            Metrics =
            [
                new PluginPageHeaderMetric
                {
                    Label = "Total",
                    Value = TotalCount.ToString(),
                    Tone = PluginPageHeaderTone.Neutral
                },
                new PluginPageHeaderMetric
                {
                    Label = "Running",
                    Value = RunningCount.ToString(),
                    Tone = PluginPageHeaderTone.Success
                },
                new PluginPageHeaderMetric
                {
                    Label = "Faulted",
                    Value = FaultedCount.ToString(),
                    Tone = PluginPageHeaderTone.Danger
                },
                new PluginPageHeaderMetric
                {
                    Label = "Errors",
                    Value = ErrorDiagnosticCount.ToString(),
                    Tone = ErrorDiagnosticCount > 0 ? PluginPageHeaderTone.Danger : PluginPageHeaderTone.Accent
                }
            ],
            Badges = badges
        };
    }

    private IReadOnlyList<AppDiagnosticEntry> GetPluginDiagnostics(PluginMonitorItemViewModel item)
        => Diagnostics
            .Where(entry =>
                entry.Source.Contains(item.Id, StringComparison.OrdinalIgnoreCase)
                || entry.Source.Contains(item.Name, StringComparison.OrdinalIgnoreCase)
                || entry.Message.Contains(item.Id, StringComparison.OrdinalIgnoreCase)
                || entry.Message.Contains(item.Name, StringComparison.OrdinalIgnoreCase))
            .Take(8)
            .ToArray();

    private static Window? TryGetOwnerWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;

        return null;
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
