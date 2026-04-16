using Avalonia.Controls;
using TOrbit.Designer.Abstractions;
using TOrbit.Designer.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Models;
using TOrbit.Plugin.Migration.Models;
using TOrbit.Plugin.Migration.Services;
using TOrbit.Plugin.Migration.ViewModels;
using TOrbit.Plugin.Migration.Views;

namespace TOrbit.Plugin.Migration;

public sealed class MigrationPlugin : BasePlugin, IVisualPlugin, IPluginVariableReceiver
{
    private MigrationView? _view;
    private MigrationViewModel? _viewModel;
    private readonly MigrationService _service = new();
    private readonly MigrationConfigurationStore _configurationStore = new();
    private MigrationVariables _variables = new();

    public override PluginDescriptor Descriptor { get; } = CreateDescriptor<MigrationPlugin>(MigrationPluginMetadata.Instance);

    protected override ValueTask OnStartAsync(CancellationToken cancellationToken = default)
    {
        EnsureView();
        return ValueTask.CompletedTask;
    }

    public override Control GetMainView()
    {
        EnsureView();
        return _view!;
    }

    public void OnVariablesInjected(IReadOnlyDictionary<string, string> rawValues)
    {
        _variables = PluginVariableBinder.Bind<MigrationVariables>(rawValues);
        _viewModel?.UpdateVariables(_variables);
    }

    private void EnsureView()
    {
        if (_viewModel is null)
        {
            var dialogService = Context.GetTool<IDesignerDialogService>();
            _viewModel = new MigrationViewModel(_service, _configurationStore, _variables, dialogService);
        }

        _view ??= new MigrationView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        _view = null;
        _viewModel = null;
        return ValueTask.CompletedTask;
    }
}
