using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using TOrbit.Plugin.RuntimeHost.ViewModels;

namespace TOrbit.Plugin.RuntimeHost.Views;

public partial class RuntimeHostView : UserControl
{
    public RuntimeHostView() => AvaloniaXamlLoader.Load(this);

    private void AppCard_OnTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control { DataContext: HostedAppItemViewModel item })
            return;

        if (DataContext is not RuntimeHostViewModel viewModel)
            return;

        viewModel.SelectedApp = item;
        e.Handled = false;
    }
}
