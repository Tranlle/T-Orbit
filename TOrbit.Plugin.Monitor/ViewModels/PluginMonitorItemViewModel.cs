using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using TOrbit.Core.Models;
using TOrbit.Core.Services;
using TOrbit.Designer.Services;
using TOrbit.Plugin.Core.Enums;

namespace TOrbit.Plugin.Monitor.ViewModels;

public sealed partial class PluginMonitorItemViewModel : ObservableObject, IDisposable
{
    private static IBrush SuccessBackground => ResolveBrush("TOrbitBadgeSuccessBackgroundBrush");
    private static IBrush SuccessForeground => ResolveBrush("TOrbitBadgeSuccessForegroundBrush");
    private static IBrush WarningBackground => ResolveBrush("TOrbitBadgeWarningBackgroundBrush");
    private static IBrush WarningForeground => ResolveBrush("TOrbitBadgeWarningForegroundBrush");
    private static IBrush DangerBackground => ResolveBrush("TOrbitBadgeDangerBackgroundBrush");
    private static IBrush DangerForeground => ResolveBrush("TOrbitBadgeDangerForegroundBrush");
    private static IBrush NeutralBackground => ResolveBrush("TOrbitBadgeNeutralBackgroundBrush");
    private static IBrush NeutralForeground => ResolveBrush("TOrbitBadgeNeutralForegroundBrush");

    private readonly PluginEntry _entry;
    private readonly IPluginLifecycleService _pluginLifecycleService;
    private readonly ILocalizationService _localizationService;
    private bool _isBusy;

    public string Id => _entry.Id;
    public string Name => _entry.Name;
    public string Description => _entry.Description;
    public string Version => _entry.Version;
    public string Icon => _entry.Icon;
    public PluginKind Kind => _entry.Kind;
    public string KindLabel => _entry.Kind == PluginKind.Service ? L("monitor.item.kind.service") : L("monitor.item.kind.visual");
    public string KindTagLabel => _entry.Kind == PluginKind.Service ? "backend" : "frontend";
    public bool IsBuiltIn => _entry.IsBuiltIn;
    public string SourceLabel => _entry.IsBuiltIn ? L("monitor.item.source.builtIn") : L("monitor.item.source.external");
    public string EnabledLabel => _entry.IsEnabled ? L("monitor.item.enabled") : L("monitor.item.disabled");
    public string EnableSummary => EnabledLabel;
    public bool CanDisable => _entry.CanDisable;
    public bool CanToggleEnabled => !_isBusy && (_entry.CanDisable || _entry.IsEnabled);
    public IReadOnlyList<string> CapabilityTags => _entry.Capabilities.Select(FormatCapability).ToArray();
    public bool HasCapabilities => CapabilityTags.Count > 0;
    public string CapabilitySummary => HasCapabilities ? string.Join(" / ", CapabilityTags) : L("monitor.item.noneDeclared");
    public IReadOnlyList<string> DisplayTags => _entry.DisplayTags;
    public bool HasDisplayTags => DisplayTags.Count > 0;
    public bool IsBusy => _isBusy;

    public IReadOnlyList<PluginMonitorTagViewModel> CardTags => BuildCardTags();

    public bool IsEnabled
    {
        get => _entry.IsEnabled;
        set
        {
            if (_entry.IsEnabled == value)
                return;

            _ = SetEnabledAsync(value);
        }
    }

    public bool IsRunning
    {
        get => _entry.State is PluginState.Running or PluginState.Starting;
        set
        {
            if (IsRunning == value)
                return;

            _ = SetRunningAsync(value);
        }
    }

    public PluginState State => _entry.State;
    public string StateLabel => _entry.State switch
    {
        PluginState.Running => L("monitor.item.state.running"),
        PluginState.Loaded => L("monitor.item.state.stopped"),
        PluginState.Faulted => L("monitor.item.state.faulted"),
        PluginState.Stopping => L("monitor.item.state.stopping"),
        PluginState.Starting => L("monitor.item.state.starting"),
        _ => _entry.State.ToString()
    };

    public string StateTagLabel => _entry.State switch
    {
        PluginState.Running => L("monitor.item.state.running"),
        PluginState.Faulted => L("monitor.item.state.faulted"),
        PluginState.Starting => L("monitor.item.state.starting"),
        PluginState.Stopping => L("monitor.item.state.stopping"),
        PluginState.Loaded => L("monitor.item.state.stopped"),
        _ => L("app.state.unknown")
    };

    public IBrush StateBadgeBackground => _entry.State switch
    {
        PluginState.Running => SuccessBackground,
        PluginState.Faulted => DangerBackground,
        _ => WarningBackground
    };

    public IBrush StateBadgeForeground => _entry.State switch
    {
        PluginState.Running => SuccessForeground,
        PluginState.Faulted => DangerForeground,
        _ => WarningForeground
    };

    public string StateStatusLabel => _entry.State == PluginState.Running ? L("monitor.item.status.online") : L("monitor.item.status.offline");
    public IBrush StateDotBrush => _entry.State == PluginState.Running ? SuccessForeground : WarningForeground;
    public string? LastErrorMessage => _entry.LastError?.Message;
    public string DisplayLastErrorMessage => string.IsNullOrWhiteSpace(LastErrorMessage)
        ? L("monitor.details.noCurrentError")
        : LastErrorMessage;
    public string StateChangedAtText => _entry.StateChangedAt.ToString("yyyy-MM-dd HH:mm:ss");
    public bool HasError => _entry.LastError is not null;
    public bool CanRestart => !_isBusy && _entry.IsEnabled && _entry.CanDisable && _entry.State != PluginState.Stopping;
    public bool CanStop => !_isBusy && _entry.IsEnabled && _entry.State is PluginState.Running or PluginState.Starting;
    public bool CanToggleRunning => !_isBusy && _entry.IsEnabled && _entry.State is not PluginState.Stopping;

    public IAsyncRelayCommand RestartCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }

    public PluginMonitorItemViewModel(PluginEntry entry, IPluginLifecycleService pluginLifecycleService, ILocalizationService localizationService)
    {
        _entry = entry;
        _pluginLifecycleService = pluginLifecycleService;
        _localizationService = localizationService;
        _entry.PropertyChanged += EntryPropertyChanged;

        RestartCommand = new AsyncRelayCommand(
            () => ExecuteBusyActionAsync(() => _pluginLifecycleService.RestartAsync(Id)),
            () => CanRestart);

        StopCommand = new AsyncRelayCommand(
            () => ExecuteBusyActionAsync(() => _pluginLifecycleService.StopAsync(Id)),
            () => CanStop);
    }

    private IReadOnlyList<PluginMonitorTagViewModel> BuildCardTags()
    {
        var tags = new List<PluginMonitorTagViewModel>
        {
            new()
            {
                Text = StateLabel,
                Background = StateBadgeBackground,
                Foreground = StateBadgeForeground
            },
            new()
            {
                Text = KindLabel,
                Background = NeutralBackground,
                Foreground = NeutralForeground
            }
        };

        tags.AddRange(DisplayTags.Select(tag => new PluginMonitorTagViewModel
        {
            Text = tag,
            Background = NeutralBackground,
            Foreground = NeutralForeground
        }));

        return tags;
    }

    private async Task SetEnabledAsync(bool value)
    {
        await ExecuteBusyActionAsync(async () =>
        {
            _entry.IsEnabled = value;
            RaiseStateProperties();

            if (value)
            {
                if (_entry.State is PluginState.Loaded or PluginState.Faulted)
                    await _pluginLifecycleService.StartAsync(Id);

                return;
            }

            if (_entry.State is PluginState.Running or PluginState.Starting)
                await _pluginLifecycleService.StopAsync(Id);
        });
    }

    private async Task SetRunningAsync(bool value)
    {
        await ExecuteBusyActionAsync(async () =>
        {
            if (value)
            {
                if (_entry.IsEnabled && _entry.State is PluginState.Loaded or PluginState.Faulted)
                    await _pluginLifecycleService.StartAsync(Id);

                return;
            }

            if (_entry.State is PluginState.Running or PluginState.Starting)
                await _pluginLifecycleService.StopAsync(Id);
        });
    }

    private async Task ExecuteBusyActionAsync(Func<Task> action)
    {
        if (_isBusy)
            return;

        try
        {
            _isBusy = true;
            RaiseBusyProperties();
            await action();
        }
        finally
        {
            _isBusy = false;
            RaiseBusyProperties();
        }
    }

    private void EntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PluginEntry.State)
            or nameof(PluginEntry.LastError)
            or nameof(PluginEntry.StateChangedAt)
            or nameof(PluginEntry.IsEnabled)
            or nameof(PluginEntry.Sort))
        {
            RaiseStateProperties();
        }
    }

    private void RaiseBusyProperties()
    {
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(CanToggleEnabled));
        OnPropertyChanged(nameof(CanToggleRunning));
        OnPropertyChanged(nameof(CanRestart));
        OnPropertyChanged(nameof(CanStop));
        RestartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }

    private void RaiseStateProperties()
    {
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(KindLabel));
        OnPropertyChanged(nameof(SourceLabel));
        OnPropertyChanged(nameof(CapabilityTags));
        OnPropertyChanged(nameof(HasCapabilities));
        OnPropertyChanged(nameof(CapabilitySummary));
        OnPropertyChanged(nameof(StateLabel));
        OnPropertyChanged(nameof(StateTagLabel));
        OnPropertyChanged(nameof(StateBadgeBackground));
        OnPropertyChanged(nameof(StateBadgeForeground));
        OnPropertyChanged(nameof(StateStatusLabel));
        OnPropertyChanged(nameof(StateDotBrush));
        OnPropertyChanged(nameof(LastErrorMessage));
        OnPropertyChanged(nameof(DisplayLastErrorMessage));
        OnPropertyChanged(nameof(StateChangedAtText));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(EnabledLabel));
        OnPropertyChanged(nameof(EnableSummary));
        OnPropertyChanged(nameof(CanToggleEnabled));
        OnPropertyChanged(nameof(CanToggleRunning));
        OnPropertyChanged(nameof(CanRestart));
        OnPropertyChanged(nameof(CanStop));
        OnPropertyChanged(nameof(CardTags));
        RestartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        _entry.PropertyChanged -= EntryPropertyChanged;
    }

    public void NotifyLocalizationChanged() => RaiseStateProperties();

    private string FormatCapability(PluginCapability capability) => capability switch
    {
        PluginCapability.FileSystem => L("monitor.item.capability.fileSystem"),
        PluginCapability.LocalProcess => L("monitor.item.capability.localProcess"),
        PluginCapability.Network => L("monitor.item.capability.network"),
        PluginCapability.Secrets => L("monitor.item.capability.secrets"),
        _ => capability.ToString()
    };

    private string L(string key) => _localizationService.GetString(key);

    private static IBrush ResolveBrush(string resourceKey)
        => Application.Current?.TryGetResource(resourceKey, Application.Current.ActualThemeVariant, out var value) == true && value is IBrush brush
            ? brush
            : Brushes.Transparent;
}
