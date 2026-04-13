using Avalonia.Controls;
using Avalonia.Interactivity;
using Tranbok.Tools.Designer.Models;
using Tranbok.Tools.Designer.ViewModels;

namespace Tranbok.Tools.Designer.Controls.Dialogs;

public partial class DesignerSheetWindow : Window
{
    public DesignerSheetWindow()
    {
        InitializeComponent();
    }

    public DesignerSheetWindow(DesignerSheetViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void ConfirmClicked(object? sender, RoutedEventArgs e) => Close(DesignerDialogResult<bool>.Confirmed(true));

    private void CancelClicked(object? sender, RoutedEventArgs e) => Close(DesignerDialogResult<bool>.Cancelled(false));
}
