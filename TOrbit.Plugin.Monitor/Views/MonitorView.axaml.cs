using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using TOrbit.Plugin.Monitor.ViewModels;

namespace TOrbit.Plugin.Monitor.Views;

public partial class MonitorView : UserControl
{
    public MonitorView() => AvaloniaXamlLoader.Load(this);

    private async void MonitorCard_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control { DataContext: PluginMonitorItemViewModel item })
            return;

        if (DataContext is not MonitorViewModel viewModel)
            return;

        await viewModel.ShowDetailsAsync(item);
        e.Handled = true;
    }
}
