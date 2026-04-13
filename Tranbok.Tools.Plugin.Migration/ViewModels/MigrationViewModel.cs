using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Tranbok.Tools.Designer.Models;
using Tranbok.Tools.Designer.Services;
using Tranbok.Tools.Designer.ViewModels;
using Tranbok.Tools.Plugin.Migration.Models;
using Tranbok.Tools.Plugin.Migration.Services;

namespace Tranbok.Tools.Plugin.Migration.ViewModels;

public sealed partial class MigrationViewModel : PluginBaseViewModel
{
    private readonly MigrationService _service = new();
    private readonly IDesignerDialogService? _dialogService;
    private CancellationTokenSource? _cts;
    private bool _isInitializing = true;

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
    private string statusMessage = "Ready";

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
    public bool HasSelectedMigration => SelectedMigration is not null;
    public bool CanRollback => SelectedMigration is not null && IsLastMigration(SelectedMigration);

    public string StartupProjectPath => ProjectPath;
    public string CurrentDatabaseTypeDisplay => HasSelectedProfile ? ActiveProfile!.DisplayName : string.Empty;
    public string OutputDirectory => HasProjectPath && HasSelectedProfile ? MigrationService.GetOutputDirectory(ProjectPath, ActiveProfile!) : string.Empty;
    public string SelectedMigrationFileName => HasSelectedMigration ? $"{SelectedMigration!.FullName}.cs" : "— 请从左侧选择迁移 —";
    public string RollbackTooltip => SelectedMigration switch
    {
        null => "请先选择一条迁移",
        _ when !IsLastMigration(SelectedMigration) => "EF Core 只能撤回最后一条迁移（dotnet ef migrations remove）",
        _ when SelectedMigration.Status == MigrationStatus.Applied => "将先执行 database update 回滚 DB，再运行 migrations remove",
        _ => "运行 dotnet ef migrations remove（删除文件并更新 ModelSnapshot）"
    };

    public IReadOnlyList<DbConnectionProfile> OrderedProfiles => DbProfiles
        .Where(profile => ActiveProfile is not null && profile.DbType == ActiveProfile.DbType)
        .OrderByDescending(profile => ReferenceEquals(profile, ActiveProfile))
        .ThenBy(profile => profile.EffectiveProfileName, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    public IReadOnlyList<DesignerOptionItem> DbTypeOptions { get; } =
    [
        new DesignerOptionItem { Label = "SQL Server", Value = DbType.SqlServer, Description = "适用于 SQL Server / LocalDB" },
        new DesignerOptionItem { Label = "PostgreSQL", Value = DbType.PostgreSQL, Description = "适用于 PostgreSQL" },
        new DesignerOptionItem { Label = "MySQL", Value = DbType.MySQL, Description = "适用于 MySQL / MariaDB" }
    ];

    public DesignerOptionItem? SelectedEditingDbTypeOption
    {
        get => DbTypeOptions.FirstOrDefault(x => x.Value is DbType dbType && EditingProfile is not null && dbType == EditingProfile.DbType);
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
    public IRelayCommand SaveEditorCommand { get; }
    public IRelayCommand ExecuteUpdateCommand { get; }
    public IRelayCommand RollbackSelectedCommand { get; }
    public IRelayCommand CancelOperationCommand { get; }
    public IRelayCommand ClearLogCommand { get; }
    public IRelayCommand<DbConnectionProfile> SelectProfileCommand { get; }
    public IRelayCommand<MigrationEntry> SelectMigrationCommand { get; }

    public MigrationViewModel(IDesignerDialogService? dialogService = null)
    {
        _dialogService = dialogService;
        InitializeProfilesFromExistingConfig();

        RefreshCommand = new AsyncRelayCommand(RefreshMigrationsAsync);
        AddProfileCommand = new RelayCommand(AddProfile);
        DeleteProfileCommand = new AsyncRelayCommand(DeleteProfileAsync);
        OpenEditProfileCommand = new AsyncRelayCommand(OpenEditProfileAsync);
        ShowNewPanelCommand = new AsyncRelayCommand(OpenNewMigrationPanelAsync);
        SaveEditorCommand = new RelayCommand(SaveEditor);
        ExecuteUpdateCommand = new AsyncRelayCommand(ExecuteUpdateAsync);
        RollbackSelectedCommand = new AsyncRelayCommand(RollbackSelectedAsync);
        CancelOperationCommand = new RelayCommand(() => _cts?.Cancel());
        ClearLogCommand = new RelayCommand(() => OutputLog = string.Empty);
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
        OnPropertyChanged(nameof(HasSelectedMigration));
        OnPropertyChanged(nameof(CanRollback));
        OnPropertyChanged(nameof(StartupProjectPath));
        OnPropertyChanged(nameof(CurrentDatabaseTypeDisplay));
        OnPropertyChanged(nameof(OutputDirectory));
        OnPropertyChanged(nameof(SelectedMigrationFileName));
        OnPropertyChanged(nameof(RollbackTooltip));
        OnPropertyChanged(nameof(OrderedProfiles));
    }

    private void InitializeProfilesFromExistingConfig()
    {
        var settings = _service.LoadSettings();
        var config = _service.LoadConfig();
        var resolvedProjectPath = !string.IsNullOrWhiteSpace(settings.ProjectPath) ? settings.ProjectPath : config.ProjectPath;

        var profiles = settings.Profiles.Count > 0 ? settings.Profiles : config.Profiles;
        if (!string.IsNullOrWhiteSpace(resolvedProjectPath))
            ProjectPath = resolvedProjectPath;

        if (profiles.Count > 0)
        {
            foreach (var profile in profiles)
                DbProfiles.Add(ToRuntimeProfile(profile));

            var activeProfileId = settings.ActiveProfileId ?? config.ActiveProfileId;
            ActiveProfile = DbProfiles.FirstOrDefault(p => p.Id == activeProfileId) ?? DbProfiles.First();
            ActiveProfile.IsSelected = true;
            return;
        }

        var defaultProfile = CreateDefaultProfile();
        DbProfiles.Add(defaultProfile);
        ActiveProfile = defaultProfile;
        ActiveProfile.IsSelected = true;
    }

    private DbConnectionProfile CreateDefaultProfile()
    {
        return new DbConnectionProfile
        {
            ProfileName = $"配置 {NewProfileSeed++}",
            DbType = DbType.SqlServer,
            UseWorkspace = true
        };
    }

    private void AddProfile()
    {
        var source = ActiveProfile ?? CreateDefaultProfile();
        var profile = new DbConnectionProfile
        {
            ProfileName = $"配置 {NewProfileSeed++}",
            DbType = source.DbType,
            ContextName = source.ContextName,
            ConnectionString = source.ConnectionString,
            UseWorkspace = source.UseWorkspace
        };

        DbProfiles.Add(profile);
        ActiveProfile = profile;
        foreach (var item in DbProfiles)
            item.IsSelected = ReferenceEquals(item, profile);
        AppendLog($"✓ 已新增配置：{profile.EffectiveProfileName}");
        RaiseDerivedProperties();
    }

    private async Task DeleteProfileAsync()
    {
        if (DbProfiles.Count <= 1 || ActiveProfile is null)
            return;

        var profile = ActiveProfile;
        var confirmed = await ShowConfirmAsync(
            "确认删除配置",
            $"将删除配置：{profile.EffectiveProfileName}\n数据库类型：{profile.DisplayName}\n\n此操作不会删除数据库，但会移除当前保存的连接配置。",
            "删除配置",
            isDanger: true,
            note: "建议先确认当前没有未保存的配置修改。"
        );

        if (!confirmed)
            return;

        var index = DbProfiles.IndexOf(profile);
        DbProfiles.Remove(profile);
        ActiveProfile = DbProfiles[Math.Max(0, index - 1)];
        foreach (var item in DbProfiles)
            item.IsSelected = ReferenceEquals(item, ActiveProfile);
        SaveConfig();
        AppendLog($"✓ 已删除配置：{profile.EffectiveProfileName}");
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
            ConnectionString = ActiveProfile.ConnectionString,
            UseWorkspace = ActiveProfile.UseWorkspace
        };

        var sheetResult = await ShowProfileEditorSheetAsync("编辑配置", "修改项目路径、数据库类型与连接信息。", "保存配置");
        if (sheetResult)
            ConfirmEditProfile();
        else
        {
            EditingProfile = null;
            EditingProjectPath = string.Empty;
        }
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

        var settings = new MigrationSettings
        {
            ProjectPath = ProjectPath,
            ActiveProfileId = ActiveProfile.Id,
            Profiles = DbProfiles.Select(ToSettingsProfile).ToList()
        };

        _service.SaveSettings(settings);
        _service.SaveConfig(new MigrationToolConfig
        {
            ProjectPath = ProjectPath,
            ActiveProfileId = ActiveProfile.Id,
            Profiles = DbProfiles.Select(ToSettingsProfile).ToList()
        });

        AppendLog("✓ 配置已保存至 .tranbok-tools.json 和 Migration/setting.json");
        StatusMessage = "配置已保存";
    }

    private void ClearMigrationSelection()
    {
        Migrations.Clear();
        SelectedMigration = null;
        EditorContent = string.Empty;
        RaiseDerivedProperties();
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
            var last = Migrations.OrderBy(m => m.TimestampId).Last();
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
            AppendLog("[MigrationListFallbackWarning] dotnet ef migrations list failed — showing filesystem scan as fallback");
            AppendLog(raw.Error.TrimEnd());
        }

        StatusMessage = $"{Migrations.Count} migration(s)  ·  {ActiveProfile.EffectiveProfileName}";
        AppendLog($"Loaded {Migrations.Count} migration(s) via 'dotnet ef migrations list'");
        RaiseDerivedProperties();
        LoadSelectedFileContent();
    }

    private async Task OpenNewMigrationPanelAsync()
    {
        var error = ValidateMigrationPrerequisites(false);
        if (!string.IsNullOrWhiteSpace(error))
        {
            await ShowAlertAsync("无法新增迁移", error);
            return;
        }

        var result = await ShowPromptAsync(
            "新增迁移",
            "请输入迁移名称，建议使用业务语义明确的 PascalCase 名称。",
            "创建迁移",
            placeholder: "例如 AddUserAuditLog",
            note: "迁移名称至少需要 3 个字符。"
        );

        if (!result.IsConfirmed)
            return;

        NewMigrationName = result.Value?.Trim() ?? string.Empty;
        await AddMigrationAsync();
    }

    private async Task AddMigrationAsync()
    {
        var error = ValidateMigrationPrerequisites(true);
        if (!string.IsNullOrWhiteSpace(error))
        {
            await ShowAlertAsync("无法生成迁移", error);
            return;
        }

        var name = NewMigrationName.Trim();
        await RunOperationAsync($"dotnet ef migrations add {name}", ct => _service.AddMigrationAsync(ProjectPath, StartupProjectPath, name, ActiveProfile!, ct));
        NewMigrationName = string.Empty;
        await RefreshMigrationsAsync();
    }

    private string? ValidateMigrationPrerequisites(bool includeName)
    {
        if (ActiveProfile is null)
            return "请先选择一个配置项。";
        if (string.IsNullOrWhiteSpace(ProjectPath))
            return "请先为当前配置选择 Domain 项目。";
        if (!File.Exists(ProjectPath))
            return $"未找到 Domain 项目文件：{ProjectPath}";
        if (string.IsNullOrWhiteSpace(ActiveProfile.ContextName))
            return "请先填写 DbContext。";

        var designSettingsPath = Path.Combine(Path.GetDirectoryName(ProjectPath)!, "DesignSettings.json");
        if (!File.Exists(designSettingsPath))
            return $"未找到 DesignSettings.json：{designSettingsPath}";

        if (includeName)
        {
            if (string.IsNullOrWhiteSpace(NewMigrationName))
                return "请先填写迁移名称。";
            if (NewMigrationName.Trim().Length < 3)
                return "迁移名称至少需要 3 个字符。";
        }

        return null;
    }

    private async Task ExecuteUpdateAsync()
    {
        if (ActiveProfile is null)
            return;

        var confirmed = await ShowConfirmAsync(
            "确认执行更新",
            $"目标配置：{ActiveProfile.EffectiveProfileName}\n目标数据库：{ActiveProfile.DisplayName}\nDbContext：{ActiveProfile.ContextName}\n\n将执行 database update，应用所有 Pending 迁移。",
            "执行更新",
            note: "请确认连接字符串和项目路径正确。"
        );

        if (!confirmed)
            return;

        await RunOperationAsync("dotnet ef database update", ct => _service.UpdateDatabaseAsync(ProjectPath, StartupProjectPath, ActiveProfile, null, ct));
        await RefreshMigrationsAsync();
    }

    private async Task RollbackSelectedAsync()
    {
        if (SelectedMigration is null || !CanRollback || ActiveProfile is null)
            return;

        var migration = SelectedMigration;
        var steps = migration.Status == MigrationStatus.Applied
            ? "1. dotnet ef database update <prev>（执行 Down 方法）\n2. dotnet ef migrations remove（删除文件 + 更新 ModelSnapshot）"
            : "1. dotnet ef migrations remove（删除文件 + 更新 ModelSnapshot）";

        var confirmed = await ShowConfirmAsync(
            "确认删除迁移",
            $"迁移：{migration.FullName}\n目标数据库：{ActiveProfile.DisplayName}\nDbContext：{ActiveProfile.ContextName}\n\n操作步骤：\n{steps}\n\n此操作不可撤销。",
            "删除迁移",
            isDanger: true,
            note: "仅支持撤回最后一条迁移。"
        );

        if (!confirmed)
            return;

        var allMigrations = Migrations.ToList();
        await RunOperationAsync($"撤回 {migration.MigrationName}", ct => _service.RollbackLastMigrationAsync(ProjectPath, StartupProjectPath, ActiveProfile, migration, allMigrations, ct));
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
            EditorContent = "// 文件路径未找到。请确认输出目录中存在迁移文件。";
            IsEditorDirty = false;
            return;
        }

        var content = _service.ReadMigrationFile(SelectedMigration.FilePath);
        SelectedMigration.Content = content;
        EditorContent = content;
        IsEditorDirty = false;
    }

    private void SaveEditor()
    {
        if (SelectedMigration is null || !IsEditorDirty || string.IsNullOrWhiteSpace(SelectedMigration.FilePath))
            return;

        _service.SaveMigrationFile(SelectedMigration.FilePath, EditorContent);
        SelectedMigration.Content = EditorContent;
        IsEditorDirty = false;
        AppendLog($"✓ Saved {Path.GetFileName(SelectedMigration.FilePath)}");
        StatusMessage = "File saved";
    }

    private bool IsLastMigration(MigrationEntry migration)
    {
        if (Migrations.Count == 0)
            return false;

        var last = Migrations.OrderBy(m => m.TimestampId).Last();
        return last.FullName == migration.FullName;
    }

    private async Task RunOperationAsync(string description, Func<CancellationToken, Task<ProcessResult>> operation)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        IsBusy = true;
        StatusMessage = description + "…";
        AppendLog($"\n▶  {description}");
        AppendLog(new string('─', 60));

        try
        {
            var result = await operation(_cts.Token);
            if (!string.IsNullOrWhiteSpace(result.Output))
                AppendLog(result.Output.TrimEnd());
            if (!string.IsNullOrWhiteSpace(result.Error))
                AppendLog(result.Error.TrimEnd().StartsWith("[") ? result.Error.TrimEnd() : $"[StandardError] {result.Error.TrimEnd()}");

            AppendLog(result.Success ? "✓ Completed" : "✗ Failed");
            StatusMessage = result.Success && ActiveProfile is not null ? $"✓ Done  ·  {ActiveProfile.EffectiveProfileName}" : "✗ Failed  ·  see output log";
        }
        catch (Exception ex)
        {
            AppendLog($"[UnhandledException] {ex}");
            StatusMessage = "Operation failed";
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
            ConfirmText = "知道了",
            CancelText = string.Empty,
            Icon = DesignerDialogIcon.Info
        });
    }

    private async Task<bool> ShowProfileEditorSheetAsync(string title, string description, string confirmText)
    {
        if (EditingProfile is null || _dialogService is null || TryGetOwnerWindow() is not { } owner)
            return false;

        var content = new Views.EditProfileSheetView
        {
            DataContext = this
        };

        var result = await _dialogService.ShowSheetAsync(owner, new DesignerSheetViewModel
        {
            Title = title,
            Description = description,
            ConfirmText = confirmText,
            CancelText = "取消",
            Icon = DesignerDialogIcon.Info,
            Content = content
        });

        return result.IsConfirmed;
    }

    private Window? TryGetOwnerWindow()
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
