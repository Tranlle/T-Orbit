using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Tranbok.Tools.Designer.Controls;

public partial class DesignerStackPage : ContentControl
{
    public DesignerStackPage()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
