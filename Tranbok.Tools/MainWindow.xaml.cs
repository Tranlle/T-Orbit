using System.Windows;
using Tranbok.Tools.Infrastructure;
using Tranbok.Tools.Plugins.Migration;
using Tranbok.Tools.ViewModels;

namespace Tranbok.Tools;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var pluginManager = new PluginManager();
        pluginManager.Register(new MigrationPlugin());
        // Future plugins registered here

        DataContext = new MainViewModel(pluginManager);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();
}
