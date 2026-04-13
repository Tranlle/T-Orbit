using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Tranbok.Tools.Designer.Controls;

public partial class DesignerPage : ContentControl
{
    public DesignerPage()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
