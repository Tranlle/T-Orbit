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

    private readonly PluginEntry _entry;
    private readonly IPluginLifecycleService _pluginLifecycleService;

    public string Id => _entry.Id;
    public string Name => _entry.Name;
    public string Description => _entry.Description;
    public string Version => _entry.Version;
    public string Icon => _entry.Icon;
    public PluginKind Kind => _entry.Kind;
    public string KindLabel => _entry.Kind == PluginKind.Service ? "后台服务" : "前台插件";
    public string KindTagLabel => _entry.Kind == PluginKind.Service ? "backend" : "frontend";
    public bool IsBuiltIn => _entry.IsBuiltIn;
    public string SourceLabel => _entry.IsBuiltIn ? "内置" : "外部";
    public string EnabledLabel => _entry.IsEnabled ? "已启用" : "已禁用";
    public bool CanDisable => _entry.CanDisable;
    public bool CanToggleEnabled => _entry.CanDisable || _entry.IsEnabled;
    public IReadOnlyList<string> CapabilityTags => _entry.Capabilities.Select(FormatCapability).ToArray();
    public bool HasCapabilities => CapabilityTags.Count > 0;
    public string CapabilitySummary => HasCapabilities
        ? string.Join(" / ", CapabilityTags)
        : "未声明";

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

    public PluginState State => _entry.State;
    public string StateLabel => _entry.State switch
    {
        PluginState.Running => "运行中",
        PluginState.Loaded => "已停止",
        PluginState.Faulted => "故障",
        PluginState.Stopping => "停止中",
        PluginState.Starting => "启动中",
        _ => _entry.State.ToString()
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

    public string StateStatusLabel => _entry.State == PluginState.Running ? "在线" : "离线";
    public IBrush StateDotBrush => _entry.State == PluginState.Running ? SuccessForeground : DangerForeground;
    public string? LastErrorMessage => _entry.LastError?.Message;
    public string StateChangedAtText => _entry.StateChangedAt.ToString("yyyy-MM-dd HH:mm:ss");
    public bool HasError => _entry.LastError is not null;
    public bool CanRestart => _entry.IsEnabled && _entry.CanDisable && _entry.State != PluginState.Stopping;

    public IAsyncRelayCommand RestartCommand { get; }

    public PluginMonitorItemViewModel(PluginEntry entry, IPluginLifecycleService pluginLifecycleService)
    {
        _entry = entry;
        _pluginLifecycleService = pluginLifecycleService;
        _entry.PropertyChanged += EntryPropertyChanged;

        RestartCommand = new AsyncRelayCommand(
            () => _pluginLifecycleService.RestartAsync(Id),
            () => CanRestart);
    }

    private async Task SetEnabledAsync(bool value)
    {
        _entry.IsEnabled = value;
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(EnabledLabel));
        OnPropertyChanged(nameof(CanRestart));
        RestartCommand.NotifyCanExecuteChanged();

        if (value)
        {
            if (_entry.State is PluginState.Loaded or PluginState.Faulted)
                await _pluginLifecycleService.StartAsync(Id);

            return;
        }

        if (_entry.State == PluginState.Running)
            await _pluginLifecycleService.StopAsync(Id);
    }

    private void EntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PluginEntry.State)
            or nameof(PluginEntry.LastError)
            or nameof(PluginEntry.StateChangedAt)
            or nameof(PluginEntry.IsEnabled)
            or nameof(PluginEntry.Sort))
        {
            OnPropertyChanged(nameof(State));
            OnPropertyChanged(nameof(StateLabel));
            OnPropertyChanged(nameof(StateBadgeBackground));
            OnPropertyChanged(nameof(StateBadgeForeground));
            OnPropertyChanged(nameof(StateStatusLabel));
            OnPropertyChanged(nameof(StateDotBrush));
            OnPropertyChanged(nameof(LastErrorMessage));
            OnPropertyChanged(nameof(StateChangedAtText));
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(IsEnabled));
            OnPropertyChanged(nameof(EnabledLabel));
            OnPropertyChanged(nameof(CanToggleEnabled));
            OnPropertyChanged(nameof(CanRestart));
            RestartCommand.NotifyCanExecuteChanged();
        }
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
