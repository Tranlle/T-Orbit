using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Tranbok.Tools.Designer.Controls;

public partial class DesignerStatusBadge : UserControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<DesignerStatusBadge, string?>(nameof(Text));

    public static readonly StyledProperty<string?> VariantProperty =
        AvaloniaProperty.Register<DesignerStatusBadge, string?>(nameof(Variant), "default");

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public bool IsSuccess => string.Equals(Variant, "success", StringComparison.OrdinalIgnoreCase);
    public bool IsWarning => string.Equals(Variant, "warning", StringComparison.OrdinalIgnoreCase);
    public bool IsDanger => string.Equals(Variant, "danger", StringComparison.OrdinalIgnoreCase);

    public DesignerStatusBadge()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
