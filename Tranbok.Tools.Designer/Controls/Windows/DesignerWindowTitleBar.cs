using Avalonia;
using Avalonia.Controls;

namespace Tranbok.Tools.Designer.Controls.Windows;

public partial class DesignerWindowTitleBar : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<DesignerWindowTitleBar, string?>(nameof(Title));

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<DesignerWindowTitleBar, string?>(nameof(Subtitle));

    public static readonly StyledProperty<bool> ShowWindowControlsProperty =
        AvaloniaProperty.Register<DesignerWindowTitleBar, bool>(nameof(ShowWindowControls), true);

    public static readonly StyledProperty<double> TitleBarHeightProperty =
        AvaloniaProperty.Register<DesignerWindowTitleBar, double>(nameof(TitleBarHeight), 40d);

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public bool ShowWindowControls
    {
        get => GetValue(ShowWindowControlsProperty);
        set => SetValue(ShowWindowControlsProperty, value);
    }

    public double TitleBarHeight
    {
        get => GetValue(TitleBarHeightProperty);
        set => SetValue(TitleBarHeightProperty, value);
    }

}
