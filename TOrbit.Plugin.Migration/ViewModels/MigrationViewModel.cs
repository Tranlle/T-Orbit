using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TOrbit.Designer.Models;
using TOrbit.Designer.Services;
using TOrbit.Designer.ViewModels;
using TOrbit.Designer.ViewModels.Dialogs;
using TOrbit.Plugin.Migration.Models;
using TOrbit.Plugin.Migration.Services;
using TOrbit.Plugin.Migration.Views;

namespace TOrbit.Plugin.Migration.ViewModels;

public sealed partial class MigrationViewModel : PluginBaseViewModel, IDisposable
{
    private readonly MigrationService _service;
    private readonly MigrationConfigurationStore _configurationStore;
    private readonly ILocalizationService _localizationService;
    private readonly IDesignerDialogService? _dialogService;
    private MigrationVariables _variables;
    private CancellationTokenSource? _cts;
    private bool _isInitializing = true;

    public event EventHandler? HeaderSummaryChanged;

    public ObservableCollection<MigrationEntry> Migrations { get; } = [];
    public ObservableCollection<DbConnectionProfile> DbProfiles { get; } = [];

    [ObservableProperty]
    private string projectPath = string.Empty;

    [ObservableProperty]
    private DbConnectionProfile? activeProfile;

    [ObservableProperty]
    private int newProfileSeed = 1;

    [ObservableProperty]
    private MigrationEntry? selectedMigration;

    [ObservableProperty]
    private string editorContent = string.Empty;

    [ObservableProperty]
    private bool isEditorDirty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string outputLog = string.Empty;

    [ObservableProperty]
    private string newMigrationName = string.Empty;

    [ObservableProperty]
    private DbConnectionProfile? editingProfile;

    [ObservableProperty]
    private string editingProjectPath = string.Empty;

    public bool HasProjectPath => !string.IsNullOrWhiteSpace(ProjectPath);
    public bool HasSelectedProfile => ActiveProfile is not null;
    public bool IsSqlServer => HasSelectedProfile && ActiveProfile!.DbType == DbType.SqlServer;
    public bool IsPostgreSQL => HasSelectedProfile && ActiveProfile!.DbType == DbType.PostgreSQL;
    public bool IsMySQL => HasSelectedProfile && ActiveProfile!.DbType == DbType.MySQL;
    public bool IsIdle => !IsBusy;
    public bool HasMigrations => Migrations.Count > 0;
    public int MigrationCount => Migrations.Count;
    public int ProfileCount => DbProfiles.Count;
    public bool HasSelectedMigration => SelectedMigration is not null;
    public bool CanRollback => SelectedMigration is not null && IsLastMigration(SelectedMigration);
    public string StartupProjectPath => ProjectPath;
    public string CurrentDatabaseTypeDisplay => HasSelectedProfile ? ActiveProfile!.DisplayName : string.Empty;
    public string OutputDirectory => HasProjectPath && HasSelectedProfile
        ? MigrationService.GetOutputDirectory(ProjectPath, ActiveProfile!)
        : string.Empty;

    public string SelectedMigrationFileName => HasSelectedMigration
        ? $"{SelectedMigration!.FullName}.cs"
        : L("migration.messages.noMigrationSelected");

    public IReadOnlyList<PropertyGridItem> ActiveProfileProperties =>
    [
        new PropertyGridItem
        {
            Label = L("migration.databaseType"),
            Value = CurrentDatabaseTypeDisplay
        },
        new PropertyGridItem
        {
            Label = L("migration.migrationFile"),
            Value = HasMigrations
                ? new ComboBox
                {
                    [!ComboBox.ItemsSourceProperty] = new Binding(nameof(Migrations)),
                    [!SelectingItemsControl.SelectedItemProperty] = new Binding(nameof(SelectedMigration)) { Mode = BindingMode.TwoWay },
                    ItemTemplate = new FuncDataTemplate<MigrationEntry>((item, _) => new TextBlock
                    {
                        Text = item?.FullName,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    })
                }
                : new TextBlock
                {
                    Text = L("migration.messages.noMigrationFiles"),
                    Classes = { "caption-muted" }
                }
        }
    ];

    public string RollbackTooltip => SelectedMigration switch
    {
        null => L("migration.messages.rollbackSelectFirst"),
        _ when !IsLastMigration(SelectedMigration) => L("migration.messages.rollbackOnlyLatest"),
        _ when SelectedMigration.Status == MigrationStatus.Applied => L("migration.messages.rollbackApplied"),
        _ => L("migration.messages.rollbackPending")
    };

    public IReadOnlyList<DbConnectionProfile> OrderedProfiles => DbProfiles
        .Where(profile => ActiveProfile is not null && profile.DbType == ActiveProfile.DbType)
        .OrderByDescending(profile => ReferenceEquals(profile, ActiveProfile))
        .ThenBy(profile => profile.EffectiveProfileName, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    public IReadOnlyList<DesignerOptionItem> DbTypeOptions => BuildDbTypeOptions();

    public DesignerOptionItem? SelectedEditingDbTypeOption
    {
        get => DbTypeOptions.FirstOrDefault(item => item.Value is DbType dbType && EditingProfile is not null && dbType == EditingProfile.DbType);
        set
        {
            if (value?.Value is DbType dbType && EditingProfile is not null)
            {
                EditingProfile.DbType = dbType;
                OnPropertyChanged();
            }
        }
    }

    public IRelayCommand RefreshCommand { get; }
    public IRelayCommand AddProfileCommand { get; }
    public IRelayCommand DeleteProfileCommand { get; }
    public IRelayCommand OpenEditProfileCommand { get; }
    public IRelayCommand ShowNewPanelCommand { get; }
    public IRelayCommand OpenMigrationFileCommand { get; }
    public IRelayCommand ExecuteUpdateCommand { get; }
    public IRelayCommand RollbackSelectedCommand { get; }
    public IRelayCommand CancelOperationCommand { get; }
    public IRelayCommand ClearLogCommand { get; }
    public IRelayCommand BrowseProjectPathCommand { get; }
    public IRelayCommand<DbConnectionProfile> SelectProfileCommand { get; }
    public IRelayCommand<MigrationEntry> SelectMigrationCommand { get; }

    public MigrationViewModel(
        MigrationService service,
        MigrationConfigurationStore configurationStore,
        MigrationVariables variables,
        ILocalizationService localizationService,
        IDesignerDialogService? dialogService = null)
    {
        _service = service;
        _configurationStore = configurationStore;
        _variables = variables;
        _localizationService = localizationService;
        _dialogService = dialogService;

        _localizationService.LanguageChanged += LocalizationServiceOnLanguageChanged;
        StatusMessage = L("migration.messages.ready");

        InitializeProfilesFromExistingConfig();

        RefreshCommand = new AsyncRelayCommand(ReloadProfilesAsync);
        AddProfileCommand = new RelayCommand(AddProfile);
        DeleteProfileCommand = new AsyncRelayCommand(DeleteProfileAsync);
        OpenEditProfileCommand = new AsyncRelayCommand(OpenEditProfileAsync);
        ShowNewPanelCommand = new AsyncRelayCommand(OpenNewMigrationPanelAsync);
        OpenMigrationFileCommand = new AsyncRelayCommand(OpenMigrationFileAsync);
        ExecuteUpdateCommand = new AsyncRelayCommand(ExecuteUpdateAsync);
        RollbackSelectedCommand = new AsyncRelayCommand(RollbackSelectedAsync);
        CancelOperationCommand = new RelayCommand(() => _cts?.Cancel());
        ClearLogCommand = new RelayCommand(() => OutputLog = string.Empty);
        BrowseProjectPathCommand = new AsyncRelayCommand(BrowseProjectPathAsync);
        SelectProfileCommand = new RelayCommand<DbConnectionProfile>(profile =>
        {
            if (profile is not null)
                ActiveProfile = profile;
        });
        SelectMigrationCommand = new RelayCommand<MigrationEntry>(migration =>
        {
            if (migration is not null)
                SelectedMigration = migration;
        });

        _isInitializing = false;
        RaiseDerivedProperties();

        if (HasProjectPath && HasSelectedProfile)
            _ = RefreshMigrationsAsync();
    }

    partial void OnProjectPathChanged(string value) => RaiseDerivedProperties();

    partial void OnActiveProfileChanged(DbConnectionProfile? value)
    {
        foreach (var item in DbProfiles)
            item.IsSelected = ReferenceEquals(item, value);

        ClearMigrationSelection();
        RaiseDerivedProperties();

        if (!_isInitializing && value is not null && HasProjectPath)
            _ = RefreshMigrationsAsync();
    }

    partial void OnSelectedMigrationChanged(MigrationEntry? value)
    {
        foreach (var item in Migrations)
            item.IsSelected = ReferenceEquals(item, value);

        RaiseDerivedProperties();
        LoadSelectedFileContent();
    }

    partial void OnIsBusyChanged(bool value) => RaiseDerivedProperties();
    partial void OnEditorContentChanged(string value) => IsEditorDirty = SelectedMigration?.Content != value;

    public void UpdateVariables(MigrationVariables variables)
    {
        _variables = variables;

        foreach (var profile in DbProfiles.Where(profile => string.IsNullOrWhiteSpace(profile.ConnectionString)))
            profile.ConnectionString = _variables.DefaultConnectionString;

        if (EditingProfile is not null && string.IsNullOrWhiteSpace(EditingProfile.ConnectionString))
            EditingProfile.ConnectionString = _variables.DefaultConnectionString;
    }

    public void Dispose()
    {
        _localizationService.LanguageChanged -= LocalizationServiceOnLanguageChanged;
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private void RaiseDerivedProperties()
    {
        OnPropertyChanged(nameof(HasProjectPath));
        OnPropertyChanged(nameof(HasSelectedProfile));
        OnPropertyChanged(nameof(IsSqlServer));
        OnPropertyChanged(nameof(IsPostgreSQL));
        OnPropertyChanged(nameof(IsMySQL));
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(HasMigrations));
        OnPropertyChanged(nameof(MigrationCount));
        OnPropertyChanged(nameof(ProfileCount));
        OnPropertyChanged(nameof(HasSelectedMigration));
        OnPropertyChanged(nameof(CanRollback));
        OnPropertyChanged(nameof(StartupProjectPath));
        OnPropertyChanged(nameof(CurrentDatabaseTypeDisplay));
        OnPropertyChanged(nameof(OutputDirectory));
        OnPropertyChanged(nameof(SelectedMigrationFileName));
        OnPropertyChanged(nameof(ActiveProfileProperties));
        OnPropertyChanged(nameof(RollbackTooltip));
        OnPropertyChanged(nameof(OrderedProfiles));
        OnPropertyChanged(nameof(DbTypeOptions));
        OnPropertyChanged(nameof(SelectedEditingDbTypeOption));
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void InitializeProfilesFromExistingConfig(string? preferredProfileId = null)
    {
        _isInitializing = true;
        DbProfiles.Clear();
        ClearMigrationSelection();

        var settings = _configurationStore.LoadSettings();
        var config = _configurationStore.LoadConfig();
        var resolvedProjectPath = !string.IsNullOrWhiteSpace(settings.ProjectPath) ? settings.ProjectPath : config.ProjectPath;
        var profiles = settings.Profiles.Count > 0 ? settings.Profiles : config.Profiles;

        ProjectPath = !string.IsNullOrWhiteSpace(resolvedProjectPath) ? resolvedProjectPath : string.Empty;

        if (profiles.Count > 0)
        {
            foreach (var profile in profiles)
                DbProfiles.Add(ToRuntimeProfile(profile));

            var activeProfileId = preferredProfileId ?? settings.ActiveProfileId ?? config.ActiveProfileId;
            ActiveProfile = DbProfiles.FirstOrDefault(profile => profile.Id == activeProfileId) ?? DbProfiles.First();
            ActiveProfile.IsSelected = true;
            _isInitializing = false;
            RaiseDerivedProperties();
            return;
        }

        var defaultProfile = CreateDefaultProfile();
        DbProfiles.Add(defaultProfile);
        ActiveProfile = defaultProfile;
        ActiveProfile.IsSelected = true;
        _isInitializing = false;
        RaiseDerivedProperties();
    }

    private DbConnectionProfile CreateDefaultProfile()
    {
        return new DbConnectionProfile
        {
            ProfileName = $"Profile {NewProfileSeed++}",
            DbType = DbType.SqlServer,
            UseWorkspace = true,
            ConnectionString = _variables.DefaultConnectionString
        };
    }

    private void AddProfile()
    {
        var source = ActiveProfile ?? CreateDefaultProfile();
        var profile = new DbConnectionProfile
        {
            ProfileName = $"Profile {NewProfileSeed++}",
            DbType = source.DbType,
            ContextName = source.ContextName,
            ConnectionString = string.IsNullOrWhiteSpace(source.ConnectionString)
                ? _variables.DefaultConnectionString
                : source.ConnectionString,
            UseWorkspace = source.UseWorkspace
        };

        DbProfiles.Add(profile);
        ActiveProfile = profile;

        foreach (var item in DbProfiles)
            item.IsSelected = ReferenceEquals(item, profile);

        AppendLog(Format("migration.messages.profileAdded", profile.EffectiveProfileName));
        RaiseDerivedProperties();
    }

    private async Task DeleteProfileAsync()
    {
        if (DbProfiles.Count <= 1 || ActiveProfile is null)
            return;

        var profile = ActiveProfile;
        var confirmed = await ShowConfirmAsync(
            L("migration.messages.deleteProfileTitle"),
            string.Format(
                L("migration.messages.deleteProfileMessage"),
                profile.EffectiveProfileName,
                profile.DisplayName),
            L("migration.deleteProfile"),
            isDanger: true,
            note: L("migration.messages.deleteProfileNote"));

        if (!confirmed)
            return;

        var index = DbProfiles.IndexOf(profile);
        DbProfiles.Remove(profile);
        ActiveProfile = DbProfiles[Math.Max(0, index - 1)];

        foreach (var item in DbProfiles)
            item.IsSelected = ReferenceEquals(item, ActiveProfile);

        SaveConfig();
        AppendLog(Format("migration.messages.profileDeleted", profile.EffectiveProfileName));
        RaiseDerivedProperties();
    }

    private async Task OpenEditProfileAsync()
    {
        if (ActiveProfile is null)
            return;

        EditingProjectPath = ProjectPath;
        EditingProfile = new DbConnectionProfile
        {
            Id = ActiveProfile.Id,
            ProfileName = ActiveProfile.ProfileName,
            DbType = ActiveProfile.DbType,
            ContextName = ActiveProfile.ContextName,
            ConnectionString = string.IsNullOrWhiteSpace(ActiveProfile.ConnectionString)
                ? _variables.DefaultConnectionString
                : ActiveProfile.ConnectionString,
            UseWorkspace = ActiveProfile.UseWorkspace
        };

        var confirmed = await ShowProfileEditorSheetAsync(
            L("migration.messages.editProfileTitle"),
            L("migration.messages.editProfileDescription"),
            L("migration.messages.saveProfile"));

        if (confirmed)
        {
            ConfirmEditProfile();
            return;
        }

        EditingProfile = null;
        EditingProjectPath = string.Empty;
    }

    private void ConfirmEditProfile()
    {
        if (EditingProfile is null || ActiveProfile is null)
            return;

        ProjectPath = EditingProjectPath;
        ActiveProfile.ProfileName = EditingProfile.ProfileName;
        ActiveProfile.DbType = EditingProfile.DbType;
        ActiveProfile.ContextName = EditingProfile.ContextName;
        ActiveProfile.ConnectionString = EditingProfile.ConnectionString;
        ActiveProfile.UseWorkspace = EditingProfile.UseWorkspace;

        EditingProfile = null;
        SaveConfig();
        ClearMigrationSelection();
        _ = RefreshMigrationsAsync();
        RaiseDerivedProperties();
    }

    private void SaveConfig()
    {
        if (!HasProjectPath || ActiveProfile is null)
            return;

        var profiles = DbProfiles.Select(ToSettingsProfile).ToList();
        var settings = new MigrationSettings
        {
            ProjectPath = ProjectPath,
            ActiveProfileId = ActiveProfile.Id,
            Profiles = profiles
        };

        _configurationStore.SaveSettings(settings);
        _configurationStore.SaveConfig(new MigrationToolConfig
        {
            ProjectPath = ProjectPath,
            ActiveProfileId = ActiveProfile.Id,
            Profiles = profiles
        });

        AppendLog(L("migration.messages.configSavedLog"));
        StatusMessage = L("migration.messages.configSaved");
    }

    private void ClearMigrationSelection()
    {
        Migrations.Clear();
        SelectedMigration = null;
        EditorContent = string.Empty;
        IsEditorDirty = false;
        RaiseDerivedProperties();
    }

    private async Task ReloadProfilesAsync()
    {
        var currentProfileId = ActiveProfile?.Id;
        InitializeProfilesFromExistingConfig(currentProfileId);

        AppendLog(L("migration.messages.profilesReloadedLog"));
        StatusMessage = L("migration.messages.profilesReloaded");

        if (HasProjectPath && HasSelectedProfile)
            await RefreshMigrationsAsync();
    }

    private async Task RefreshMigrationsAsync()
    {
        ClearMigrationSelection();
        if (!HasProjectPath || ActiveProfile is null)
            return;

        var (migrations, raw) = await _service.ListMigrationsAsync(ProjectPath, StartupProjectPath, ActiveProfile);

        Migrations.Clear();
        foreach (var migration in migrations)
        {
            migration.IsSelected = false;
            Migrations.Add(migration);
        }

        if (Migrations.Count > 0)
        {
            var last = Migrations.OrderBy(item => item.TimestampId).Last();
            foreach (var migration in Migrations)
                migration.IsLast = migration.FullName == last.FullName;

            SelectedMigration = Migrations[0];
        }
        else
        {
            SelectedMigration = null;
            EditorContent = string.Empty;
        }

        if (!raw.Success && !string.IsNullOrWhiteSpace(raw.Error))
        {
            AppendLog("[MigrationListFallbackWarning] dotnet ef migrations list failed - showing filesystem scan as fallback");
            AppendLog(raw.Error.TrimEnd());
        }

        StatusMessage = string.Format(L("migration.messages.migrationCountStatus"), Migrations.Count, ActiveProfile.EffectiveProfileName);
        AppendLog(Format("migration.messages.migrationsLoadedLog", Migrations.Count));
        RaiseDerivedProperties();
        LoadSelectedFileContent();
    }

    private async Task OpenNewMigrationPanelAsync()
    {
        var error = ValidateMigrationPrerequisites(includeName: false);
        if (!string.IsNullOrWhiteSpace(error))
        {
            await ShowAlertAsync(L("migration.messages.newMigrationUnavailableTitle"), error);
            return;
        }

        var result = await ShowPromptAsync(
            L("migration.messages.newMigrationTitle"),
            L("migration.messages.newMigrationPrompt"),
            L("migration.messages.createMigration"),
            placeholder: L("migration.messages.newMigrationPlaceholder"),
            note: L("migration.messages.newMigrationNote"));

        if (!result.IsConfirmed)
            return;

        NewMigrationName = result.Value?.Trim() ?? string.Empty;
        await AddMigrationAsync();
    }

    private async Task AddMigrationAsync()
    {
        var error = ValidateMigrationPrerequisites(includeName: true);
        if (!string.IsNullOrWhiteSpace(error))
        {
            await ShowAlertAsync(L("migration.messages.createMigrationUnavailableTitle"), error);
            return;
        }

        var name = NewMigrationName.Trim();
        await RunOperationAsync(
            $"dotnet ef migrations add {name}",
            token => _service.AddMigrationAsync(ProjectPath, StartupProjectPath, name, ActiveProfile!, token));

        NewMigrationName = string.Empty;
        await RefreshMigrationsAsync();
    }

    private string? ValidateMigrationPrerequisites(bool includeName)
    {
        if (ActiveProfile is null)
            return L("migration.messages.validation.selectProfile");

        if (string.IsNullOrWhiteSpace(ProjectPath))
            return L("migration.messages.validation.selectDomainProject");

        if (!File.Exists(ProjectPath))
            return string.Format(L("migration.messages.validation.domainProjectMissing"), ProjectPath);

        if (string.IsNullOrWhiteSpace(ActiveProfile.ContextName))
            return L("migration.messages.validation.contextRequired");

        var designSettingsPath = Path.Combine(Path.GetDirectoryName(ProjectPath)!, "DesignSettings.json");
        if (!File.Exists(designSettingsPath))
            return string.Format(L("migration.messages.validation.designSettingsMissing"), designSettingsPath);

        if (includeName)
        {
            if (string.IsNullOrWhiteSpace(NewMigrationName))
                return L("migration.messages.validation.migrationNameRequired");

            if (NewMigrationName.Trim().Length < 3)
                return L("migration.messages.validation.migrationNameTooShort");
        }

        return null;
    }

    private async Task ExecuteUpdateAsync()
    {
        if (ActiveProfile is null)
            return;

        var confirmed = await ShowConfirmAsync(
            L("migration.messages.executeUpdateTitle"),
            string.Format(
                L("migration.messages.executeUpdateMessage"),
                ActiveProfile.EffectiveProfileName,
                ActiveProfile.DisplayName,
                ActiveProfile.ContextName),
            L("migration.messages.executeUpdateConfirm"),
            note: L("migration.messages.executeUpdateNote"));

        if (!confirmed)
            return;

        await RunOperationAsync(
            "dotnet ef database update",
            token => _service.UpdateDatabaseAsync(ProjectPath, StartupProjectPath, ActiveProfile, null, token));

        await RefreshMigrationsAsync();
    }

    private async Task RollbackSelectedAsync()
    {
        if (SelectedMigration is null || !CanRollback || ActiveProfile is null)
            return;

        var migration = SelectedMigration;
        var steps = migration.Status == MigrationStatus.Applied
            ? L("migration.messages.rollbackStepsApplied")
            : L("migration.messages.rollbackStepsPending");

        var confirmed = await ShowConfirmAsync(
            L("migration.messages.rollbackTitle"),
            string.Format(
                L("migration.messages.rollbackMessage"),
                migration.FullName,
                ActiveProfile.DisplayName,
                ActiveProfile.ContextName,
                steps),
            L("migration.messages.rollbackConfirm"),
            isDanger: true,
            note: L("migration.messages.rollbackNote"));

        if (!confirmed)
            return;

        await RunOperationAsync(
            $"Rollback {migration.MigrationName}",
            token => _service.RollbackLastMigrationAsync(ProjectPath, StartupProjectPath, ActiveProfile, migration, Migrations.ToList(), token));

        SelectedMigration = null;
        await RefreshMigrationsAsync();
    }

    private void LoadSelectedFileContent()
    {
        if (SelectedMigration is null)
        {
            EditorContent = string.Empty;
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedMigration.FilePath))
        {
            EditorContent = L("migration.messages.migrationFileMissingContent");
            IsEditorDirty = false;
            return;
        }

        var content = _service.ReadMigrationFile(SelectedMigration.FilePath);
        SelectedMigration.Content = content;
        EditorContent = content;
        IsEditorDirty = false;
    }

    private async Task OpenMigrationFileAsync()
    {
        if (SelectedMigration is null)
        {
            await ShowAlertAsync(
                L("migration.messages.openMigrationUnavailableTitle"),
                L("migration.messages.openMigrationUnavailableMessage"));
            return;
        }

        if (TryGetOwnerWindow() is not { } owner)
            return;

        LoadSelectedFileContent();

        var dialogViewModel = new MigrationFileDialogViewModel(
            SelectedMigrationFileName,
            EditorContent,
            SaveEditorContent);

        var dialog = new MigrationFileDialog(dialogViewModel)
        {
            Icon = owner.Icon
        };

        await dialog.ShowDialog(owner);
    }

    private void SaveEditorContent(string content)
    {
        if (SelectedMigration is null || string.IsNullOrWhiteSpace(SelectedMigration.FilePath))
            return;

        _service.SaveMigrationFile(SelectedMigration.FilePath, content);
        SelectedMigration.Content = content;
        EditorContent = content;
        IsEditorDirty = false;
        AppendLog(Format("migration.messages.fileSavedLog", Path.GetFileName(SelectedMigration.FilePath)));
        StatusMessage = L("migration.messages.fileSaved");
    }

    private bool IsLastMigration(MigrationEntry migration)
    {
        if (Migrations.Count == 0)
            return false;

        var last = Migrations.OrderBy(item => item.TimestampId).Last();
        return last.FullName == migration.FullName;
    }

    private async Task RunOperationAsync(string description, Func<CancellationToken, Task<ProcessResult>> operation)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        IsBusy = true;
        StatusMessage = string.Format(L("migration.messages.operationRunning"), description);
        AppendLog(string.Empty);
        AppendLog($"> {description}");
        AppendLog(new string('-', 60));

        try
        {
            var result = await operation(_cts.Token);
            if (!string.IsNullOrWhiteSpace(result.Output))
                AppendLog(result.Output.TrimEnd());

            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                var error = result.Error.TrimEnd();
                AppendLog(error.StartsWith("[", StringComparison.Ordinal)
                    ? error
                    : $"[StandardError] {error}");
            }

            AppendLog(result.Success ? L("migration.messages.operationCompletedLog") : L("migration.messages.operationFailedLog"));
            StatusMessage = result.Success && ActiveProfile is not null
                ? string.Format(L("migration.messages.operationSucceeded"), ActiveProfile.EffectiveProfileName)
                : L("migration.messages.operationFailed");
        }
        catch (Exception ex)
        {
            AppendLog($"[UnhandledException] {ex}");
            StatusMessage = L("migration.messages.operationFailed");
        }
        finally
        {
            IsBusy = false;
            RaiseDerivedProperties();
        }
    }

    private void AppendLog(string message)
    {
        OutputLog += message + Environment.NewLine;
    }

    private IReadOnlyList<DesignerOptionItem> BuildDbTypeOptions()
    {
        return
        [
            new DesignerOptionItem
            {
                Label = "SQL Server",
                Value = DbType.SqlServer,
                Description = L("migration.dbType.sqlServerDescription")
            },
            new DesignerOptionItem
            {
                Label = "PostgreSQL",
                Value = DbType.PostgreSQL,
                Description = L("migration.dbType.postgreSqlDescription")
            },
            new DesignerOptionItem
            {
                Label = "MySQL",
                Value = DbType.MySQL,
                Description = L("migration.dbType.mySqlDescription")
            }
        ];
    }

    private static MigrationProfileSettings ToSettingsProfile(DbConnectionProfile profile)
    {
        return new MigrationProfileSettings
        {
            Id = profile.Id,
            ProfileName = profile.ProfileName,
            DbType = profile.DbType,
            ContextName = profile.ContextName,
            ConnectionString = profile.ConnectionString,
            UseWorkspace = profile.UseWorkspace
        };
    }

    private async Task<bool> ShowConfirmAsync(string title, string message, string confirmText, bool isDanger = false, string? note = null)
    {
        if (_dialogService is null || TryGetOwnerWindow() is not { } owner)
            return false;

        var result = await _dialogService.ShowConfirmAsync(owner, new DesignerConfirmDialogViewModel
        {
            Title = title,
            Message = message,
            ConfirmText = confirmText,
            CancelText = _localizationService.GetString("dialog.cancel"),
            IsDanger = isDanger,
            Icon = isDanger ? DesignerDialogIcon.Warning : DesignerDialogIcon.Question,
            Note = note
        });

        return result.IsConfirmed;
    }

    private async Task<DesignerDialogResult<string>> ShowPromptAsync(string title, string message, string confirmText, string placeholder = "", string? note = null)
    {
        if (_dialogService is null || TryGetOwnerWindow() is not { } owner)
            return DesignerDialogResult<string>.Cancelled();

        return await _dialogService.ShowPromptAsync(owner, new DesignerPromptDialogViewModel
        {
            Title = title,
            Message = message,
            ConfirmText = confirmText,
            Placeholder = placeholder,
            Note = note,
            Icon = DesignerDialogIcon.Info
        });
    }

    private async Task ShowAlertAsync(string title, string message)
    {
        if (_dialogService is null || TryGetOwnerWindow() is not { } owner)
            return;

        await _dialogService.ShowConfirmAsync(owner, new DesignerConfirmDialogViewModel
        {
            Title = title,
            Message = message,
            ConfirmText = L("migration.messages.gotIt"),
            CancelText = string.Empty,
            Icon = DesignerDialogIcon.Info
        });
    }

    private async Task<bool> ShowProfileEditorSheetAsync(string title, string description, string confirmText)
    {
        if (EditingProfile is null || _dialogService is null || TryGetOwnerWindow() is not { } owner)
            return false;

        var result = await _dialogService.ShowSheetAsync(owner, new DesignerSheetViewModel
        {
            Title = title,
            Description = description,
            ConfirmText = confirmText,
            CancelText = _localizationService.GetString("dialog.cancel"),
            Icon = DesignerDialogIcon.Info,
            Content = new EditProfileSheetView { DataContext = this },
            BaseFontSize = 13,
            DialogWidth = 860,
            DialogHeight = 0,
            LockSize = true,
            HideSystemDecorations = true
        });

        return result.IsConfirmed;
    }

    private async Task BrowseProjectPathAsync()
    {
        if (_dialogService is null || TryGetOwnerWindow() is not { } owner)
            return;

        var file = await _dialogService.PickFileAsync(owner, L("migration.messages.selectDomainProjectFile"), [new FilePickerFileType("C# Project")
        {
            Patterns = ["*.csproj"],
            AppleUniformTypeIdentifiers = ["public.xml"],
            MimeTypes = ["text/xml", "application/xml"]
        }]);

        if (!string.IsNullOrWhiteSpace(file))
            EditingProjectPath = file;
    }

    private void LocalizationServiceOnLanguageChanged(object? sender, EventArgs e)
    {
        foreach (var profile in DbProfiles)
            profile.NotifyLocalizationChanged();

        foreach (var migration in Migrations)
            migration.NotifyLocalizationChanged();

        if (string.IsNullOrWhiteSpace(StatusMessage)
            || string.Equals(StatusMessage, L("migration.messages.ready"), StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = L("migration.messages.ready");
        }

        RaiseDerivedProperties();
    }

    private string L(string key) => _localizationService.GetString(key);

    private string Format(string key, params object[] args) => string.Format(L(key), args);

    private static Window? TryGetOwnerWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;

        return null;
    }

    private static DbConnectionProfile ToRuntimeProfile(MigrationProfileSettings profile)
    {
        return new DbConnectionProfile
        {
            Id = string.IsNullOrWhiteSpace(profile.Id) ? Guid.NewGuid().ToString("N") : profile.Id,
            ProfileName = profile.ProfileName,
            DbType = profile.DbType,
            ContextName = profile.ContextName,
            ConnectionString = profile.ConnectionString,
            UseWorkspace = profile.UseWorkspace
        };
    }
}
