using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TOrbit.Plugin.SubtitleTranslator.Views;

public partial class TranslationSettingsSheetView : UserControl
{
    public TranslationSettingsSheetView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
