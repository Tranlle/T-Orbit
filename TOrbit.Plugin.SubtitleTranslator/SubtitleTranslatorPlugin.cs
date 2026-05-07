using Avalonia.Controls;
using System.Runtime.Versioning;
using TOrbit.Core.Services;
using TOrbit.Designer.Abstractions;
using TOrbit.Designer.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Models;
using TOrbit.Plugin.Core.Tools;
using TOrbit.Plugin.SubtitleTranslator.Models;
using TOrbit.Plugin.SubtitleTranslator.Services;
using TOrbit.Plugin.SubtitleTranslator.ViewModels;
using TOrbit.Plugin.SubtitleTranslator.Views;

namespace TOrbit.Plugin.SubtitleTranslator;

[SupportedOSPlatform("windows")]
public sealed class SubtitleTranslatorPlugin : BasePlugin, IVisualPlugin, IPluginVariableReceiver, IPluginHeaderActionsProvider, IPluginPageHeaderProvider, IPluginDisplayInfoProvider
{
    private readonly ILocalizationService _localizationService;
    private readonly IPluginVariableService _pluginVariableService;
    private readonly IDesignerDialogService _dialogService;
    private readonly PluginDescriptor _descriptor;
    private SubtitleTranslatorVariables _variables = new();

    private SubtitleTranslatorView? _view;
    private SubtitleTranslatorViewModel? _viewModel;

    public SubtitleTranslatorPlugin(
        ILocalizationService localizationService,
        IPluginVariableService pluginVariableService,
        IDesignerDialogService dialogService)
    {
        _localizationService = localizationService;
        _pluginVariableService = pluginVariableService;
        _dialogService = dialogService;
        _descriptor = CreateDescriptor<SubtitleTranslatorPlugin>(
            SubtitleTranslatorPluginMetadata.Instance.Id,
            _localizationService.GetString("plugins.subtitleTranslator.name"),
            SubtitleTranslatorPluginMetadata.Instance.Version,
            _localizationService.GetString("plugins.subtitleTranslator.description"),
            SubtitleTranslatorPluginMetadata.Instance.Author,
            SubtitleTranslatorPluginMetadata.Instance.Icon,
            SubtitleTranslatorPluginMetadata.Instance.Tags,
            variableDefinitions: BuildVariableDefinitions(),
            capabilities: SubtitleTranslatorPluginMetadata.Instance.Capabilities);
    }

    public override PluginDescriptor Descriptor => _descriptor;

    public event EventHandler? HeaderChanged;
    public event EventHandler? DisplayInfoChanged;

    public string DisplayName => Descriptor.Name;

    public string DisplayDescription => Descriptor.Description ?? string.Empty;

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
            new PluginHeaderAction(_localizationService.GetString("subtitle.actions.translationSettings"), _viewModel.ShowTranslationSettingsCommand)
        ];
    }

    public PluginPageHeaderModel? GetPageHeader()
    {
        EnsureView();
        return _viewModel?.CreatePageHeader();
    }

    public void OnVariablesInjected(IReadOnlyDictionary<string, string> rawValues)
    {
        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var definitions = Descriptor.VariableDefinitions ?? [];
        var encryptionTool = Context.GetTool<IPluginEncryptionTool>();

        foreach (var definition in definitions)
        {
            rawValues.TryGetValue(definition.Key, out var rawValue);

            var value = definition.IsEncrypted && !string.IsNullOrWhiteSpace(rawValue) && encryptionTool is not null
                ? encryptionTool.TryDecrypt(rawValue) ?? definition.DefaultValue
                : string.IsNullOrWhiteSpace(rawValue) ? definition.DefaultValue : rawValue;

            resolved[definition.Key] = value;
        }

        _variables = PluginVariableBinder.Bind<SubtitleTranslatorVariables>(resolved);
        _viewModel?.UpdateVariables(_variables);
    }

    private void EnsureView()
    {
        if (_viewModel is null)
        {
            _localizationService.LanguageChanged += LocalizationServiceOnLanguageChanged;
            _viewModel = new SubtitleTranslatorViewModel(
                SubtitleTranslatorPluginMetadata.Instance.Id,
                _variables,
                _localizationService,
                _pluginVariableService,
                _dialogService);
            _viewModel.HeaderSummaryChanged += ViewModelOnHeaderSummaryChanged;
        }

        _view ??= new SubtitleTranslatorView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        if (_viewModel is not null)
        {
            _localizationService.LanguageChanged -= LocalizationServiceOnLanguageChanged;
            _viewModel.HeaderSummaryChanged -= ViewModelOnHeaderSummaryChanged;
            _viewModel.Dispose();
        }

        _view = null;
        _viewModel = null;
        return ValueTask.CompletedTask;
    }

    private void ViewModelOnHeaderSummaryChanged(object? sender, EventArgs e)
    {
        HeaderChanged?.Invoke(this, EventArgs.Empty);
        DisplayInfoChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LocalizationServiceOnLanguageChanged(object? sender, EventArgs e)
    {
        _viewModel?.RefreshLocalization();
        HeaderChanged?.Invoke(this, EventArgs.Empty);
        DisplayInfoChanged?.Invoke(this, EventArgs.Empty);
    }

    private IReadOnlyList<PluginVariableDefinition> BuildVariableDefinitions() =>
    [
        new(
            Key: "SUBTITLE_TARGET_LANGUAGE",
            DefaultValue: "zh-CN",
            DisplayName: _localizationService.GetString("subtitle.variables.targetLanguage.name"),
            Description: _localizationService.GetString("subtitle.variables.targetLanguage.description"),
            IsRequired: true),
        new(
            Key: "SUBTITLE_CAPTURE_INTERVAL_MS",
            DefaultValue: "500",
            DisplayName: _localizationService.GetString("subtitle.variables.captureInterval.name"),
            Description: _localizationService.GetString("subtitle.variables.captureInterval.description"),
            IsRequired: true,
            ValidationPattern: @"^\d+$",
            ValidationMessage: _localizationService.GetString("subtitle.variables.captureInterval.validation")),
        new(
            Key: "SUBTITLE_SHOW_SOURCE_TEXT",
            DefaultValue: "true",
            DisplayName: _localizationService.GetString("subtitle.variables.showSource.name"),
            Description: _localizationService.GetString("subtitle.variables.showSource.description"),
            AllowedValues: ["true", "false"]),
        new(
            Key: "SUBTITLE_SHOW_TRANSLATED_TEXT",
            DefaultValue: "true",
            DisplayName: _localizationService.GetString("subtitle.variables.showTranslation.name"),
            Description: _localizationService.GetString("subtitle.variables.showTranslation.description"),
            AllowedValues: ["true", "false"]),
        new(
            Key: "SUBTITLE_OCR_PADDING",
            DefaultValue: "20",
            DisplayName: _localizationService.GetString("subtitle.variables.ocrPadding.name"),
            Description: _localizationService.GetString("subtitle.variables.ocrPadding.description"),
            ValidationPattern: @"^\d+$",
            ValidationMessage: _localizationService.GetString("subtitle.variables.ocrPadding.validation")),
        new(
            Key: "SUBTITLE_SIMILARITY_THRESHOLD",
            DefaultValue: "0.92",
            DisplayName: _localizationService.GetString("subtitle.variables.similarityThreshold.name"),
            Description: _localizationService.GetString("subtitle.variables.similarityThreshold.description"),
            ValidationPattern: @"^(?:0(?:\.\d+)?|1(?:\.0+)?)$",
            ValidationMessage: _localizationService.GetString("subtitle.variables.similarityThreshold.validation")),
        new(
            Key: "SUBTITLE_MIN_REFRESH_INTERVAL_MS",
            DefaultValue: "600",
            DisplayName: _localizationService.GetString("subtitle.variables.minRefreshInterval.name"),
            Description: _localizationService.GetString("subtitle.variables.minRefreshInterval.description"),
            ValidationPattern: @"^\d+$",
            ValidationMessage: _localizationService.GetString("subtitle.variables.minRefreshInterval.validation")),
        new(
            Key: "SUBTITLE_STABILIZATION_WINDOW_MS",
            DefaultValue: "900",
            DisplayName: _localizationService.GetString("subtitle.variables.stabilizationWindow.name"),
            Description: _localizationService.GetString("subtitle.variables.stabilizationWindow.description"),
            ValidationPattern: @"^\d+$",
            ValidationMessage: _localizationService.GetString("subtitle.variables.stabilizationWindow.validation")),
        new(
            Key: "SUBTITLE_ENABLE_FRAME_CHANGE_DETECTION",
            DefaultValue: "true",
            DisplayName: _localizationService.GetString("subtitle.variables.frameChangeDetection.name"),
            Description: _localizationService.GetString("subtitle.variables.frameChangeDetection.description"),
            AllowedValues: ["true", "false"]),
        new(
            Key: "SUBTITLE_FRAME_CHANGE_THRESHOLD",
            DefaultValue: "0.035",
            DisplayName: _localizationService.GetString("subtitle.variables.frameChangeThreshold.name"),
            Description: _localizationService.GetString("subtitle.variables.frameChangeThreshold.description"),
            ValidationPattern: @"^(?:0(?:\.\d+)?|1(?:\.0+)?)$",
            ValidationMessage: _localizationService.GetString("subtitle.variables.frameChangeThreshold.validation")),
        new(
            Key: "SUBTITLE_TRANSLATION_PROVIDER",
            DefaultValue: TranslationProviders.Disabled,
            DisplayName: _localizationService.GetString("subtitle.variables.translationProvider.name"),
            Description: _localizationService.GetString("subtitle.variables.translationProvider.description"),
            AllowedValues: [TranslationProviders.Disabled, TranslationProviders.Echo, TranslationProviders.OpenAiCompatible]),
        new(
            Key: "SUBTITLE_TRANSLATION_MODEL",
            DefaultValue: string.Empty,
            DisplayName: _localizationService.GetString("subtitle.variables.translationModel.name"),
            Description: _localizationService.GetString("subtitle.variables.translationModel.description")),
        new(
            Key: "SUBTITLE_TRANSLATION_ENDPOINT",
            DefaultValue: string.Empty,
            DisplayName: _localizationService.GetString("subtitle.variables.translationEndpoint.name"),
            Description: _localizationService.GetString("subtitle.variables.translationEndpoint.description")),
        new(
            Key: "SUBTITLE_TRANSLATION_API_KEY",
            DefaultValue: string.Empty,
            DisplayName: _localizationService.GetString("subtitle.variables.translationApiKey.name"),
            Description: _localizationService.GetString("subtitle.variables.translationApiKey.description"),
            IsEncrypted: true)
    ];
}
