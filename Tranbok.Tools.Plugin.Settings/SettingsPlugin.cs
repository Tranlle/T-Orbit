using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Tranbok.Tools.Core.Services;
using Tranbok.Tools.Designer.Abstractions;
using Tranbok.Tools.Designer.Services;
using Tranbok.Tools.Plugin.Core;
using Tranbok.Tools.Plugin.Core.Abstractions;
using Tranbok.Tools.Plugin.Core.Base;
using Tranbok.Tools.Plugin.Settings.ViewModels;
using Tranbok.Tools.Plugin.Settings.Views;

namespace Tranbok.Tools.Plugin.Settings;

public sealed class SettingsPlugin : BasePlugin, IVisualPlugin, IPluginHeaderActionsProvider
{
    private SettingsView? _view;
    private SettingsViewModel? _viewModel;
    private IReadOnlyList<PluginHeaderAction>? _headerActions;

    public override PluginDescriptor Descriptor { get; } = CreateDescriptor<SettingsPlugin>(SettingsPluginMetadata.Instance);

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

    public IReadOnlyList<PluginHeaderAction> GetHeaderActions()
    {
        EnsureView();
        return _headerActions ?? [];
    }

    private void EnsureView()
    {
        if (_viewModel is null)
        {
            var services = Context.Services ?? throw new InvalidOperationException("Plugin services are unavailable.");
            var shellService = services.GetRequiredService<IAppShellService>();
            var themeService = services.GetRequiredService<IThemeService>();
            var preferencesService = services.GetRequiredService<IAppPreferencesService>();
            _viewModel = new SettingsViewModel(shellService, themeService, preferencesService);
            _headerActions =
            [
                new PluginHeaderAction("重置设置", _viewModel.ResetCommand),
                new PluginHeaderAction("保存设置", _viewModel.SaveCommand, IsPrimary: true)
            ];
        }

        _view ??= new SettingsView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        _view = null;
        _viewModel = null;
        _headerActions = null;
        return ValueTask.CompletedTask;
    }
}
