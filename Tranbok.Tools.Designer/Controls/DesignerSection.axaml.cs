using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Tranbok.Tools.Designer.Controls;

public partial class DesignerSection : ContentControl
{
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly Avalonia.StyledProperty<string?> TitleProperty =
        Avalonia.AvaloniaProperty.Register<DesignerSection, string?>(nameof(Title));

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public static readonly Avalonia.StyledProperty<string?> DescriptionProperty =
        Avalonia.AvaloniaProperty.Register<DesignerSection, string?>(nameof(Description));

    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public DesignerSection()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
