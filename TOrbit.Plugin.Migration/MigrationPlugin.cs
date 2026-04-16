using Avalonia.Controls;
using TOrbit.Designer.Abstractions;
using TOrbit.Designer.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Migration.ViewModels;
using TOrbit.Plugin.Migration.Views;

namespace TOrbit.Plugin.Migration;

public sealed class MigrationPlugin : BasePlugin, IVisualPlugin
{
    private MigrationView? _view;
    private MigrationViewModel? _viewModel;

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

    private void EnsureView()
    {
        if (_viewModel is null)
        {
            var dialogService = Context.GetTool<IDesignerDialogService>();
            _viewModel = new MigrationViewModel(dialogService);
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
