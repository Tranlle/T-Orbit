using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Tranbok.Tools.Plugin.Promptor.Views;

public partial class PromptorView : UserControl
{
    public PromptorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
