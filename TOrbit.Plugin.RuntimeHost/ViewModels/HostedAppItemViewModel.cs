using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using TOrbit.Designer.Services;
using TOrbit.Plugin.RuntimeHost.Models;

namespace TOrbit.Plugin.RuntimeHost.ViewModels;

public sealed partial class HostedAppItemViewModel : ObservableObject
{
    private HostedAppProfile _profile;
    private HostedAppRuntimeState _runtimeState;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private bool isSelected;

    public HostedAppItemViewModel(HostedAppProfile profile, HostedAppRuntimeState runtimeState, ILocalizationService localizationService)
    {
        _profile = profile;
        _runtimeState = runtimeState;
        _localizationService = localizationService;
    }

    public HostedAppProfile Profile => _profile;
    public string Id => _profile.Id;
    public string Name => _profile.Name;
    public string Description => string.IsNullOrWhiteSpace(_profile.Description) ? L("runtime.item.noDescription") : _profile.Description;
    public string DeployFolderName => string.IsNullOrWhiteSpace(_profile.DeployFolderName) ? _profile.Id : _profile.DeployFolderName;
    public string EntryRelativePath => string.IsNullOrWhiteSpace(_profile.EntryRelativePath) ? L("runtime.notDeployed") : _profile.EntryRelativePath;
    public bool AutoStart => _profile.AutoStart;
    public string StartupModeLabel => _profile.AutoStart ? L("runtime.item.autoStart") : L("runtime.item.manual");
    public RuntimeAppStatus Status => _runtimeState.Status;
    public string StatusLabel => _runtimeState.Status switch
    {
        RuntimeAppStatus.Running => L("runtime.running"),
        RuntimeAppStatus.Starting => L("runtime.item.starting"),
        RuntimeAppStatus.Stopping => L("runtime.item.stopping"),
        RuntimeAppStatus.Faulted => L("runtime.faulted"),
        _ => L("runtime.stopped")
    };
    public string StatusSummary => _runtimeState.ProcessId is int pid
        ? string.Format(L("runtime.item.pid"), pid)
        : _runtimeState.LastExitCode is int exitCode
            ? string.Format(L("runtime.item.exit"), exitCode)
            : L("runtime.idle");
    public string LastDeployLabel => _runtimeState.LastDeployAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? L("runtime.item.noDeployment");
    public string UptimeLabel => _runtimeState.StartedAt.HasValue && _runtimeState.Status == RuntimeAppStatus.Running
        ? FormatDuration(DateTimeOffset.Now - _runtimeState.StartedAt.Value)
        : L("runtime.item.notRunning");
    public string LastError => _runtimeState.LastError;
    public bool HasError => !string.IsNullOrWhiteSpace(_runtimeState.LastError);
    public IBrush StatusBrush => _runtimeState.Status switch
    {
        RuntimeAppStatus.Running => ResolveBrush("TOrbitBadgeSuccessForegroundBrush"),
        RuntimeAppStatus.Faulted => ResolveBrush("TOrbitBadgeDangerForegroundBrush"),
        RuntimeAppStatus.Starting or RuntimeAppStatus.Stopping => ResolveBrush("TOrbitBadgeWarningForegroundBrush"),
        _ => ResolveBrush("TOrbitTextMutedBrush")
    };

    public void UpdateProfile(HostedAppProfile profile)
    {
        _profile = profile;
        RaiseAll();
    }

    public void UpdateState(HostedAppRuntimeState runtimeState)
    {
        _runtimeState = runtimeState;
        RaiseAll();
    }

    public void RefreshTiming() => OnPropertyChanged(nameof(UptimeLabel));

    public void NotifyLocalizationChanged() => RaiseAll();

    private void RaiseAll()
    {
        OnPropertyChanged(nameof(Profile));
        OnPropertyChanged(nameof(Id));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(DeployFolderName));
        OnPropertyChanged(nameof(EntryRelativePath));
        OnPropertyChanged(nameof(AutoStart));
        OnPropertyChanged(nameof(StartupModeLabel));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(StatusSummary));
        OnPropertyChanged(nameof(LastDeployLabel));
        OnPropertyChanged(nameof(UptimeLabel));
        OnPropertyChanged(nameof(LastError));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(StatusBrush));
    }

    private string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return string.Format(L("runtime.item.duration.hours"), (int)duration.TotalHours, duration.Minutes);

        if (duration.TotalMinutes >= 1)
            return string.Format(L("runtime.item.duration.minutes"), (int)duration.TotalMinutes, duration.Seconds);

        return string.Format(L("runtime.item.duration.seconds"), Math.Max(0, duration.Seconds));
    }

    private string L(string key) => _localizationService.GetString(key);

    private static IBrush ResolveBrush(string key)
        => Application.Current?.TryGetResource(key, Application.Current.ActualThemeVariant, out var value) == true && value is IBrush brush
            ? brush
            : Brushes.Transparent;
}
