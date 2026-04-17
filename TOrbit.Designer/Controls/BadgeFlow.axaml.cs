using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml;

namespace TOrbit.Designer.Controls;

public partial class BadgeFlow : UserControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<BadgeFlow, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<BadgeFlow, IDataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<BadgeFlow, double>(nameof(ItemSpacing), 8d);

    public static readonly StyledProperty<double> LineSpacingProperty =
        AvaloniaProperty.Register<BadgeFlow, double>(nameof(LineSpacing), 8d);

    public BadgeFlow()
    {
        InitializeComponent();
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public double ItemSpacing
    {
        get => GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    public double LineSpacing
    {
        get => GetValue(LineSpacingProperty);
        set => SetValue(LineSpacingProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
