using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TOrbit.Plugin.SubtitleTranslator.Views;

public partial class SubtitleTranslatorView : UserControl
{
    public SubtitleTranslatorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
