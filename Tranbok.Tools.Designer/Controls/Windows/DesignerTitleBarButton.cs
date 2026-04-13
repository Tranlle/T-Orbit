using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Tranbok.Tools.Designer.Controls.Windows;

public class DesignerTitleBarButton : Button
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<DesignerTitleBarButton, string?>(nameof(Text));

    public static readonly StyledProperty<bool> IsDangerProperty =
        AvaloniaProperty.Register<DesignerTitleBarButton, bool>(nameof(IsDanger));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsDanger
    {
        get => GetValue(IsDangerProperty);
        set => SetValue(IsDangerProperty, value);
    }
}
