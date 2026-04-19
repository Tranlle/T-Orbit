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

public sealed class SettingsPlugin : BasePlugin, IVisualPlugin, IPluginHeaderActionsProvider, IPluginPageHeaderProvider, IPluginDisplayInfoProvider
{
    private readonly IAppShellService _shellService;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly IAppPreferencesService _preferencesService;
    private readonly IPluginCatalogService _pluginCatalog;
    private readonly IPluginVariableService _variableService;
    private readonly IPluginValidationStatusService _validationStatusService;
    private readonly PluginDescriptor _descriptor;

    private SettingsView? _view;
    private SettingsViewModel? _viewModel;

    public SettingsPlugin(
        IAppShellService shellService,
        IThemeService themeService,
        ILocalizationService localizationService,
        IAppPreferencesService preferencesService,
        IPluginCatalogService pluginCatalog,
        IPluginVariableService variableService,
        IPluginValidationStatusService validationStatusService)
    {
        _shellService = shellService;
        _themeService = themeService;
        _localizationService = localizationService;
        _preferencesService = preferencesService;
        _pluginCatalog = pluginCatalog;
        _variableService = variableService;
        _validationStatusService = validationStatusService;
        _descriptor = CreateDescriptor<SettingsPlugin>(
            SettingsPluginMetadata.Instance.Id,
            _localizationService.GetString("plugins.settings.name"),
            SettingsPluginMetadata.Instance.Version,
            _localizationService.GetString("plugins.settings.description"),
            SettingsPluginMetadata.Instance.Author,
            SettingsPluginMetadata.Instance.Icon,
            SettingsPluginMetadata.Instance.Tags);
    }

    public override PluginDescriptor Descriptor => _descriptor;

    public event EventHandler? HeaderChanged;
    public event EventHandler? DisplayInfoChanged;

    public string DisplayName => _localizationService.GetString("plugins.settings.name");

    public string DisplayDescription => _localizationService.GetString("plugins.settings.description");

    public override Control GetMainView()
    {
        EnsureView();
        return _view!;
    }

    public IReadOnlyList<PluginHeaderAction> GetHeaderActions()
    {
        EnsureView();
        if (_viewModel is null)
            return [];

        return
        [
            new PluginHeaderAction(_localizationService.GetString("settings.header.actions.reset"), _viewModel.ResetCommand),
            new PluginHeaderAction(_localizationService.GetString("settings.header.actions.save"), _viewModel.SaveCommand, IsPrimary: true)
        ];
    }

    public PluginPageHeaderModel? GetPageHeader()
    {
        EnsureView();
        if (_viewModel is null)
            return null;

        return new PluginPageHeaderModel
        {
            Context = _viewModel.ShowAdvancedThemeSettings
                ? _localizationService.GetString("settings.header.advancedTheme")
                : _localizationService.GetString("settings.header.basicTheme"),
            Metrics =
            [
                new PluginPageHeaderMetric
                {
                    Label = _localizationService.GetString("settings.header.metrics.plugins"),
                    Value = _viewModel.PluginCount.ToString(),
                    Tone = PluginPageHeaderTone.Neutral
                },
                new PluginPageHeaderMetric
                {
                    Label = _localizationService.GetString("settings.header.metrics.variables"),
                    Value = _viewModel.VariableCount.ToString(),
                    Tone = PluginPageHeaderTone.Accent
                },
                new PluginPageHeaderMetric
                {
                    Label = _localizationService.GetString("settings.header.metrics.covered"),
                    Value = _viewModel.VariablePluginCount.ToString(),
                    Tone = PluginPageHeaderTone.Success
                },
                new PluginPageHeaderMetric
                {
                    Label = _localizationService.GetString("settings.header.metrics.warnings"),
                    Value = _viewModel.ValidationIssuePluginCount.ToString(),
                    Tone = _viewModel.ValidationIssuePluginCount > 0 ? PluginPageHeaderTone.Warning : PluginPageHeaderTone.Success
                }
            ],
            Badges =
            [
                new PluginPageHeaderBadge
                {
                    Text = _viewModel.SelectedPaletteOption?.Label ?? _localizationService.GetString("settings.header.badges.defaultPalette"),
                    Tone = PluginPageHeaderTone.Neutral
                },
                new PluginPageHeaderBadge
                {
                    Text = _viewModel.SelectedFontOption?.Label ?? _localizationService.GetString("settings.header.badges.systemFont"),
                    Tone = PluginPageHeaderTone.Neutral
                },
                new PluginPageHeaderBadge
                {
                    Text = _viewModel.MinimizeToTrayOnClose
                        ? _localizationService.GetString("settings.header.badges.trayClose")
                        : _localizationService.GetString("settings.header.badges.exitClose"),
                    Tone = PluginPageHeaderTone.Accent
                },
                new PluginPageHeaderBadge
                {
                    Text = _viewModel.ShowAdvancedThemeSettings
                        ? _localizationService.GetString("settings.header.badges.advancedTheme")
                        : _localizationService.GetString("settings.header.badges.basicTheme"),
                    Tone = _viewModel.ShowAdvancedThemeSettings ? PluginPageHeaderTone.Warning : PluginPageHeaderTone.Success
                }
            ]
        };
    }

    private void EnsureView()
    {
        if (_viewModel is null)
        {
            _localizationService.LanguageChanged += LocalizationServiceOnLanguageChanged;
            _viewModel = new SettingsViewModel(
                _shellService,
                _themeService,
                _localizationService,
                _preferencesService,
                _pluginCatalog,
                _variableService,
                _validationStatusService);
            _viewModel.HeaderSummaryChanged += ViewModelOnHeaderSummaryChanged;
        }

        _view ??= new SettingsView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        _localizationService.LanguageChanged -= LocalizationServiceOnLanguageChanged;
        if (_viewModel is not null)
            _viewModel.HeaderSummaryChanged -= ViewModelOnHeaderSummaryChanged;

        _view = null;
        _viewModel = null;
        return ValueTask.CompletedTask;
    }

    private void ViewModelOnHeaderSummaryChanged(object? sender, EventArgs e)
        => HeaderChanged?.Invoke(this, EventArgs.Empty);

    private void LocalizationServiceOnLanguageChanged(object? sender, EventArgs e)
    {
        DisplayInfoChanged?.Invoke(this, EventArgs.Empty);
        HeaderChanged?.Invoke(this, EventArgs.Empty);
    }
}
