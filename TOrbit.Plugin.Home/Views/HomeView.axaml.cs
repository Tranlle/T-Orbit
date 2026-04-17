using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TOrbit.Plugin.Home.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
