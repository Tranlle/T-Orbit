using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Tranbok.Tools.Designer.Controls;

public partial class DesignerCard : ContentControl
{
    public DesignerCard()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
