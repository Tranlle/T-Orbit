using Avalonia.Controls;
using TOrbit.Core.Services;
using TOrbit.Designer.Abstractions;
using TOrbit.Designer.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Models;
using TOrbit.Plugin.Settings.ViewModels;
using TOrbit.Plugin.Settings.Views;

namespace TOrbit.Plugin.Settings;

public sealed class SettingsPlugin : BasePlugin, IVisualPlugin, IPluginHeaderActionsProvider, IPluginPageHeaderProvider
{
    private readonly IAppShellService _shellService;
    private readonly IThemeService _themeService;
    private readonly IAppPreferencesService _preferencesService;
    private readonly IPluginCatalogService _pluginCatalog;
    private readonly IPluginVariableService _variableService;
    private readonly IPluginValidationStatusService _validationStatusService;

    private SettingsView? _view;
    private SettingsViewModel? _viewModel;
    private IReadOnlyList<PluginHeaderAction>? _headerActions;

    public SettingsPlugin(
        IAppShellService shellService,
        IThemeService themeService,
        IAppPreferencesService preferencesService,
        IPluginCatalogService pluginCatalog,
        IPluginVariableService variableService,
        IPluginValidationStatusService validationStatusService)
    {
        _shellService = shellService;
        _themeService = themeService;
        _preferencesService = preferencesService;
        _pluginCatalog = pluginCatalog;
        _variableService = variableService;
        _validationStatusService = validationStatusService;
    }

    public override PluginDescriptor Descriptor { get; } = CreateDescriptor<SettingsPlugin>(SettingsPluginMetadata.Instance);

    public event EventHandler? HeaderChanged;

    public override Control GetMainView()
    {
        EnsureView();
        return _view!;
    }

    public IReadOnlyList<PluginHeaderAction> GetHeaderActions()
    {
        EnsureView();
        return _headerActions ?? [];
    }

    public PluginPageHeaderModel? GetPageHeader()
    {
        EnsureView();
        if (_viewModel is null)
            return null;

        return new PluginPageHeaderModel
        {
            Context = _viewModel.ShowAdvancedThemeSettings
                ? "当前已启用高级主题设置。"
                : "当前使用基础主题配置。",
            Metrics =
            [
                new PluginPageHeaderMetric
                {
                    Label = "插件数",
                    Value = _viewModel.PluginCount.ToString(),
                    Tone = PluginPageHeaderTone.Neutral
                },
                new PluginPageHeaderMetric
                {
                    Label = "变量数",
                    Value = _viewModel.VariableCount.ToString(),
                    Tone = PluginPageHeaderTone.Accent
                },
                new PluginPageHeaderMetric
                {
                    Label = "覆盖插件",
                    Value = _viewModel.VariablePluginCount.ToString(),
                    Tone = PluginPageHeaderTone.Success
                },
                new PluginPageHeaderMetric
                {
                    Label = "告警",
                    Value = _viewModel.ValidationIssuePluginCount.ToString(),
                    Tone = _viewModel.ValidationIssuePluginCount > 0 ? PluginPageHeaderTone.Warning : PluginPageHeaderTone.Success
                }
            ],
            Badges =
            [
                new PluginPageHeaderBadge
                {
                    Text = _viewModel.SelectedPaletteOption?.Label ?? "默认配色",
                    Tone = PluginPageHeaderTone.Neutral
                },
                new PluginPageHeaderBadge
                {
                    Text = _viewModel.SelectedFontOption?.Label ?? "系统字体",
                    Tone = PluginPageHeaderTone.Neutral
                },
                new PluginPageHeaderBadge
                {
                    Text = _viewModel.MinimizeToTrayOnClose ? "关闭到托盘" : "关闭即退出",
                    Tone = PluginPageHeaderTone.Accent
                },
                new PluginPageHeaderBadge
                {
                    Text = _viewModel.ShowAdvancedThemeSettings ? "高级主题" : "基础主题",
                    Tone = _viewModel.ShowAdvancedThemeSettings ? PluginPageHeaderTone.Warning : PluginPageHeaderTone.Success
                }
            ]
        };
    }

    private void EnsureView()
    {
        if (_viewModel is null)
        {
            _viewModel = new SettingsViewModel(
                _shellService,
                _themeService,
                _preferencesService,
                _pluginCatalog,
                _variableService,
                _validationStatusService);
            _viewModel.HeaderSummaryChanged += ViewModelOnHeaderSummaryChanged;
            _headerActions =
            [
                new PluginHeaderAction("重置设置", _viewModel.ResetCommand),
                new PluginHeaderAction("保存设置", _viewModel.SaveCommand, IsPrimary: true)
            ];
        }

        _view ??= new SettingsView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        if (_viewModel is not null)
            _viewModel.HeaderSummaryChanged -= ViewModelOnHeaderSummaryChanged;

        _view = null;
        _viewModel = null;
        _headerActions = null;
        return ValueTask.CompletedTask;
    }

    private void ViewModelOnHeaderSummaryChanged(object? sender, EventArgs e)
        => HeaderChanged?.Invoke(this, EventArgs.Empty);
}
