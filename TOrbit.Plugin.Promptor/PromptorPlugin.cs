using Avalonia.Controls;
using TOrbit.Designer.Abstractions;
using TOrbit.Designer.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Models;
using TOrbit.Plugin.Core.Tools;
using TOrbit.Plugin.Promptor.Models;
using TOrbit.Plugin.Promptor.ViewModels;
using TOrbit.Plugin.Promptor.Views;

namespace TOrbit.Plugin.Promptor;

public sealed class PromptorPlugin : BasePlugin, IVisualPlugin, IPluginVariableReceiver, IPluginHeaderActionsProvider, IPluginDisplayInfoProvider
{
    private PromptorView? _view;
    private PromptorViewModel? _viewModel;
    private PromptorVariables _variables = new();
    private readonly ILocalizationService _localizationService;
    private readonly PluginDescriptor _descriptor;

    public PromptorPlugin(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        _descriptor = CreateDescriptor<PromptorPlugin>(
            PromptorPluginMetadata.Instance.Id,
            _localizationService.GetString("plugins.promptor.name"),
            PromptorPluginMetadata.Instance.Version,
            _localizationService.GetString("plugins.promptor.description"),
            PromptorPluginMetadata.Instance.Author,
            PromptorPluginMetadata.Instance.Icon,
            PromptorPluginMetadata.Instance.Tags,
            variableDefinitions:
            [
                new PluginVariableDefinition(
                    Key: "PROMPTOR_PROVIDER",
                    DefaultValue: "openai",
                    DisplayName: _localizationService.GetString("plugins.promptor.variables.provider.name"),
                    Description: _localizationService.GetString("plugins.promptor.variables.provider.description"),
                    IsRequired: true,
                    AllowedValues: ["openai", "qwen", "kimi", "ollama"]),
                new PluginVariableDefinition(
                    Key: "PROMPTOR_API_ENDPOINT",
                    DefaultValue: string.Empty,
                    DisplayName: _localizationService.GetString("plugins.promptor.variables.endpoint.name"),
                    Description: _localizationService.GetString("plugins.promptor.variables.endpoint.description")),
                new PluginVariableDefinition(
                    Key: "PROMPTOR_API_KEY",
                    DefaultValue: string.Empty,
                    DisplayName: _localizationService.GetString("plugins.promptor.variables.apiKey.name"),
                    Description: _localizationService.GetString("plugins.promptor.variables.apiKey.description"),
                    IsEncrypted: true),
                new PluginVariableDefinition(
                    Key: "PROMPTOR_MODEL_NAME",
                    DefaultValue: "gpt-4o",
                    DisplayName: _localizationService.GetString("plugins.promptor.variables.model.name"),
                    Description: _localizationService.GetString("plugins.promptor.variables.model.description"),
                    IsRequired: true),
                new PluginVariableDefinition(
                    Key: "PROMPTOR_MAX_TOKENS",
                    DefaultValue: "2048",
                    DisplayName: _localizationService.GetString("plugins.promptor.variables.maxTokens.name"),
                    Description: _localizationService.GetString("plugins.promptor.variables.maxTokens.description"),
                    IsRequired: true,
                    ValidationPattern: @"^\d+$",
                    ValidationMessage: _localizationService.GetString("plugins.promptor.variables.maxTokens.validation")),
                new PluginVariableDefinition(
                    Key: "PROMPTOR_TEMPERATURE",
                    DefaultValue: "1.0",
                    DisplayName: _localizationService.GetString("plugins.promptor.variables.temperature.name"),
                    Description: _localizationService.GetString("plugins.promptor.variables.temperature.description"),
                    ValidationPattern: @"^(?:0(?:\.\d+)?|1(?:\.\d+)?|2(?:\.0+)?)$",
                    ValidationMessage: _localizationService.GetString("plugins.promptor.variables.temperature.validation"))
            ],
            capabilities: PromptorPluginMetadata.Instance.Capabilities);
    }

    public override PluginDescriptor Descriptor => _descriptor;

    public event EventHandler? DisplayInfoChanged;

    public string DisplayName => _localizationService.GetString("plugins.promptor.name");

    public string DisplayDescription => _localizationService.GetString("plugins.promptor.description");

    public void OnVariablesInjected(IReadOnlyDictionary<string, string> rawValues)
    {
        var tool = Context.GetTool<IPluginEncryptionTool>();
        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var definitions = Descriptor.VariableDefinitions ?? [];

        foreach (var definition in definitions)
        {
            rawValues.TryGetValue(definition.Key, out var rawValue);

            var value = definition.IsEncrypted && !string.IsNullOrEmpty(rawValue) && tool is not null
                ? tool.TryDecrypt(rawValue) ?? definition.DefaultValue
                : string.IsNullOrEmpty(rawValue) ? definition.DefaultValue : rawValue;

            resolved[definition.Key] = value;
        }

        _variables = PluginVariableBinder.Bind<PromptorVariables>(resolved);
        _viewModel?.UpdateVariables(_variables);
    }

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
            new PluginHeaderAction(_localizationService.GetString("promptor.log"), _viewModel.ShowLogCommand),
            new PluginHeaderAction(_localizationService.GetString("promptor.clear"), _viewModel.ClearAllCommand),
            new PluginHeaderAction(_localizationService.GetString("promptor.copy"), _viewModel.CopyCommand),
            new PluginHeaderAction(_localizationService.GetString("promptor.optimize"), _viewModel.OptimizeCommand, IsPrimary: true)
        ];
    }

    private void EnsureView()
    {
        if (_viewModel is null)
        {
            _localizationService.LanguageChanged += LocalizationServiceOnLanguageChanged;
            var dialogService = Context.GetTool<IDesignerDialogService>();
            _viewModel = new PromptorViewModel(dialogService, _variables, _localizationService);
        }

        _view ??= new PromptorView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        _localizationService.LanguageChanged -= LocalizationServiceOnLanguageChanged;
        _viewModel?.Dispose();
        _view = null;
        _viewModel = null;
        return ValueTask.CompletedTask;
    }

    private void LocalizationServiceOnLanguageChanged(object? sender, EventArgs e)
        => DisplayInfoChanged?.Invoke(this, EventArgs.Empty);
}
