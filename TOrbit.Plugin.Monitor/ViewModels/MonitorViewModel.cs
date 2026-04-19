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
    private readonly ILocalizationService _localizationService;
    private readonly IDesignerDialogService? _dialogService;
    private readonly Dictionary<string, PluginMonitorItemViewModel> _itemIndex = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedMonitorItem))]
    private PluginMonitorItemViewModel? selectedMonitorItem;

    [ObservableProperty]
    private string searchText = string.Empty;

    public ObservableCollection<PluginMonitorItemViewModel> MonitorItems { get; } = [];
    public ObservableCollection<AppDiagnosticEntry> Diagnostics { get; } = [];
    public bool HasSelectedMonitorItem => SelectedMonitorItem is not null;
    public bool HasDiagnostics => Diagnostics.Count > 0;
    public int TotalCount => _itemIndex.Count;
    public int VisibleCount => MonitorItems.Count;
    public int RunningCount => _itemIndex.Values.Count(x => x.State == PluginState.Running);
    public int FaultedCount => _itemIndex.Values.Count(x => x.State == PluginState.Faulted);
    public int DisabledCount => _itemIndex.Values.Count(x => !x.IsEnabled);
    public int ServiceCount => _itemIndex.Values.Count(x => x.Kind == PluginKind.Service);
    public int FrontendCount => _itemIndex.Values.Count(x => x.Kind == PluginKind.Visual);
    public int DiagnosticCount => Diagnostics.Count;
    public int ErrorDiagnosticCount => Diagnostics.Count(x => x.Severity == AppDiagnosticSeverity.Error);

    public event EventHandler? HeaderSummaryChanged;

    public MonitorViewModel(
        IPluginCatalogService pluginCatalog,
        IPluginLifecycleService pluginLifecycleService,
        IAppDiagnosticsService diagnosticsService,
        ILocalizationService localizationService,
        IDesignerDialogService? dialogService = null)
    {
        _pluginCatalog = pluginCatalog;
        _pluginLifecycleService = pluginLifecycleService;
        _diagnosticsService = diagnosticsService;
        _localizationService = localizationService;
        _dialogService = dialogService;
        _localizationService.LanguageChanged += LocalizationServiceOnLanguageChanged;

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

    partial void OnSearchTextChanged(string value)
    {
        SyncPlugins();
        OnPropertyChanged(nameof(VisibleCount));
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
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
            ConfirmText = _localizationService.GetString("dialog.close"),
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

        foreach (var plugin in orderedPlugins)
        {
            if (!_itemIndex.TryGetValue(plugin.Id, out var item))
            {
                item = new PluginMonitorItemViewModel(plugin, _pluginLifecycleService, _localizationService);
                _itemIndex[plugin.Id] = item;
            }
        }

        var filteredItems = orderedPlugins.Where(MatchesSearch).Select(plugin => _itemIndex[plugin.Id]).ToList();
        var filteredIds = filteredItems.Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var staleVisible in MonitorItems.Where(item => !filteredIds.Contains(item.Id)).ToList())
            MonitorItems.Remove(staleVisible);

        for (var index = 0; index < filteredItems.Count; index++)
        {
            var item = filteredItems[index];
            var currentIndex = MonitorItems.IndexOf(item);
            if (currentIndex < 0)
            {
                MonitorItems.Insert(index, item);
                continue;
            }

            if (currentIndex != index)
                MonitorItems.Move(currentIndex, index);
        }

        SelectedMonitorItem = MonitorItems.FirstOrDefault(x => x.Id == selectedMonitorId);
        RaiseSummaryProperties();
    }

    private void DiagnosticsEntryRecorded(object? sender, AppDiagnosticEntry entry) => Diagnostics.Insert(0, entry);

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
        OnPropertyChanged(nameof(VisibleCount));
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
            new() { Text = $"{_localizationService.GetString("monitor.header.visual")} {FrontendCount}", Tone = PluginPageHeaderTone.Neutral },
            new() { Text = $"{_localizationService.GetString("monitor.header.service")} {ServiceCount}", Tone = PluginPageHeaderTone.Neutral }
        };

        if (!string.IsNullOrWhiteSpace(SearchText))
            badges.Add(new PluginPageHeaderBadge { Text = $"{_localizationService.GetString("monitor.header.matched")} {VisibleCount}", Tone = PluginPageHeaderTone.Accent });

        if (DisabledCount > 0)
            badges.Add(new PluginPageHeaderBadge { Text = $"{_localizationService.GetString("monitor.header.disabled")} {DisabledCount}", Tone = PluginPageHeaderTone.Warning });

        badges.Add(new PluginPageHeaderBadge
        {
            Text = ErrorDiagnosticCount > 0
                ? $"{_localizationService.GetString("monitor.header.diagnostics")} {DiagnosticCount}"
                : _localizationService.GetString("monitor.header.healthy"),
            Tone = ErrorDiagnosticCount > 0 ? PluginPageHeaderTone.Danger : PluginPageHeaderTone.Success
        });

        return new PluginPageHeaderModel
        {
            Context = ErrorDiagnosticCount > 0
                ? _localizationService.GetString("monitor.header.contextError")
                : _localizationService.GetString("monitor.header.contextHealthy"),
            Metrics =
            [
                new PluginPageHeaderMetric { Label = _localizationService.GetString("monitor.header.total"), Value = TotalCount.ToString(), Tone = PluginPageHeaderTone.Neutral },
                new PluginPageHeaderMetric { Label = _localizationService.GetString("monitor.header.running"), Value = RunningCount.ToString(), Tone = PluginPageHeaderTone.Success },
                new PluginPageHeaderMetric { Label = _localizationService.GetString("monitor.header.faulted"), Value = FaultedCount.ToString(), Tone = PluginPageHeaderTone.Danger },
                new PluginPageHeaderMetric { Label = _localizationService.GetString("monitor.header.disabled"), Value = DisabledCount.ToString(), Tone = DisabledCount > 0 ? PluginPageHeaderTone.Warning : PluginPageHeaderTone.Success },
                new PluginPageHeaderMetric { Label = _localizationService.GetString("monitor.header.errors"), Value = ErrorDiagnosticCount.ToString(), Tone = ErrorDiagnosticCount > 0 ? PluginPageHeaderTone.Danger : PluginPageHeaderTone.Accent }
            ],
            Badges = badges
        };
    }

    private bool MatchesSearch(PluginEntry plugin)
        => string.IsNullOrWhiteSpace(SearchText)
           || plugin.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
           || plugin.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

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
        _localizationService.LanguageChanged -= LocalizationServiceOnLanguageChanged;
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

    private void LocalizationServiceOnLanguageChanged(object? sender, EventArgs e)
    {
        foreach (var item in _itemIndex.Values)
            item.NotifyLocalizationChanged();

        RaiseSummaryProperties();
    }
}
