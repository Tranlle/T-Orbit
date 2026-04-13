using Avalonia.Controls;
using Avalonia.Interactivity;
using Tranbok.Tools.Designer.Models;
using Tranbok.Tools.Designer.ViewModels;

namespace Tranbok.Tools.Designer.Controls.Dialogs;

public partial class DesignerConfirmDialogWindow : Window
{
    public DesignerConfirmDialogWindow()
    {
        InitializeComponent();
    }

    public DesignerConfirmDialogWindow(DesignerConfirmDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void ConfirmClicked(object? sender, RoutedEventArgs e) => Close(DesignerDialogResult<bool>.Confirmed(true));
    private void CancelClicked(object? sender, RoutedEventArgs e) => Close(DesignerDialogResult<bool>.Cancelled(false));
}
