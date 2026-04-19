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

public sealed class PromptorPlugin : BasePlugin, IVisualPlugin, IPluginVariableReceiver, IPluginHeaderActionsProvider
{
    private PromptorView? _view;
    private PromptorViewModel? _viewModel;
    private IReadOnlyList<PluginHeaderAction>? _headerActions;
    private PromptorVariables _variables = new();

    public override PluginDescriptor Descriptor { get; } =
        CreateDescriptor<PromptorPlugin>(PromptorPluginMetadata.Instance);

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
        return _headerActions ?? [];
    }

    private void EnsureView()
    {
        if (_viewModel is null)
        {
            var dialogService = Context.GetTool<IDesignerDialogService>();
            _viewModel = new PromptorViewModel(dialogService, _variables);
            _headerActions =
            [
                new PluginHeaderAction("Log", _viewModel.ShowLogCommand),
                new PluginHeaderAction("Clear", _viewModel.ClearAllCommand),
                new PluginHeaderAction("Copy", _viewModel.CopyCommand),
                new PluginHeaderAction("Optimize", _viewModel.OptimizeCommand, IsPrimary: true)
            ];
        }

        _view ??= new PromptorView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        if (_viewModel is not null)
            _viewModel.Dispose();

        _view = null;
        _viewModel = null;
        _headerActions = null;
        return ValueTask.CompletedTask;
    }
}
