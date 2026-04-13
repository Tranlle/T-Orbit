using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Tranbok.Tools.Plugin.Migration.Views;

public partial class MigrationView : UserControl
{
    public MigrationView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
