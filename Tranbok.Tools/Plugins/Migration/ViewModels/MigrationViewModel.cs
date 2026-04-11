using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Tranbok.Tools.Infrastructure;
using Tranbok.Tools.Plugins.Migration.Models;
using Tranbok.Tools.Plugins.Migration.Services;

namespace Tranbok.Tools.Plugins.Migration.ViewModels;

public sealed class MigrationViewModel : ObservableObject
{
    private readonly MigrationService _service = new();
    private CancellationTokenSource? _cts;
    private string _projectPath = string.Empty;
    private DbConnectionProfile? _activeProfile;
    private int _newProfileSeed = 1;
    private MigrationEntry? _selectedMigration;
    private string _editorContent = string.Empty;
    private bool _isEditorDirty;
    private bool _isBusy;
    private string _statusMessage = "Ready";
    private string _outputLog = string.Empty;
    private bool _showNewMigrationPanel;
    private string _newMigrationName = string.Empty;
    private bool _showEditProfileDialog;
    private DbConnectionProfile? _editingProfile;
    private string _editingProjectPath = string.Empty;
    private bool _showPromptDialog;
    private string _promptTitle = string.Empty;
    private string _promptMessage = string.Empty;
    private string _promptConfirmText = "确定";
    private bool _showPromptCancel;
    private Func<Task>? _promptConfirmAction;

    public ObservableCollection<MigrationEntry> Migrations { get; } = [];
    public ObservableCollection<DbConnectionProfile> DbProfiles { get; } = [];

    public string ProjectPath
    {
        get => _projectPath;
        set
        {
            if (_projectPath == value)
                return;

            SetField(ref _projectPath, value);
            OnPropertyChanged(nameof(HasProjectPath));
            OnPropertyChanged(nameof(OutputDirectory));
        }
    }

    public bool HasProjectPath => !string.IsNullOrWhiteSpace(_projectPath);

    public DbConnectionProfile ActiveProfile
    {
        get => _activeProfile ??= CreateDefaultProfile();
        set
        {
            if (ReferenceEquals(_activeProfile, value))
                return;

            SetField(ref _activeProfile, value);

            foreach (var profile in DbProfiles)
                profile.IsSelected = ReferenceEquals(profile, value);

            OnPropertyChanged(nameof(HasSelectedProfile));
            OnPropertyChanged(nameof(IsSqlServer));
            OnPropertyChanged(nameof(IsPostgreSQL));
            OnPropertyChanged(nameof(IsMySQL));
            OnPropertyChanged(nameof(OutputDirectory));

            ClearMigrationSelection();
        }
    }

    public bool HasSelectedProfile => _activeProfile is not null;

    public bool IsSqlServer
    {
        get => HasSelectedProfile && ActiveProfile.DbType == DbType.SqlServer;
        set { if (value && HasSelectedProfile) ActiveProfile.DbType = DbType.SqlServer; }
    }

    public bool IsPostgreSQL
    {
        get => HasSelectedProfile && ActiveProfile.DbType == DbType.PostgreSQL;
        set { if (value && HasSelectedProfile) ActiveProfile.DbType = DbType.PostgreSQL; }
    }

    public bool IsMySQL
    {
        get => HasSelectedProfile && ActiveProfile.DbType == DbType.MySQL;
        set { if (value && HasSelectedProfile) ActiveProfile.DbType = DbType.MySQL; }
    }

    public string StartupProjectPath => ResolveProviderProjectPath(ActiveProfile);

    public MigrationEntry? SelectedMigration
    {
        get => _selectedMigration;
        set
        {
            if (ReferenceEquals(_selectedMigration, value))
                return;

            SetField(ref _selectedMigration, value);

            foreach (var migration in Migrations)
                migration.IsSelected = ReferenceEquals(migration, value);

            LoadSelectedFileContent();
            OnPropertyChanged(nameof(HasSelectedMigration));
            OnPropertyChanged(nameof(CanRollback));
            OnPropertyChanged(nameof(RollbackTooltip));
            OnPropertyChanged(nameof(SelectedMigrationFileName));
        }
    }

    public bool HasSelectedMigration => _selectedMigration is not null;
    public bool CanRollback => _selectedMigration is not null && IsLastMigration(_selectedMigration);

    public string RollbackTooltip => _selectedMigration switch
    {
        null => "请先选择一条迁移",
        _ when !IsLastMigration(_selectedMigration) => "EF Core 只能撤回最后一条迁移（dotnet ef migrations remove）",
        _ when _selectedMigration.Status == MigrationStatus.Applied => "将先执行 database update 回滚 DB，再运行 migrations remove",
        _ => "运行 dotnet ef migrations remove（删除文件并更新 ModelSnapshot）"
    };

    public string EditorContent
    {
        get => _editorContent;
        set
        {
            SetField(ref _editorContent, value);
            IsEditorDirty = _selectedMigration?.Content != value;
        }
    }

    public bool IsEditorDirty
    {
        get => _isEditorDirty;
        set => SetField(ref _isEditorDirty, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            SetField(ref _isBusy, value);
            OnPropertyChanged(nameof(IsIdle));
        }
    }

    public bool IsIdle => !_isBusy;

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public string OutputLog
    {
        get => _outputLog;
        set => SetField(ref _outputLog, value);
    }

    public bool ShowNewMigrationPanel
    {
        get => _showNewMigrationPanel;
        set => SetField(ref _showNewMigrationPanel, value);
    }

    public string NewMigrationName
    {
        get => _newMigrationName;
        set => SetField(ref _newMigrationName, value);
    }

    public bool ShowEditProfileDialog
    {
        get => _showEditProfileDialog;
        set => SetField(ref _showEditProfileDialog, value);
    }

    public DbConnectionProfile? EditingProfile
    {
        get => _editingProfile;
        set => SetField(ref _editingProfile, value);
    }

    public bool ShowPromptDialog
    {
        get => _showPromptDialog;
        set => SetField(ref _showPromptDialog, value);
    }

    public string PromptTitle
    {
        get => _promptTitle;
        set => SetField(ref _promptTitle, value);
    }

    public string PromptMessage
    {
        get => _promptMessage;
        set => SetField(ref _promptMessage, value);
    }

    public string PromptConfirmText
    {
        get => _promptConfirmText;
        set => SetField(ref _promptConfirmText, value);
    }

    public bool ShowPromptCancel
    {
        get => _showPromptCancel;
        set => SetField(ref _showPromptCancel, value);
    }

    public string EditingProjectPath
    {
        get => _editingProjectPath;
        set => SetField(ref _editingProjectPath, value);
    }

    public string OutputDirectory => HasProjectPath && HasSelectedProfile
        ? MigrationService.GetOutputDirectory(ProjectPath, ActiveProfile)
        : string.Empty;

    public bool HasMigrations => Migrations.Count > 0;
    public int MigrationCount => Migrations.Count;
    public string SelectedMigrationFileName => HasSelectedMigration ? $"{SelectedMigration!.FullName}.cs" : "— 请从左侧选择迁移 —";

    public RelayCommand BrowseProjectCommand { get; }
    public RelayCommand BrowseEditingProjectCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand AddProfileCommand { get; }
    public RelayCommand DeleteProfileCommand { get; }
    public RelayCommand OpenEditProfileCommand { get; }
    public RelayCommand ConfirmEditProfileCommand { get; }
    public RelayCommand CancelEditProfileCommand { get; }
    public RelayCommand ShowNewPanelCommand { get; }
    public RelayCommand CancelNewMigrationCommand { get; }
    public RelayCommand ConfirmAddMigrationCommand { get; }
    public RelayCommand ConfirmPromptCommand { get; }
    public RelayCommand CancelPromptCommand { get; }
    public RelayCommand SaveEditorCommand { get; }
    public RelayCommand ExecuteUpdateCommand { get; }
    public RelayCommand RollbackSelectedCommand { get; }
    public RelayCommand CancelOperationCommand { get; }
    public RelayCommand ClearLogCommand { get; }
    public RelayCommand<DbConnectionProfile> SelectProfileCommand { get; }
    public RelayCommand<MigrationEntry> SelectMigrationCommand { get; }

    public MigrationViewModel()
    {
        InitializeProfilesFromExistingConfig();

        BrowseProjectCommand = new RelayCommand(BrowseProject);
        BrowseEditingProjectCommand = new RelayCommand(BrowseEditingProject, () => ShowEditProfileDialog);
        RefreshCommand = new RelayCommand(() => _ = RefreshMigrationsAsync());
        AddProfileCommand = new RelayCommand(AddProfile);
        DeleteProfileCommand = new RelayCommand(DeleteProfile, () => DbProfiles.Count > 1 && HasSelectedProfile);
        OpenEditProfileCommand = new RelayCommand(OpenEditProfile, () => HasSelectedProfile);
        ConfirmEditProfileCommand = new RelayCommand(ConfirmEditProfile, () => EditingProfile is not null);
        CancelEditProfileCommand = new RelayCommand(() =>
        {
            ShowEditProfileDialog = false;
            EditingProfile = null;
            EditingProjectPath = string.Empty;
        });
        ShowNewPanelCommand = new RelayCommand(OpenNewMigrationPanel);
        CancelNewMigrationCommand = new RelayCommand(() =>
        {
            ShowNewMigrationPanel = false;
            NewMigrationName = string.Empty;
        });
        ConfirmAddMigrationCommand = new RelayCommand(() => _ = AddMigrationAsync());
        ConfirmPromptCommand = new RelayCommand(() => _ = ConfirmPromptAsync());
        CancelPromptCommand = new RelayCommand(ClosePrompt);
        SaveEditorCommand = new RelayCommand(SaveEditor, () => IsEditorDirty && HasSelectedMigration);
        ExecuteUpdateCommand = new RelayCommand(() => _ = ExecuteUpdateAsync(), () => IsIdle && HasProjectPath && HasSelectedProfile);
        RollbackSelectedCommand = new RelayCommand(() => _ = RollbackSelectedAsync(), () => IsIdle && CanRollback);
        CancelOperationCommand = new RelayCommand(() => _cts?.Cancel(), () => IsBusy);
        ClearLogCommand = new RelayCommand(() => OutputLog = string.Empty);
        SelectProfileCommand = new RelayCommand<DbConnectionProfile>(profile => { if (profile is not null) { ActiveProfile = profile; ClearMigrationSelection(); } });
        SelectMigrationCommand = new RelayCommand<MigrationEntry>(migration => SelectedMigration = migration);

        OnPropertyChanged(nameof(HasProjectPath));
        OnPropertyChanged(nameof(HasSelectedProfile));
        OnPropertyChanged(nameof(OutputDirectory));
        OnPropertyChanged(nameof(IsSqlServer));
        OnPropertyChanged(nameof(IsPostgreSQL));
        OnPropertyChanged(nameof(IsMySQL));
    }

    private void InitializeProfilesFromExistingConfig()
    {
        var settings = _service.LoadSettings();
        var config = _service.LoadConfig();

        var projectPath = !string.IsNullOrWhiteSpace(settings.ProjectPath)
            ? settings.ProjectPath
            : config.ProjectPath;

        if (string.IsNullOrWhiteSpace(projectPath))
        {
            var legacyProject = FindLegacyProjectPath();
            if (!string.IsNullOrWhiteSpace(legacyProject))
            {
                settings = _service.LoadSettings(legacyProject);
                config = _service.LoadConfig(legacyProject);
                projectPath = !string.IsNullOrWhiteSpace(settings.ProjectPath)
                    ? settings.ProjectPath
                    : config.ProjectPath;
            }
        }

        var profiles = settings.Profiles.Count > 0
            ? settings.Profiles
            : config.Profiles;

        if (!string.IsNullOrWhiteSpace(projectPath))
            _projectPath = projectPath;

        if (profiles.Count > 0)
        {
            foreach (var profile in profiles)
                DbProfiles.Add(ToRuntimeProfile(profile));

            var activeProfileId = settings.ActiveProfileId ?? config.ActiveProfileId;
            _activeProfile = DbProfiles.FirstOrDefault(p => p.Id == activeProfileId) ?? DbProfiles.First();
            _activeProfile.IsSelected = true;
            return;
        }

        var defaultProfile = CreateDefaultProfile();
        DbProfiles.Add(defaultProfile);
        _activeProfile = defaultProfile;
        _activeProfile.IsSelected = true;
    }

    private static string? FindLegacyProjectPath()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var legacyConfigFiles = Directory.GetFiles(root, ".tranbok-tools.json", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToList();

        foreach (var file in legacyConfigFiles)
        {
            var dir = Path.GetDirectoryName(file);
            if (string.IsNullOrWhiteSpace(dir))
                continue;

            var projectName = Path.GetFileName(dir);
            var csproj = Path.Combine(dir, projectName + ".csproj");
            if (File.Exists(csproj))
                return csproj;
        }

        return null;
    }

    private DbConnectionProfile CreateDefaultProfile()
    {
        return new DbConnectionProfile
        {
            ProfileName = $"配置 {_newProfileSeed++}",
            DbType = DbType.SqlServer,
            UseWorkspace = true
        };
    }

    private void BrowseProject()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择 Domain 项目 (.csproj)",
            Filter = "C# 项目文件 (*.csproj)|*.csproj"
        };

        if (dialog.ShowDialog() != true)
            return;

        LoadProject(dialog.FileName);
    }

    private void BrowseEditingProject()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择 Domain 项目 (.csproj)",
            Filter = "C# 项目文件 (*.csproj)|*.csproj"
        };

        if (dialog.ShowDialog() != true)
            return;

        EditingProjectPath = dialog.FileName;
    }

    private void LoadProject(string projectPath)
    {
        ProjectPath = projectPath;

        var config = _service.LoadConfig(projectPath);
        var settings = _service.LoadSettings(projectPath);
        var profiles = settings.Profiles.Count > 0 ? settings.Profiles : config.Profiles;

        DbProfiles.Clear();
        foreach (var profile in profiles)
            DbProfiles.Add(ToRuntimeProfile(profile));

        if (DbProfiles.Count == 0)
            DbProfiles.Add(CreateDefaultProfile());

        var activeProfileId = settings.ActiveProfileId ?? config.ActiveProfileId;
        var active = DbProfiles.FirstOrDefault(p => p.Id == activeProfileId) ?? DbProfiles.First();
        ActiveProfile = active;
        ClearMigrationSelection();
    }

    private void AddProfile()
    {
        var source = ActiveProfile;
        var profile = new DbConnectionProfile
        {
            ProfileName = $"配置 {_newProfileSeed++}",
            DbType = source.DbType,
            ContextName = source.ContextName,
            ConnectionString = source.ConnectionString,
            UseWorkspace = source.UseWorkspace
        };

        DbProfiles.Add(profile);
        ActiveProfile = profile;
        AppendLog($"✓ 已新增配置：{profile.ProfileName}");
    }

    private void DeleteProfile()
    {
        if (DbProfiles.Count <= 1 || !HasSelectedProfile)
            return;

        var profile = ActiveProfile;
        var index = DbProfiles.IndexOf(profile);
        DbProfiles.Remove(profile);
        ActiveProfile = DbProfiles[Math.Max(0, index - 1)];
        AppendLog($"✓ 已删除配置：{profile.ProfileName}");
    }

    private void SaveConfig()
    {
        if (!HasProjectPath)
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

    private void OpenEditProfile()
    {
        if (!HasSelectedProfile)
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
        ShowEditProfileDialog = true;
    }

    private void ConfirmEditProfile()
    {
        if (EditingProfile is null || !HasSelectedProfile)
            return;

        ProjectPath = EditingProjectPath;
        ActiveProfile.ProfileName = EditingProfile.ProfileName;
        ActiveProfile.DbType = EditingProfile.DbType;
        ActiveProfile.ContextName = EditingProfile.ContextName;
        ActiveProfile.ConnectionString = EditingProfile.ConnectionString;
        ActiveProfile.UseWorkspace = EditingProfile.UseWorkspace;

        ShowEditProfileDialog = false;
        EditingProfile = null;
        SaveConfig();
        OnPropertyChanged(nameof(IsSqlServer));
        OnPropertyChanged(nameof(IsPostgreSQL));
        OnPropertyChanged(nameof(IsMySQL));
        OnPropertyChanged(nameof(OutputDirectory));
        ClearMigrationSelection();
    }

    private void ClearMigrationSelection()
    {
        Migrations.Clear();
        SelectedMigration = null;
        EditorContent = string.Empty;
        OnPropertyChanged(nameof(HasMigrations));
        OnPropertyChanged(nameof(MigrationCount));
    }

    private async Task RefreshMigrationsAsync()
    {
        ClearMigrationSelection();

        if (!HasProjectPath || !HasSelectedProfile)
            return;

        var startupProjectPath = ResolveProviderProjectPath(ActiveProfile);
        var (migrations, raw) = await _service.ListMigrationsAsync(ProjectPath, startupProjectPath, ActiveProfile);

        Application.Current.Dispatcher.Invoke(() =>
        {
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

            OnPropertyChanged(nameof(HasMigrations));
            OnPropertyChanged(nameof(MigrationCount));
            OnPropertyChanged(nameof(CanRollback));
            OnPropertyChanged(nameof(RollbackTooltip));

            if (!raw.Success && !string.IsNullOrWhiteSpace(raw.Error))
            {
                AppendLog("[warn] dotnet ef migrations list failed — showing filesystem scan as fallback");
                AppendLog(raw.Error.TrimEnd());
            }

            StatusMessage = $"{Migrations.Count} migration(s)  ·  {ActiveProfile.ProfileName}";
            AppendLog($"Loaded {Migrations.Count} migration(s) via 'dotnet ef migrations list'");
        });
    }

    private void OpenNewMigrationPanel()
    {
        var error = ValidateMigrationPrerequisites(includeName: false);
        if (!string.IsNullOrWhiteSpace(error))
        {
            ShowAlert("无法新增迁移", error);
            return;
        }

        NewMigrationName = string.Empty;
        ShowNewMigrationPanel = true;
    }

    private async Task AddMigrationAsync()
    {
        var error = ValidateMigrationPrerequisites(includeName: true);
        if (!string.IsNullOrWhiteSpace(error))
        {
            ShowAlert("无法生成迁移", error);
            return;
        }

        var name = NewMigrationName.Trim();
        var startupProjectPath = ResolveProviderProjectPath(ActiveProfile);

        await RunOperationAsync($"dotnet ef migrations add {name}", ct =>
            _service.AddMigrationAsync(ProjectPath, startupProjectPath, name, ActiveProfile, ct));

        ShowNewMigrationPanel = false;
        NewMigrationName = string.Empty;
        await RefreshMigrationsAsync();
    }

    private bool CanAddMigration()
        => string.IsNullOrWhiteSpace(ValidateMigrationPrerequisites(includeName: true));

    private string? ValidateMigrationPrerequisites(bool includeName)
    {
        if (!HasSelectedProfile)
            return "请先选择一个配置项。";

        if (string.IsNullOrWhiteSpace(ProjectPath))
            return "请先为当前配置选择 Domain 项目。";

        if (!File.Exists(ProjectPath))
            return $"未找到 Domain 项目文件：{ProjectPath}";

        if (string.IsNullOrWhiteSpace(ActiveProfile.ContextName))
            return "请先填写 DbContext。";

        var providerProjectPath = ResolveProviderProjectPath(ActiveProfile);
        if (string.IsNullOrWhiteSpace(providerProjectPath) || !File.Exists(providerProjectPath))
            return "未找到当前数据库类型对应的 Provider 项目，请先确认 Domain 项目路径和数据库类型。";

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

    private Task ExecuteUpdateAsync()
    {
        ShowConfirm(
            "确认执行更新",
            $"目标配置：{ActiveProfile.ProfileName}\n目标数据库：{ActiveProfile.DisplayName}\nDbContext：{ActiveProfile.ContextName}\n\n将执行 database update，应用所有 Pending 迁移。",
            "执行更新",
            async () =>
            {
                var startupProjectPath = ResolveProviderProjectPath(ActiveProfile);
                await RunOperationAsync("dotnet ef database update", ct =>
                    _service.UpdateDatabaseAsync(ProjectPath, startupProjectPath, ActiveProfile, null, ct));

                await RefreshMigrationsAsync();
            });

        return Task.CompletedTask;
    }

    private Task RollbackSelectedAsync()
    {
        if (_selectedMigration is null || !CanRollback)
            return Task.CompletedTask;

        var migration = _selectedMigration;
        var steps = migration.Status == MigrationStatus.Applied
            ? "1. dotnet ef database update <prev>（执行 Down 方法）\n2. dotnet ef migrations remove（删除文件 + 更新 ModelSnapshot）"
            : "1. dotnet ef migrations remove（删除文件 + 更新 ModelSnapshot）";

        ShowConfirm(
            "确认删除迁移",
            $"迁移：{migration.FullName}\n目标数据库：{ActiveProfile.DisplayName}\nDbContext：{ActiveProfile.ContextName}\n\n操作步骤：\n{steps}\n\n此操作不可撤销。",
            "删除迁移",
            async () =>
            {
                var startupProjectPath = ResolveProviderProjectPath(ActiveProfile);
                var allMigrations = Migrations.ToList();
                await RunOperationAsync($"撤回 {migration.MigrationName}", ct =>
                    _service.RollbackLastMigrationAsync(ProjectPath, startupProjectPath, ActiveProfile, migration, allMigrations, ct));

                SelectedMigration = null;
                await RefreshMigrationsAsync();
            });

        return Task.CompletedTask;
    }

    private void ShowAlert(string title, string message)
    {
        PromptTitle = title;
        PromptMessage = message;
        PromptConfirmText = "知道了";
        ShowPromptCancel = false;
        _promptConfirmAction = null;
        ShowPromptDialog = true;
    }

    private void ShowConfirm(string title, string message, string confirmText, Func<Task> confirmAction)
    {
        PromptTitle = title;
        PromptMessage = message;
        PromptConfirmText = confirmText;
        ShowPromptCancel = true;
        _promptConfirmAction = confirmAction;
        ShowPromptDialog = true;
    }

    private async Task ConfirmPromptAsync()
    {
        var action = _promptConfirmAction;
        ClosePrompt();

        if (action is not null)
            await action();
    }

    private void ClosePrompt()
    {
        ShowPromptDialog = false;
        _promptConfirmAction = null;
        PromptTitle = string.Empty;
        PromptMessage = string.Empty;
        PromptConfirmText = "确定";
        ShowPromptCancel = false;
    }

    private void LoadSelectedFileContent()
    {
        if (_selectedMigration is null)
        {
            EditorContent = string.Empty;
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedMigration.FilePath))
        {
            _editorContent = "// 文件路径未找到。请确认输出目录中存在迁移文件。";
            OnPropertyChanged(nameof(EditorContent));
            _isEditorDirty = false;
            OnPropertyChanged(nameof(IsEditorDirty));
            return;
        }

        var content = _service.ReadMigrationFile(_selectedMigration.FilePath);
        _selectedMigration.Content = content;
        _editorContent = content;
        OnPropertyChanged(nameof(EditorContent));
        _isEditorDirty = false;
        OnPropertyChanged(nameof(IsEditorDirty));
    }

    private void SaveEditor()
    {
        if (_selectedMigration is null || !_isEditorDirty || string.IsNullOrWhiteSpace(_selectedMigration.FilePath))
            return;

        _service.SaveMigrationFile(_selectedMigration.FilePath, _editorContent);
        _selectedMigration.Content = _editorContent;
        _isEditorDirty = false;
        OnPropertyChanged(nameof(IsEditorDirty));
        AppendLog($"✓ Saved {Path.GetFileName(_selectedMigration.FilePath)}");
        StatusMessage = "File saved";
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

    private bool IsLastMigration(MigrationEntry migration)
    {
        if (Migrations.Count == 0)
            return false;

        var last = Migrations.OrderBy(m => m.TimestampId).Last();
        return last.FullName == migration.FullName;
    }

    private string ResolveProviderProjectPath(DbConnectionProfile profile)
    {
        if (!HasProjectPath)
            return string.Empty;

        var domainProjectPath = Path.GetFullPath(ProjectPath);
        var domainDir = Path.GetDirectoryName(domainProjectPath);
        if (string.IsNullOrWhiteSpace(domainDir))
            return string.Empty;

        var tranbokRoot = Directory.GetParent(domainDir)?.FullName;
        if (string.IsNullOrWhiteSpace(tranbokRoot))
            return string.Empty;

        var providerFolder = profile.DbType switch
        {
            DbType.SqlServer => "Tranbok.Infrastructure.SqlServer",
            DbType.PostgreSQL => "Tranbok.Infrastructure.PostgreSQL",
            DbType.MySQL => "Tranbok.Infrastructure.MySQL",
            _ => string.Empty
        };

        return string.IsNullOrWhiteSpace(providerFolder)
            ? string.Empty
            : Path.Combine(tranbokRoot, "Tranbok.Infrastructure", providerFolder, providerFolder + ".csproj");
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
                AppendLog($"[stderr]  {result.Error.TrimEnd()}");

            AppendLog(result.Success ? "✓ Completed" : "✗ Failed");
            StatusMessage = result.Success ? $"✓ Done  ·  {ActiveProfile.ProfileName}" : "✗ Failed  ·  see output log";
        }
        catch (Exception ex)
        {
            AppendLog($"[exception]  {ex.Message}");
            StatusMessage = "Operation failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AppendLog(string message)
        => OutputLog += message + Environment.NewLine;
}