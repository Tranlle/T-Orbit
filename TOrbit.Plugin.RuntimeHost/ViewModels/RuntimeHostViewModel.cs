using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TOrbit.Designer.Models;
using TOrbit.Designer.Services;
using TOrbit.Designer.ViewModels;
using TOrbit.Designer.ViewModels.Dialogs;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Models;
using TOrbit.Plugin.RuntimeHost.Models;
using TOrbit.Plugin.RuntimeHost.Services;

namespace TOrbit.Plugin.RuntimeHost.ViewModels;

public sealed partial class RuntimeHostViewModel : PluginBaseViewModel, IDisposable
{
    private readonly RuntimeConfigurationStore _store;
    private readonly RuntimePackageService _packageService;
    private readonly RuntimeProcessService _processService;
    private readonly ILocalizationService _localizationService;
    private readonly IDesignerDialogService? _dialogService;
    private readonly Dictionary<string, HostedAppItemViewModel> _itemIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly DispatcherTimer _uptimeTimer;
    private bool _isSaving;

    public event EventHandler? HeaderSummaryChanged;

    public ObservableCollection<HostedAppItemViewModel> Apps { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedApp))]
    [NotifyPropertyChangedFor(nameof(CanDeploy))]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    [NotifyPropertyChangedFor(nameof(CanStop))]
    [NotifyPropertyChangedFor(nameof(CanRestart))]
    private HostedAppItemViewModel? selectedApp;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string outputLog = string.Empty;

    [ObservableProperty]
    private string editingName = string.Empty;

    [ObservableProperty]
    private string editingDescription = string.Empty;

    [ObservableProperty]
    private string editingDeployFolderName = string.Empty;

    [ObservableProperty]
    private string editingEntryRelativePath = string.Empty;

    [ObservableProperty]
    private bool editingRunWithDotnet;

    [ObservableProperty]
    private bool editingAutoStart;

    [ObservableProperty]
    private string environmentVariablesText = string.Empty;

    public bool HasSelectedApp => SelectedApp is not null;
    public bool HasApps => Apps.Count > 0;
    public int TotalCount => _itemIndex.Count;
    public int RunningCount => _itemIndex.Values.Count(static app => app.Status == RuntimeAppStatus.Running);
    public int FaultedCount => _itemIndex.Values.Count(static app => app.Status == RuntimeAppStatus.Faulted);
    public int StoppedCount => _itemIndex.Values.Count(static app => app.Status == RuntimeAppStatus.Stopped);
    public bool CanDeploy => HasSelectedApp;
    public bool CanStart => SelectedApp is not null && SelectedApp.Status is RuntimeAppStatus.Stopped or RuntimeAppStatus.Faulted;
    public bool CanStop => SelectedApp is not null && SelectedApp.Status is RuntimeAppStatus.Running or RuntimeAppStatus.Starting;
    public bool CanRestart => SelectedApp is not null && SelectedApp.Profile.EntryRelativePath.Length > 0;
    public string SelectedDeployPath => SelectedApp is null ? string.Empty : _store.GetCurrentDeploymentDirectory(SelectedApp.Id);
    public string SelectedLogsPath => SelectedApp is null ? string.Empty : _store.GetLogsDirectory(SelectedApp.Id);
    public string SelectedPackageName => SelectedApp?.Profile.PackagePath is { Length: > 0 } package ? Path.GetFileName(package) : _localizationService.GetString("runtime.noPackage");
    public string SelectedEntryMode => EditingRunWithDotnet ? _localizationService.GetString("runtime.dotnetHost") : _localizationService.GetString("runtime.directExecutable");
    public IReadOnlyList<PropertyGridItem> SelectedRuntimeProperties =>
        SelectedApp is null
            ? []
            :
            [
                new PropertyGridItem { Label = _localizationService.GetString("runtime.status"), Value = SelectedApp.StatusLabel },
                new PropertyGridItem { Label = _localizationService.GetString("runtime.deploy"), Value = SelectedApp.LastDeployLabel },
                new PropertyGridItem { Label = _localizationService.GetString("runtime.entry"), Value = string.IsNullOrWhiteSpace(SelectedApp.Profile.EntryRelativePath) ? _localizationService.GetString("runtime.notDeployed") : SelectedApp.Profile.EntryRelativePath },
                new PropertyGridItem { Label = _localizationService.GetString("runtime.mode"), Value = SelectedApp.Profile.RunWithDotnet ? _localizationService.GetString("runtime.dotnetHost") : _localizationService.GetString("runtime.directExecutable") },
                new PropertyGridItem { Label = _localizationService.GetString("runtime.uptime"), Value = SelectedApp.UptimeLabel },
                new PropertyGridItem { Label = _localizationService.GetString("runtime.package"), Value = SelectedPackageName }
            ];

    public IAsyncRelayCommand AddProfileCommand { get; }
    public IAsyncRelayCommand RemoveProfileCommand { get; }
    public IAsyncRelayCommand DeployPackageCommand { get; }
    public IAsyncRelayCommand StartCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }
    public IAsyncRelayCommand RestartCommand { get; }
    public IRelayCommand SaveProfileCommand { get; }
    public IRelayCommand ClearLogCommand { get; }
    public IRelayCommand<HostedAppItemViewModel> SelectAppCommand { get; }

    public RuntimeHostViewModel(
        RuntimeConfigurationStore store,
        RuntimePackageService packageService,
        RuntimeProcessService processService,
        ILocalizationService localizationService,
        IDesignerDialogService? dialogService = null)
    {
        _store = store;
        _packageService = packageService;
        _processService = processService;
        _localizationService = localizationService;
        _dialogService = dialogService;
        _localizationService.LanguageChanged += LocalizationServiceOnLanguageChanged;
        StatusMessage = _localizationService.GetString("runtime.ready");

        AddProfileCommand = new AsyncRelayCommand(AddProfileAsync);
        RemoveProfileCommand = new AsyncRelayCommand(RemoveProfileAsync);
        DeployPackageCommand = new AsyncRelayCommand(DeployPackageAsync);
        StartCommand = new AsyncRelayCommand(StartSelectedAsync);
        StopCommand = new AsyncRelayCommand(StopSelectedAsync);
        RestartCommand = new AsyncRelayCommand(RestartSelectedAsync);
        SaveProfileCommand = new RelayCommand(SaveSelectedProfile);
        ClearLogCommand = new RelayCommand(() => OutputLog = string.Empty);
        SelectAppCommand = new RelayCommand<HostedAppItemViewModel>(item =>
        {
            if (item is not null)
                SelectedApp = item;
        });

        _processService.LogReceived += ProcessServiceOnLogReceived;
        _processService.StateChanged += ProcessServiceOnStateChanged;

        _uptimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _uptimeTimer.Tick += (_, _) =>
        {
            foreach (var item in _itemIndex.Values)
                item.RefreshTiming();
        };
        _uptimeTimer.Start();

        LoadProfiles();
        _ = AutoStartProfilesAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
        OnPropertyChanged(nameof(HasApps));
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnSelectedAppChanged(HostedAppItemViewModel? value)
    {
        foreach (var item in _itemIndex.Values)
            item.IsSelected = ReferenceEquals(item, value);

        LoadSelectedEditor();
        OutputLog = value is null ? string.Empty : LocalizePersistedLogs(_store.LoadRecentLogs(value.Id));
        RaiseSelectionProperties();
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnEditingNameChanged(string value) => UpdateProfileDraft();
    partial void OnEditingDescriptionChanged(string value) => UpdateProfileDraft();
    partial void OnEditingDeployFolderNameChanged(string value) => UpdateProfileDraft();
    partial void OnEditingEntryRelativePathChanged(string value) => UpdateProfileDraft();
    partial void OnEditingRunWithDotnetChanged(bool value) => UpdateProfileDraft();
    partial void OnEditingAutoStartChanged(bool value) => UpdateProfileDraft();
    partial void OnEnvironmentVariablesTextChanged(string value) => UpdateProfileDraft();

    public PluginPageHeaderModel CreatePageHeader()
    {
        var badges = new List<PluginPageHeaderBadge>
        {
            new() { Text = $"{_localizationService.GetString("runtime.profiles")} {TotalCount}", Tone = PluginPageHeaderTone.Neutral },
            new() { Text = RunningCount > 0 ? $"{_localizationService.GetString("runtime.running")} {RunningCount}" : _localizationService.GetString("runtime.idle"), Tone = RunningCount > 0 ? PluginPageHeaderTone.Success : PluginPageHeaderTone.Neutral }
        };

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            badges.Add(new PluginPageHeaderBadge
            {
                Text = $"{_localizationService.GetString("monitor.header.matched")} {Apps.Count}",
                Tone = PluginPageHeaderTone.Accent
            });
        }

        if (FaultedCount > 0)
        {
            badges.Add(new PluginPageHeaderBadge
            {
                Text = $"{_localizationService.GetString("runtime.faulted")} {FaultedCount}",
                Tone = PluginPageHeaderTone.Danger
            });
        }

        return new PluginPageHeaderModel
        {
            Context = _localizationService.GetString("runtime.headerContext"),
            Metrics =
            [
                new PluginPageHeaderMetric { Label = _localizationService.GetString("runtime.apps"), Value = TotalCount.ToString(), Tone = PluginPageHeaderTone.Neutral },
                new PluginPageHeaderMetric { Label = _localizationService.GetString("runtime.running"), Value = RunningCount.ToString(), Tone = PluginPageHeaderTone.Success },
                new PluginPageHeaderMetric { Label = _localizationService.GetString("runtime.stopped"), Value = StoppedCount.ToString(), Tone = PluginPageHeaderTone.Neutral },
                new PluginPageHeaderMetric { Label = _localizationService.GetString("runtime.faulted"), Value = FaultedCount.ToString(), Tone = FaultedCount > 0 ? PluginPageHeaderTone.Danger : PluginPageHeaderTone.Success }
            ],
            Badges = badges
        };
    }

    public IReadOnlyList<PluginHeaderAction> GetHeaderActions()
    {
        return
        [
            new PluginHeaderAction(_localizationService.GetString("runtime.addApp"), AddProfileCommand, true, true),
            new PluginHeaderAction(_localizationService.GetString("runtime.deployZip"), DeployPackageCommand, CanDeploy),
            new PluginHeaderAction(CanStop ? _localizationService.GetString("runtime.stop") : _localizationService.GetString("runtime.start"), CanStop ? StopCommand : StartCommand, HasSelectedApp)
        ];
    }

    public async Task AutoStartProfilesAsync()
    {
        foreach (var item in _itemIndex.Values.Where(static item => item.Profile.AutoStart && !string.IsNullOrWhiteSpace(item.Profile.EntryRelativePath)))
        {
            try
            {
                await StartAsync(item.Profile);
            }
            catch (Exception ex)
            {
                AppendLog(item.Id, "Error", $"{_localizationService.GetString("runtime.messages.autoStartFailed")}: {ex.Message}");
            }
        }
    }

    private void LoadProfiles()
    {
        var settings = _store.Load();
        foreach (var profile in settings.Profiles.OrderBy(static profile => profile.Name, StringComparer.OrdinalIgnoreCase))
        {
            var state = _processService.GetState(profile.Id);
            var manifest = _store.LoadDeploymentManifest(profile.Id);
            if (manifest is not null)
            {
                profile.EntryRelativePath = manifest.EntryRelativePath;
                profile.RunWithDotnet = manifest.RunWithDotnet;
                state.LastDeployAt = manifest.DeployedAt;
            }

            var item = new HostedAppItemViewModel(profile, state, _localizationService);
            _itemIndex[profile.Id] = item;
        }

        ApplyFilter();
        SelectedApp = Apps.FirstOrDefault();
        OnPropertyChanged(nameof(HasApps));
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyFilter()
    {
        var filtered = _itemIndex.Values
            .Where(MatchesSearch)
            .OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Apps.Clear();
        foreach (var item in filtered)
            Apps.Add(item);

        OnPropertyChanged(nameof(HasApps));

        if (SelectedApp is not null && !Apps.Contains(SelectedApp))
            SelectedApp = Apps.FirstOrDefault();
    }

    private bool MatchesSearch(HostedAppItemViewModel item)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        return item.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.EntryRelativePath.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    private async Task AddProfileAsync()
    {
        var seed = 1;
        while (_itemIndex.Values.Any(item => string.Equals(item.Name, string.Format(_localizationService.GetString("runtime.defaultAppName"), seed), StringComparison.OrdinalIgnoreCase)))
            seed++;

        var profile = new HostedAppProfile
        {
            Name = string.Format(_localizationService.GetString("runtime.defaultAppName"), seed),
            DeployFolderName = string.Format(_localizationService.GetString("runtime.defaultDeployFolder"), seed)
        };

        var item = new HostedAppItemViewModel(profile, new HostedAppRuntimeState { ProfileId = profile.Id }, _localizationService);
        _itemIndex[profile.Id] = item;
        PersistSettings();
        ApplyFilter();
        SelectedApp = item;
        StatusMessage = _localizationService.GetString("runtime.messages.profileCreated");
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    private async Task RemoveProfileAsync()
    {
        if (SelectedApp is null)
            return;

        if (!await ConfirmAsync(
                _localizationService.GetString("runtime.messages.deleteProfileTitle"),
                string.Format(_localizationService.GetString("runtime.messages.deleteProfileMessage"), SelectedApp.Name),
                true))
            return;

        await StopSelectedAsync();

        var appRoot = _store.GetAppRoot(SelectedApp.Id);
        if (Directory.Exists(appRoot))
            Directory.Delete(appRoot, true);

        _itemIndex.Remove(SelectedApp.Id);
        PersistSettings();
        ApplyFilter();
        SelectedApp = Apps.FirstOrDefault();
        StatusMessage = _localizationService.GetString("runtime.messages.profileDeleted");
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task DeployPackageAsync()
    {
        if (SelectedApp is null || _dialogService is null || TryGetOwnerWindow() is not { } owner)
            return;

        var file = await _dialogService.PickFileAsync(owner, _localizationService.GetString("runtime.messages.selectPackage"), [new FilePickerFileType(_localizationService.GetString("runtime.zipArchive"))
        {
            Patterns = ["*.zip"],
            MimeTypes = ["application/zip"]
        }]);

        if (string.IsNullOrWhiteSpace(file))
            return;

        if (SelectedApp.Status is RuntimeAppStatus.Running or RuntimeAppStatus.Starting)
            await StopSelectedAsync();

        if (Directory.Exists(_store.GetCurrentDeploymentDirectory(SelectedApp.Id)))
        {
            var confirmed = await ConfirmAsync(
                _localizationService.GetString("runtime.messages.redeployTitle"),
                string.Format(_localizationService.GetString("runtime.messages.redeployMessage"), SelectedApp.Name),
                false);
            if (!confirmed)
                return;
        }

        try
        {
            var result = _packageService.DeployPackage(SelectedApp.Profile, file);
            SelectedApp.Profile.PackagePath = result.PackagePath;
            SelectedApp.Profile.EntryRelativePath = result.EntryRelativePath;
            SelectedApp.Profile.RunWithDotnet = result.RunWithDotnet;

            var state = _processService.GetState(SelectedApp.Id);
            state.LastDeployAt = result.DeployedAt;
            SelectedApp.UpdateProfile(SelectedApp.Profile);
            SelectedApp.UpdateState(state);
            PersistSettings();

            AppendLog(SelectedApp.Id, "Info", string.Format(_localizationService.GetString("runtime.messages.packageDeployedFrom"), Path.GetFileName(file)));
            AppendLog(SelectedApp.Id, "Info", string.Format(_localizationService.GetString("runtime.messages.detectedEntry"), result.EntryRelativePath, result.RunWithDotnet ? _localizationService.GetString("runtime.dotnetHost") : _localizationService.GetString("runtime.directExecutable")));
            StatusMessage = _localizationService.GetString("runtime.messages.packageDeployed");
            RaiseSelectionProperties();
            HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            AppendLog(SelectedApp.Id, "Error", $"{_localizationService.GetString("runtime.messages.deploymentFailed")}: {ex.Message}");
            StatusMessage = _localizationService.GetString("runtime.messages.deploymentFailed");
        }
    }

    private async Task StartSelectedAsync()
    {
        if (SelectedApp is null)
            return;

        await StartAsync(SelectedApp.Profile);
    }

    private async Task StartAsync(HostedAppProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.EntryRelativePath))
            throw new InvalidOperationException(_localizationService.GetString("runtime.messages.deployBeforeStart"));

        var request = new RuntimeStartRequest
        {
            ProfileId = profile.Id,
            WorkingDirectory = _store.GetCurrentDeploymentDirectory(profile.Id),
            EntryRelativePath = profile.EntryRelativePath,
            RunWithDotnet = profile.RunWithDotnet,
            EnvironmentVariables = profile.EnvironmentVariables
        };

        try
        {
            var state = await _processService.StartAsync(request);
            var item = _itemIndex[profile.Id];
            item.UpdateState(state);
            StatusMessage = string.Format(_localizationService.GetString("runtime.messages.started"), profile.Name);
            HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
            RaiseSelectionProperties();
        }
        catch (Exception ex)
        {
            AppendLog(profile.Id, "Error", $"{_localizationService.GetString("runtime.messages.startFailed")}: {ex.Message}");
            StatusMessage = string.Format(_localizationService.GetString("runtime.messages.failedToStart"), profile.Name);
            throw;
        }
    }

    private async Task StopSelectedAsync()
    {
        if (SelectedApp is null)
            return;

        var state = await _processService.StopAsync(SelectedApp.Id);
        _itemIndex[SelectedApp.Id].UpdateState(state);
        StatusMessage = string.Format(_localizationService.GetString("runtime.messages.stopped"), SelectedApp.Name);
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
        RaiseSelectionProperties();
    }

    private async Task RestartSelectedAsync()
    {
        if (SelectedApp is null)
            return;

        try
        {
            var request = new RuntimeStartRequest
            {
                ProfileId = SelectedApp.Id,
                WorkingDirectory = _store.GetCurrentDeploymentDirectory(SelectedApp.Id),
                EntryRelativePath = SelectedApp.Profile.EntryRelativePath,
                RunWithDotnet = SelectedApp.Profile.RunWithDotnet,
                EnvironmentVariables = SelectedApp.Profile.EnvironmentVariables
            };

            var state = await _processService.RestartAsync(request);
            SelectedApp.UpdateState(state);
            StatusMessage = string.Format(_localizationService.GetString("runtime.messages.restarted"), SelectedApp.Name);
            HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
            RaiseSelectionProperties();
        }
        catch (Exception ex)
        {
            AppendLog(SelectedApp.Id, "Error", $"{_localizationService.GetString("runtime.messages.restartFailed")}: {ex.Message}");
            StatusMessage = _localizationService.GetString("runtime.messages.restartFailed");
        }
    }

    private void SaveSelectedProfile()
    {
        PersistSettings();
        StatusMessage = _localizationService.GetString("runtime.messages.profileSaved");
    }

    private void LoadSelectedEditor()
    {
        if (SelectedApp is null)
        {
            EditingName = string.Empty;
            EditingDescription = string.Empty;
            EditingDeployFolderName = string.Empty;
            EditingEntryRelativePath = string.Empty;
            EditingRunWithDotnet = false;
            EditingAutoStart = false;
            EnvironmentVariablesText = string.Empty;
            return;
        }

        _isSaving = true;
        EditingName = SelectedApp.Profile.Name;
        EditingDescription = SelectedApp.Profile.Description;
        EditingDeployFolderName = SelectedApp.Profile.DeployFolderName;
        EditingEntryRelativePath = SelectedApp.Profile.EntryRelativePath;
        EditingRunWithDotnet = SelectedApp.Profile.RunWithDotnet;
        EditingAutoStart = SelectedApp.Profile.AutoStart;
        EnvironmentVariablesText = ToEnvironmentText(SelectedApp.Profile.EnvironmentVariables);
        _isSaving = false;
    }

    private void UpdateProfileDraft()
    {
        if (_isSaving || SelectedApp is null)
            return;

        SelectedApp.Profile.Name = string.IsNullOrWhiteSpace(EditingName) ? _localizationService.GetString("runtime.messages.unnamedApp") : EditingName.Trim();
        SelectedApp.Profile.Description = EditingDescription.Trim();
        SelectedApp.Profile.DeployFolderName = string.IsNullOrWhiteSpace(EditingDeployFolderName)
            ? SelectedApp.Profile.Id
            : EditingDeployFolderName.Trim();
        SelectedApp.Profile.EntryRelativePath = EditingEntryRelativePath.Trim();
        SelectedApp.Profile.RunWithDotnet = EditingRunWithDotnet;
        SelectedApp.Profile.AutoStart = EditingAutoStart;
        SelectedApp.Profile.EnvironmentVariables = ParseEnvironmentVariables(EnvironmentVariablesText);
        SelectedApp.UpdateProfile(SelectedApp.Profile);
        PersistSettings();
        ApplyFilter();
        RaiseSelectionProperties();
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void PersistSettings()
    {
        var settings = new RuntimeHostSettings
        {
            Profiles = _itemIndex.Values.Select(static item => item.Profile).OrderBy(static profile => profile.Name, StringComparer.OrdinalIgnoreCase).ToList()
        };
        _store.Save(settings);
    }

    private void ProcessServiceOnLogReceived(object? sender, HostedAppLogEntry entry)
        => Dispatcher.UIThread.Post(() => AppendLog(entry.ProfileId, entry.Level, entry.Message));

    private void ProcessServiceOnStateChanged(object? sender, HostedAppRuntimeState state)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!_itemIndex.TryGetValue(state.ProfileId, out var item))
                return;

            var persistedState = state;
            if (persistedState.LastDeployAt is null)
                persistedState.LastDeployAt = item.Profile.PackagePath.Length > 0
                    ? _store.LoadDeploymentManifest(item.Id)?.DeployedAt
                    : null;

            item.UpdateState(persistedState);
            if (ReferenceEquals(SelectedApp, item))
                RaiseSelectionProperties();

            HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    private void AppendLog(string profileId, string level, string message)
    {
        var localizedLevel = LocalizeLogLevel(level);
        var entry = new HostedAppLogEntry
        {
            ProfileId = profileId,
            Level = localizedLevel,
            Message = message
        };

        _store.AppendLog(profileId, entry);

        if (SelectedApp?.Id == profileId)
        {
            if (!string.IsNullOrWhiteSpace(OutputLog))
                OutputLog += Environment.NewLine;
            OutputLog += $"[{entry.Timestamp:HH:mm:ss}] [{localizedLevel}] {message}";
        }
    }

    private string LocalizePersistedLogs(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        return content
            .Replace("[Info]", $"[{LocalizeLogLevel("Info")}]", StringComparison.Ordinal)
            .Replace("[Error]", $"[{LocalizeLogLevel("Error")}]", StringComparison.Ordinal)
            .Replace("[Warning]", $"[{LocalizeLogLevel("Warning")}]", StringComparison.Ordinal);
    }

    private string LocalizeLogLevel(string level)
    {
        return level switch
        {
            "Info" => _localizationService.GetString("runtime.logLevel.info"),
            "Error" => _localizationService.GetString("runtime.logLevel.error"),
            "Warning" => _localizationService.GetString("runtime.logLevel.warning"),
            _ => level
        };
    }

    private async Task<bool> ConfirmAsync(string title, string message, bool isDanger)
    {
        if (_dialogService is null || TryGetOwnerWindow() is not { } owner)
            return true;

        var result = await _dialogService.ShowConfirmAsync(owner, new DesignerConfirmDialogViewModel
        {
            Title = title,
            Message = message,
            ConfirmText = isDanger ? _localizationService.GetString("runtime.delete") : _localizationService.GetString("dialog.confirm"),
            CancelText = _localizationService.GetString("dialog.cancel"),
            IsDanger = isDanger,
            Icon = isDanger ? DesignerDialogIcon.Warning : DesignerDialogIcon.Info
        });

        return result.IsConfirmed;
    }

    private static Window? TryGetOwnerWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;

        return null;
    }

    private void RaiseSelectionProperties()
    {
        OnPropertyChanged(nameof(HasSelectedApp));
        OnPropertyChanged(nameof(CanDeploy));
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanStop));
        OnPropertyChanged(nameof(CanRestart));
        OnPropertyChanged(nameof(SelectedDeployPath));
        OnPropertyChanged(nameof(SelectedLogsPath));
        OnPropertyChanged(nameof(SelectedPackageName));
        OnPropertyChanged(nameof(SelectedEntryMode));
        OnPropertyChanged(nameof(SelectedRuntimeProperties));
    }

    private static List<HostedAppEnvironmentVariable> ParseEnvironmentVariables(string text)
    {
        var variables = new List<HostedAppEnvironmentVariable>();
        foreach (var line in text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key = line[..separatorIndex].Trim();
            if (string.IsNullOrWhiteSpace(key))
                continue;

            variables.Add(new HostedAppEnvironmentVariable
            {
                Key = key,
                Value = line[(separatorIndex + 1)..].Trim()
            });
        }

        return variables;
    }

    private static string ToEnvironmentText(IEnumerable<HostedAppEnvironmentVariable> variables)
        => string.Join(Environment.NewLine, variables.Where(static item => !string.IsNullOrWhiteSpace(item.Key)).Select(static item => $"{item.Key}={item.Value}"));

    public void Dispose()
    {
        _uptimeTimer.Stop();
        _localizationService.LanguageChanged -= LocalizationServiceOnLanguageChanged;
        _processService.LogReceived -= ProcessServiceOnLogReceived;
        _processService.StateChanged -= ProcessServiceOnStateChanged;
    }

    private void LocalizationServiceOnLanguageChanged(object? sender, EventArgs e)
    {
        foreach (var item in _itemIndex.Values)
            item.NotifyLocalizationChanged();

        if (SelectedApp is not null)
            OutputLog = LocalizePersistedLogs(_store.LoadRecentLogs(SelectedApp.Id));

        if (string.IsNullOrWhiteSpace(StatusMessage)
            || string.Equals(StatusMessage, _localizationService.GetString("runtime.ready"), StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = _localizationService.GetString("runtime.ready");
        }

        RaiseSelectionProperties();
        ApplyFilter();
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }
}
