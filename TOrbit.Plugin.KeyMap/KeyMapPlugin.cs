using Avalonia.Controls;
using TOrbit.Core.Services;
using TOrbit.Designer.Abstractions;
using TOrbit.Designer.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Models;
using TOrbit.Plugin.KeyMap.ViewModels;
using TOrbit.Plugin.KeyMap.Views;

namespace TOrbit.Plugin.KeyMap;

public sealed class KeyMapPlugin : BasePlugin, IVisualPlugin, IPluginHeaderActionsProvider, IPluginPageHeaderProvider, IPluginDisplayInfoProvider
{
    private readonly IKeyMapService _keyMapService;
    private readonly ILocalizationService _localizationService;
    private readonly PluginDescriptor _descriptor;

    private KeyMapView? _view;
    private KeyMapViewModel? _viewModel;

    public KeyMapPlugin(IKeyMapService keyMapService, ILocalizationService localizationService)
    {
        _keyMapService = keyMapService;
        _localizationService = localizationService;
        _descriptor = CreateDescriptor<KeyMapPlugin>(
            KeyMapPluginMetadata.Instance.Id,
            _localizationService.GetString("plugins.keymap.name"),
            KeyMapPluginMetadata.Instance.Version,
            _localizationService.GetString("plugins.keymap.description"),
            KeyMapPluginMetadata.Instance.Author,
            KeyMapPluginMetadata.Instance.Icon,
            KeyMapPluginMetadata.Instance.Tags);
    }

    public override PluginDescriptor Descriptor => _descriptor;

    public event EventHandler? HeaderChanged;
    public event EventHandler? DisplayInfoChanged;

    public string DisplayName => _localizationService.GetString("plugins.keymap.name");

    public string DisplayDescription => _localizationService.GetString("plugins.keymap.description");

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
            new PluginHeaderAction(_localizationService.GetString("keymap.resetAll"), _viewModel.ResetAllCommand),
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
            Context = string.IsNullOrWhiteSpace(_viewModel.SearchText)
                ? _localizationService.GetString("keymap.header.context")
                : string.Format(_localizationService.GetString("keymap.header.contextFiltered"), _viewModel.SearchText),
            Metrics =
            [
                new PluginPageHeaderMetric
                {
                    Label = _localizationService.GetString("keymap.header.bindings"),
                    Value = _viewModel.TotalBindingCount.ToString(),
                    Tone = PluginPageHeaderTone.Neutral
                },
                new PluginPageHeaderMetric
                {
                    Label = _localizationService.GetString("keymap.header.plugins"),
                    Value = _viewModel.GroupCount.ToString(),
                    Tone = PluginPageHeaderTone.Accent
                },
                new PluginPageHeaderMetric
                {
                    Label = _localizationService.GetString("keymap.header.modified"),
                    Value = _viewModel.ModifiedBindingCount.ToString(),
                    Tone = _viewModel.ModifiedBindingCount > 0 ? PluginPageHeaderTone.Warning : PluginPageHeaderTone.Success
                },
                new PluginPageHeaderMetric
                {
                    Label = _localizationService.GetString("keymap.header.disabled"),
                    Value = _viewModel.DisabledBindingCount.ToString(),
                    Tone = _viewModel.DisabledBindingCount > 0 ? PluginPageHeaderTone.Warning : PluginPageHeaderTone.Success
                }
            ],
            Badges =
            [
                new PluginPageHeaderBadge
                {
                    Text = _viewModel.SelectedBinding is null ? _localizationService.GetString("keymap.header.noSelection") : _viewModel.SelectedBinding.Name,
                    Tone = _viewModel.SelectedBinding is null ? PluginPageHeaderTone.Neutral : PluginPageHeaderTone.Accent
                },
                new PluginPageHeaderBadge
                {
                    Text = _viewModel.SelectedBinding?.PluginName ?? _localizationService.GetString("keymap.header.allPlugins"),
                    Tone = PluginPageHeaderTone.Neutral
                }
            ]
        };
    }

    private void EnsureView()
    {
        if (_viewModel is not null)
            return;

        _localizationService.LanguageChanged += LocalizationServiceOnLanguageChanged;
        _viewModel = new KeyMapViewModel(_keyMapService);
        _viewModel.HeaderSummaryChanged += ViewModelOnHeaderSummaryChanged;
        _view = new KeyMapView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        _localizationService.LanguageChanged -= LocalizationServiceOnLanguageChanged;
        if (_viewModel is not null)
        {
            _viewModel.HeaderSummaryChanged -= ViewModelOnHeaderSummaryChanged;
            _viewModel.Dispose();
        }

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
