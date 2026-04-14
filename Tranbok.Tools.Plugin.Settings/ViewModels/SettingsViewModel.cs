using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using Tranbok.Tools.Core.Models;
using Tranbok.Tools.Core.Services;
using Tranbok.Tools.Designer.Models;
using Tranbok.Tools.Designer.Services;

namespace Tranbok.Tools.Plugin.Settings.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly IAppPreferencesService _preferencesService;

    [ObservableProperty]
    private string appName = "Tranbok.Tools";

    [ObservableProperty]
    private string theme = "Dark";

    [ObservableProperty]
    private DesignerOptionItem? selectedThemeOption;

    [ObservableProperty]
    private DesignerOptionItem? selectedPaletteOption;

    [ObservableProperty]
    private DesignerOptionItem? selectedAdvancedPaletteOption;

    [ObservableProperty]
    private DesignerOptionItem? selectedFontOption;

    [ObservableProperty]
    private bool showAdvancedThemeSettings;

    [ObservableProperty]
    private string customThemeDirectory = Path.Combine(AppContext.BaseDirectory, "themes");

    [ObservableProperty]
    private string workspaceRoot = AppContext.BaseDirectory;

    [ObservableProperty]
    private bool useWorkspaceForMigrations = true;

    [ObservableProperty]
    private string statusMessage = "设置已就绪";

    public ObservableCollection<DesignerOptionItem> ThemeOptions { get; } =
    [
        new DesignerOptionItem { Key = "Dark", Label = "深色模式", Description = "适合低亮环境" },
        new DesignerOptionItem { Key = "Light", Label = "浅色模式", Description = "适合明亮环境" }
    ];

    public ObservableCollection<DesignerOptionItem> FontOptions { get; } = [];

    public ObservableCollection<DesignerOptionItem> PaletteOptions { get; } = [];
    public ObservableCollection<DesignerOptionItem> AdvancedPaletteOptions { get; } = [];

    public bool IsInterFontWarningVisible => SelectedFontOption?.Key == "inter";
    public string FontWarningMessage => "Inter 在当前 Avalonia 版本下存在已知 TextBox 光标重合风险；若你看到输入框末尾光标压到最后一个字符，建议切回“系统推荐”。";

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand ResetCommand { get; }

    public SettingsViewModel(IAppShellService shellService, IThemeService themeService, IAppPreferencesService preferencesService)
    {
        _themeService = themeService;
        _preferencesService = preferencesService;

        var preferences = _preferencesService.Load();

        InitializeFontOptions();

        appName = shellService.AppName;
        workspaceRoot = shellService.WorkspaceRoot;
        theme = themeService.CurrentTheme == ThemeVariant.Light ? "Light" : "Dark";
        SelectedThemeOption = ThemeOptions.FirstOrDefault(option => option.Key == theme) ?? ThemeOptions.FirstOrDefault();
        SelectedFontOption = FontOptions.FirstOrDefault(option => option.Key == preferences.FontOptionKey)
            ?? FontOptions.FirstOrDefault(option => option.Key == themeService.CurrentFontOptionKey)
            ?? FontOptions.FirstOrDefault();

        foreach (var palette in themeService.GetAvailablePalettes())
        {
            var option = new DesignerOptionItem
            {
                Key = palette.Key,
                Label = palette.Label,
                Description = palette.Description,
                Value = palette
            };

            if (palette.IsBuiltIn)
            {
                PaletteOptions.Add(option);
            }
            else
            {
                AdvancedPaletteOptions.Add(option);
            }
        }

        SelectedPaletteOption = PaletteOptions.FirstOrDefault(option => option.Key == themeService.CurrentPaletteKey) ?? PaletteOptions.FirstOrDefault();
        SelectedAdvancedPaletteOption = AdvancedPaletteOptions.FirstOrDefault(option => option.Key == themeService.CurrentPaletteKey);

        SaveCommand = new RelayCommand(() =>
        {
            Theme = SelectedThemeOption?.Key ?? Theme;
            var paletteKey = ShowAdvancedThemeSettings
                ? SelectedAdvancedPaletteOption?.Key ?? SelectedPaletteOption?.Key ?? _themeService.CurrentPaletteKey
                : SelectedPaletteOption?.Key ?? _themeService.CurrentPaletteKey;
            var fontOptionKey = SelectedFontOption?.Key ?? "system";

            _themeService.SetPalette(paletteKey);
            _themeService.SetTheme(Theme);
            _themeService.SetFontOption(fontOptionKey);
            _preferencesService.Save(new AppPreferences { FontOptionKey = fontOptionKey });

            var fontLabel = SelectedFontOption?.Label ?? "系统推荐";
            StatusMessage = $"设置已保存，当前方案：{(ShowAdvancedThemeSettings ? SelectedAdvancedPaletteOption?.Label : SelectedPaletteOption?.Label) ?? "默认"} · 字体：{fontLabel}";
        });

        ResetCommand = new RelayCommand(() =>
        {
            AppName = shellService.AppName;
            Theme = "Dark";
            SelectedThemeOption = ThemeOptions.FirstOrDefault(option => option.Key == Theme);
            SelectedPaletteOption = PaletteOptions.FirstOrDefault(option => option.Key == "tranbok-dark") ?? PaletteOptions.FirstOrDefault();
            SelectedAdvancedPaletteOption = null;
            SelectedFontOption = FontOptions.FirstOrDefault(option => option.Key == "system") ?? FontOptions.FirstOrDefault();
            ShowAdvancedThemeSettings = false;
            WorkspaceRoot = shellService.WorkspaceRoot;
            UseWorkspaceForMigrations = true;
            _themeService.SetPalette(SelectedPaletteOption?.Key ?? "tranbok-dark");
            _themeService.SetTheme(Theme);
            _themeService.SetFontOption("system");
            _preferencesService.Save(new AppPreferences { FontOptionKey = "system" });
            StatusMessage = "设置已重置";
        });
    }

    partial void OnSelectedFontOptionChanged(DesignerOptionItem? value)
    {
        OnPropertyChanged(nameof(IsInterFontWarningVisible));
        OnPropertyChanged(nameof(FontWarningMessage));
    }

    private void InitializeFontOptions()
    {
        FontOptions.Clear();
        FontOptions.Add(new DesignerOptionItem
        {
            Key = "system",
            Label = "系统推荐（按平台自动选择）",
            Description = "Windows 使用 Segoe UI，macOS 使用系统字体，Linux 使用 Noto Sans / DejaVu Sans 回退链。"
        });
        FontOptions.Add(new DesignerOptionItem
        {
            Key = "inter",
            Label = "Inter",
            Description = "跨平台一致，但在当前 Avalonia 版本下可能出现输入光标与末尾字符重合问题。"
        });

        if (OperatingSystem.IsWindows())
        {
            FontOptions.Add(new DesignerOptionItem { Key = "segoe-ui", Label = "Segoe UI", Description = "Windows 默认界面字体。" });
            FontOptions.Add(new DesignerOptionItem { Key = "microsoft-yahei-ui", Label = "Microsoft YaHei UI", Description = "适合中文界面的 Windows 字体。" });
            FontOptions.Add(new DesignerOptionItem { Key = "arial", Label = "Arial", Description = "经典西文字体。" });
            FontOptions.Add(new DesignerOptionItem { Key = "bahnschrift", Label = "Bahnschrift", Description = "Windows 自带现代无衬线字体。" });
        }
    }
}
