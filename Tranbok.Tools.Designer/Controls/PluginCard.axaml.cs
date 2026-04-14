using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Tranbok.Tools.Designer.Controls;

public partial class PluginCard : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<PluginCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<PluginCard, string?>(nameof(Description));

    public static readonly StyledProperty<object?> HeaderContentProperty =
        AvaloniaProperty.Register<PluginCard, object?>(nameof(HeaderContent));

    public static readonly StyledProperty<object?> DetailsContentProperty =
        AvaloniaProperty.Register<PluginCard, object?>(nameof(DetailsContent));

    public static readonly StyledProperty<object?> FooterContentProperty =
        AvaloniaProperty.Register<PluginCard, object?>(nameof(FooterContent));

    public static readonly StyledProperty<double> CardWidthProperty =
        AvaloniaProperty.Register<PluginCard, double>(nameof(CardWidth), 400d);

    public static readonly StyledProperty<Thickness> CardMarginProperty =
        AvaloniaProperty.Register<PluginCard, Thickness>(nameof(CardMargin), new Thickness(0, 0, 20, 20));

    public PluginCard()
    {
        InitializeComponent();
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public object? HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    public object? DetailsContent
    {
        get => GetValue(DetailsContentProperty);
        set => SetValue(DetailsContentProperty, value);
    }

    public object? FooterContent
    {
        get => GetValue(FooterContentProperty);
        set => SetValue(FooterContentProperty, value);
    }

    public double CardWidth
    {
        get => GetValue(CardWidthProperty);
        set => SetValue(CardWidthProperty, value);
    }

    public Thickness CardMargin
    {
        get => GetValue(CardMarginProperty);
        set => SetValue(CardMarginProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
