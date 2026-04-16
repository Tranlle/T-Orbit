using Avalonia.Controls;
using TOrbit.Plugin.Migration.ViewModels;

namespace TOrbit.Plugin.Migration.Views;

public partial class MigrationFileDialog : Window
{
    public MigrationFileDialog(MigrationFileDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
