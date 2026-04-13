using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Tranbok.Tools.Designer.Controls.Windows;

public partial class DesignerWindowTitleBar : UserControl
{
    public DesignerWindowTitleBar()
    {
        InitializeComponent();
    }

    private Window? GetHostWindow()
    {
        if (VisualRoot is Window window)
        {
            return window;
        }

        return this.FindAncestorOfType<Window>();
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var window = GetHostWindow();
        if (window is null)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            e.Handled = true;
            return;
        }

        window.BeginMoveDrag(e);
        e.Handled = true;
    }

    private void MinimizeButton_Click(object? sender, RoutedEventArgs e)
    {
        var window = GetHostWindow();
        if (window is not null)
        {
            window.WindowState = WindowState.Minimized;
        }
    }

    private void MaximizeButton_Click(object? sender, RoutedEventArgs e)
    {
        var window = GetHostWindow();
        if (window is not null)
        {
            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        GetHostWindow()?.Close();
    }
}
