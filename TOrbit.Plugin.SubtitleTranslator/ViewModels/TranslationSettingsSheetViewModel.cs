using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TOrbit.Core.Models;
using TOrbit.Core.Services;
using TOrbit.Designer.Models;
using TOrbit.Designer.Services;
using TOrbit.Plugin.SubtitleTranslator.Models;
using TOrbit.Plugin.SubtitleTranslator.Services;

namespace TOrbit.Plugin.SubtitleTranslator.ViewModels;

public sealed partial class TranslationSettingsSheetViewModel : ObservableObject
{
    private readonly string _pluginId;
    private readonly IPluginVariableService _pluginVariableService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private DesignerOptionItem? selectedProvider;

    [ObservableProperty]
    private string targetLanguage = string.Empty;

    [ObservableProperty]
    private string translationModel = string.Empty;

    [ObservableProperty]
    private string translationEndpoint = string.Empty;

    [ObservableProperty]
    private string translationApiKey = string.Empty;

    [ObservableProperty]
    private bool showTranslatedText = true;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public TranslationSettingsSheetViewModel(
        string pluginId,
        SubtitleTranslatorVariables variables,
        IPluginVariableService pluginVariableService,
        ILocalizationService localizationService)
    {
        _pluginId = pluginId;
        _pluginVariableService = pluginVariableService;
        _localizationService = localizationService;

        ProviderOptions =
        [
            new DesignerOptionItem
            {
                Key = TranslationProviders.Disabled,
                Label = L("subtitle.settings.providers.disabled.label"),
                Description = L("subtitle.settings.providers.disabled.description")
            },
            new DesignerOptionItem
            {
                Key = TranslationProviders.Echo,
                Label = L("subtitle.settings.providers.echo.label"),
                Description = L("subtitle.settings.providers.echo.description")
            },
            new DesignerOptionItem
            {
                Key = TranslationProviders.OpenAiCompatible,
                Label = L("subtitle.settings.providers.openaiCompatible.label"),
                Description = L("subtitle.settings.providers.openaiCompatible.description")
            }
        ];

        var provider = string.IsNullOrWhiteSpace(variables.TranslationProvider)
            ? TranslationProviders.Disabled
            : variables.TranslationProvider;

        SelectedProvider = ProviderOptions.FirstOrDefault(item => string.Equals(item.Key, provider, StringComparison.OrdinalIgnoreCase))
            ?? ProviderOptions[0];
        TargetLanguage = string.IsNullOrWhiteSpace(variables.TargetLanguage) ? "zh-CN" : variables.TargetLanguage;
        TranslationModel = variables.TranslationModel;
        TranslationEndpoint = variables.TranslationEndpoint;
        TranslationApiKey = variables.TranslationApiKey;
        ShowTranslatedText = variables.ShowTranslatedText;
        StatusMessage = L("subtitle.settings.hint");

        SaveCommand = new RelayCommand(Save);
    }

    public IReadOnlyList<DesignerOptionItem> ProviderOptions { get; }

    public bool IsTranslationEnabled => !string.Equals(SelectedProvider?.Key, TranslationProviders.Disabled, StringComparison.OrdinalIgnoreCase);

    public bool IsOpenAiCompatibleProvider => string.Equals(SelectedProvider?.Key, TranslationProviders.OpenAiCompatible, StringComparison.OrdinalIgnoreCase);

    public IRelayCommand SaveCommand { get; }

    partial void OnSelectedProviderChanged(DesignerOptionItem? value)
    {
        OnPropertyChanged(nameof(IsTranslationEnabled));
        OnPropertyChanged(nameof(IsOpenAiCompatibleProvider));
        StatusMessage = string.Equals(value?.Key, TranslationProviders.Disabled, StringComparison.OrdinalIgnoreCase)
            ? L("subtitle.settings.disabledHint")
            : L("subtitle.settings.enabledHint");
    }

    private void Save()
    {
        var targetLanguage = TargetLanguage.Trim();
        var provider = SelectedProvider?.Key ?? TranslationProviders.Disabled;

        if (string.IsNullOrWhiteSpace(targetLanguage))
        {
            StatusMessage = L("subtitle.settings.validation.targetLanguage");
            return;
        }

        if (string.Equals(provider, TranslationProviders.OpenAiCompatible, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(TranslationEndpoint))
            {
                StatusMessage = L("subtitle.settings.validation.endpoint");
                return;
            }

            if (string.IsNullOrWhiteSpace(TranslationModel))
            {
                StatusMessage = L("subtitle.settings.validation.model");
                return;
            }
        }

        var store = _pluginVariableService.Load();
        Upsert(store, "SUBTITLE_TRANSLATION_PROVIDER", provider, false);
        Upsert(store, "SUBTITLE_TARGET_LANGUAGE", targetLanguage, false);
        Upsert(store, "SUBTITLE_SHOW_TRANSLATED_TEXT", ShowTranslatedText ? "true" : "false", false);
        Upsert(store, "SUBTITLE_TRANSLATION_MODEL", TranslationModel.Trim(), false);
        Upsert(store, "SUBTITLE_TRANSLATION_ENDPOINT", TranslationEndpoint.Trim(), false);
        Upsert(store, "SUBTITLE_TRANSLATION_API_KEY", TranslationApiKey.Trim(), true);

        _pluginVariableService.Save(store);
        _pluginVariableService.InjectAll();
        StatusMessage = L("subtitle.settings.saved");
    }

    private void Upsert(PluginVariableStore store, string key, string value, bool isEncrypted)
    {
        var entry = store.Entries.FirstOrDefault(item =>
            string.Equals(item.PluginId, _pluginId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            store.Entries.Add(new PluginVariableEntry
            {
                PluginId = _pluginId,
                Key = key,
                Value = value,
                IsEncrypted = isEncrypted
            });
            return;
        }

        entry.Value = value;
        entry.IsEncrypted = isEncrypted;
    }

    private string L(string key) => _localizationService.GetString(key);
}
