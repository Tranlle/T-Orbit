using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Tranbok.Tools.Designer.Abstractions;
using Tranbok.Tools.Designer.Services;
using Tranbok.Tools.Plugin.Core;
using Tranbok.Tools.Plugin.Core.Base;
using Tranbok.Tools.Plugin.Migration.ViewModels;
using Tranbok.Tools.Plugin.Migration.Views;

namespace Tranbok.Tools.Plugin.Migration;

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
            var dialogService = Context.Services?.GetService<IDesignerDialogService>();
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
