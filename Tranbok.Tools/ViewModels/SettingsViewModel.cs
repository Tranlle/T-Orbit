using Tranbok.Tools.Infrastructure;

namespace Tranbok.Tools.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private string _appName = "Tranbok Tools";
    private string _theme = "Dark";
    private string _workspaceRoot = AppDomain.CurrentDomain.BaseDirectory;
    private bool _useWorkspaceForMigrations = true;
    private string _statusMessage = "设置已就绪";

    public string AppName
    {
        get => _appName;
        set => SetField(ref _appName, value);
    }

    public string Theme
    {
        get => _theme;
        set => SetField(ref _theme, value);
    }

    public string WorkspaceRoot
    {
        get => _workspaceRoot;
        set => SetField(ref _workspaceRoot, value);
    }

    public bool UseWorkspaceForMigrations
    {
        get => _useWorkspaceForMigrations;
        set => SetField(ref _useWorkspaceForMigrations, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand ResetCommand { get; }

    public SettingsViewModel()
    {
        SaveCommand = new RelayCommand(() => StatusMessage = "设置已保存（当前为界面版，占位存储）");
        ResetCommand = new RelayCommand(() =>
        {
            AppName = "Tranbok Tools";
            Theme = "Dark";
            WorkspaceRoot = AppDomain.CurrentDomain.BaseDirectory;
            UseWorkspaceForMigrations = true;
            StatusMessage = "设置已重置";
        });
    }
}
