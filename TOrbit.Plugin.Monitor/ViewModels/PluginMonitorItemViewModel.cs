using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using TOrbit.Core.Models;
using TOrbit.Core.Services;
using TOrbit.Plugin.Core.Enums;

namespace TOrbit.Plugin.Monitor.ViewModels;

public sealed partial class PluginMonitorItemViewModel : ObservableObject, IDisposable
{
    private static readonly IBrush SuccessBackground = Brush.Parse("#1A2D20");
    private static readonly IBrush SuccessForeground = Brush.Parse("#4FD18A");
    private static readonly IBrush WarningBackground = Brush.Parse("#2E2415");
    private static readonly IBrush WarningForeground = Brush.Parse("#E8BE64");
    private static readonly IBrush DangerBackground = Brush.Parse("#2E1820");
    private static readonly IBrush DangerForeground = Brush.Parse("#ED6E7D");
    private static readonly IBrush NeutralBackground = Brush.Parse("#1F242C");
    private static readonly IBrush NeutralForeground = Brush.Parse("#9CA8B8");

    private readonly PluginEntry _entry;
    private readonly IPluginLifecycleService _pluginLifecycleService;
    private bool _isBusy;

    public string Id => _entry.Id;
    public string Name => _entry.Name;
    public string Description => _entry.Description;
    public string Version => _entry.Version;
    public string Icon => _entry.Icon;
    public PluginKind Kind => _entry.Kind;
    public string KindLabel => _entry.Kind == PluginKind.Service ? "Service" : "Visual";
    public string KindTagLabel => _entry.Kind == PluginKind.Service ? "backend" : "frontend";
    public bool IsBuiltIn => _entry.IsBuiltIn;
    public string SourceLabel => _entry.IsBuiltIn ? "Built-in" : "External";
    public string EnabledLabel => _entry.IsEnabled ? "Enabled" : "Disabled";
    public string EnableSummary => _entry.IsEnabled ? "Enabled" : "Disabled";
    public bool CanDisable => _entry.CanDisable;
    public bool CanToggleEnabled => !_isBusy && (_entry.CanDisable || _entry.IsEnabled);
    public IReadOnlyList<string> CapabilityTags => _entry.Capabilities.Select(FormatCapability).ToArray();
    public bool HasCapabilities => CapabilityTags.Count > 0;
    public string CapabilitySummary => HasCapabilities ? string.Join(" / ", CapabilityTags) : "None declared";
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
        PluginState.Running => "Running",
        PluginState.Loaded => "Stopped",
        PluginState.Faulted => "Faulted",
        PluginState.Stopping => "Stopping",
        PluginState.Starting => "Starting",
        _ => _entry.State.ToString()
    };

    public string StateTagLabel => _entry.State switch
    {
        PluginState.Running => "Running",
        PluginState.Faulted => "Faulted",
        PluginState.Starting => "Starting",
        PluginState.Stopping => "Stopping",
        PluginState.Loaded => "Stopped",
        _ => "Unknown"
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

    public string StateStatusLabel => _entry.State == PluginState.Running ? "Online" : "Offline";
    public IBrush StateDotBrush => _entry.State == PluginState.Running ? SuccessForeground : WarningForeground;
    public string? LastErrorMessage => _entry.LastError?.Message;
    public string StateChangedAtText => _entry.StateChangedAt.ToString("yyyy-MM-dd HH:mm:ss");
    public bool HasError => _entry.LastError is not null;
    public bool CanRestart => !_isBusy && _entry.IsEnabled && _entry.CanDisable && _entry.State != PluginState.Stopping;
    public bool CanStop => !_isBusy && _entry.IsEnabled && _entry.State is PluginState.Running or PluginState.Starting;
    public bool CanToggleRunning => !_isBusy && _entry.IsEnabled && _entry.State is not PluginState.Stopping;

    public IAsyncRelayCommand RestartCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }

    public PluginMonitorItemViewModel(PluginEntry entry, IPluginLifecycleService pluginLifecycleService)
    {
        _entry = entry;
        _pluginLifecycleService = pluginLifecycleService;
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
        OnPropertyChanged(nameof(StateLabel));
        OnPropertyChanged(nameof(StateTagLabel));
        OnPropertyChanged(nameof(StateBadgeBackground));
        OnPropertyChanged(nameof(StateBadgeForeground));
        OnPropertyChanged(nameof(StateStatusLabel));
        OnPropertyChanged(nameof(StateDotBrush));
        OnPropertyChanged(nameof(LastErrorMessage));
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

    private static string FormatCapability(PluginCapability capability) => capability switch
    {
        PluginCapability.FileSystem => "FileSystem",
        PluginCapability.LocalProcess => "LocalProcess",
        PluginCapability.Network => "Network",
        PluginCapability.Secrets => "Secrets",
        _ => capability.ToString()
    };
}
