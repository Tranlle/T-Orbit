using System.Windows;
using System.Windows.Controls;
using Tranbok.Tools.Plugins.Migration.ViewModels;

namespace Tranbok.Tools.Plugins.Migration.Views;

public partial class MigrationView : UserControl
{
    private MigrationViewModel? _vm;

    public MigrationView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
            _vm.PropertyChanged -= Vm_PropertyChanged;

        _vm = e.NewValue as MigrationViewModel;

        if (_vm is not null)
            _vm.PropertyChanged += Vm_PropertyChanged;
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Auto-scroll output log to bottom whenever new content arrives
        if (e.PropertyName == nameof(MigrationViewModel.OutputLog))
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
            {
                LogTextBox.ScrollToEnd();
            });
        }
    }
}
