using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Tranbok.Tools.Core.Services;
using Tranbok.Tools.Designer.Abstractions;
using Tranbok.Tools.Designer.Services;
using Tranbok.Tools.Plugin.Core;
using Tranbok.Tools.Plugin.Core.Base;
using Tranbok.Tools.Plugin.Promptor.ViewModels;
using Tranbok.Tools.Plugin.Promptor.Views;

namespace Tranbok.Tools.Plugin.Promptor;

public sealed class PromptorPlugin : BasePlugin, IVisualPlugin
{
    private PromptorView? _view;
    private PromptorViewModel? _viewModel;

    public override PluginDescriptor Descriptor { get; } = CreateDescriptor<PromptorPlugin>(PromptorPluginMetadata.Instance);

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
            var variableService = Context.Services?.GetService<IPluginVariableService>();
            _viewModel = new PromptorViewModel(dialogService, variableService, Descriptor.Id);
        }

        _view ??= new PromptorView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        _view = null;
        _viewModel?.Dispose();
        _viewModel = null;
        return ValueTask.CompletedTask;
    }
}
