using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Tranbok.Tools.Designer.Controls;

public partial class SplitWorkspace : UserControl
{
    public static readonly StyledProperty<GridLength> LeftWidthProperty =
        AvaloniaProperty.Register<SplitWorkspace, GridLength>(nameof(LeftWidth), new GridLength(320));

    public static readonly StyledProperty<double> LeftMinWidthProperty =
        AvaloniaProperty.Register<SplitWorkspace, double>(nameof(LeftMinWidth), 240d);

    public static readonly StyledProperty<double> RightMinWidthProperty =
        AvaloniaProperty.Register<SplitWorkspace, double>(nameof(RightMinWidth), 360d);

    public static readonly StyledProperty<object?> LeftContentProperty =
        AvaloniaProperty.Register<SplitWorkspace, object?>(nameof(LeftContent));

    public static readonly StyledProperty<object?> RightContentProperty =
        AvaloniaProperty.Register<SplitWorkspace, object?>(nameof(RightContent));

    public SplitWorkspace()
    {
        InitializeComponent();
    }

    public GridLength LeftWidth
    {
        get => GetValue(LeftWidthProperty);
        set => SetValue(LeftWidthProperty, value);
    }

    public double LeftMinWidth
    {
        get => GetValue(LeftMinWidthProperty);
        set => SetValue(LeftMinWidthProperty, value);
    }

    public double RightMinWidth
    {
        get => GetValue(RightMinWidthProperty);
        set => SetValue(RightMinWidthProperty, value);
    }

    public object? LeftContent
    {
        get => GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    public object? RightContent
    {
        get => GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
