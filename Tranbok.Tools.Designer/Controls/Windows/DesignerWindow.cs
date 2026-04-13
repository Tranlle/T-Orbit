using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Tranbok.Tools.Designer.Controls.Windows;

public class DesignerWindow : Window
{
    private const double ResizeBorderThickness = 6;

    public static readonly StyledProperty<double> TitleBarHeightProperty =
        AvaloniaProperty.Register<DesignerWindow, double>(nameof(TitleBarHeight), 40d);

    public static readonly StyledProperty<Thickness> WindowContentPaddingProperty =
        AvaloniaProperty.Register<DesignerWindow, Thickness>(nameof(WindowContentPadding), new Thickness(24));

    public static readonly StyledProperty<GridLength> NavigationWidthProperty =
        AvaloniaProperty.Register<DesignerWindow, GridLength>(nameof(NavigationWidth), new GridLength(220));

    public static readonly StyledProperty<bool> ConstrainContentWidthProperty =
        AvaloniaProperty.Register<DesignerWindow, bool>(nameof(ConstrainContentWidth), false);

    public static readonly StyledProperty<double> ContentMaxWidthProperty =
        AvaloniaProperty.Register<DesignerWindow, double>(nameof(ContentMaxWidth), 0d);

    public double TitleBarHeight
    {
        get => GetValue(TitleBarHeightProperty);
        set => SetValue(TitleBarHeightProperty, value);
    }

    public Thickness WindowContentPadding
    {
        get => GetValue(WindowContentPaddingProperty);
        set => SetValue(WindowContentPaddingProperty, value);
    }

    public GridLength NavigationWidth
    {
        get => GetValue(NavigationWidthProperty);
        set => SetValue(NavigationWidthProperty, value);
    }

    public bool ConstrainContentWidth
    {
        get => GetValue(ConstrainContentWidthProperty);
        set => SetValue(ConstrainContentWidthProperty, value);
    }

    public double ContentMaxWidth
    {
        get => GetValue(ContentMaxWidthProperty);
        set => SetValue(ContentMaxWidthProperty, value);
    }

    public DesignerWindow()
    {
        PointerMoved += OnPointerMoved;
        PointerPressed += OnPointerPressed;
        PointerExited += OnPointerExited;
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (WindowState != WindowState.Normal)
        {
            return;
        }

        Cursor = new Cursor(StandardCursorType.Arrow);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (WindowState != WindowState.Normal)
        {
            Cursor = new Cursor(StandardCursorType.Arrow);
            return;
        }

        var edge = GetResizeEdge(e.GetPosition(this));
        Cursor = edge switch
        {
            WindowEdge.North or WindowEdge.South => new Cursor(StandardCursorType.SizeNorthSouth),
            WindowEdge.West or WindowEdge.East => new Cursor(StandardCursorType.SizeWestEast),
            WindowEdge.NorthWest or WindowEdge.SouthEast => new Cursor(StandardCursorType.TopLeftCorner),
            WindowEdge.NorthEast or WindowEdge.SouthWest => new Cursor(StandardCursorType.TopRightCorner),
            _ => new Cursor(StandardCursorType.Arrow)
        };
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (WindowState != WindowState.Normal)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var edge = GetResizeEdge(e.GetPosition(this));
        if (edge is null)
        {
            return;
        }

        BeginResizeDrag(edge.Value, e);
        e.Handled = true;
    }

    private WindowEdge? GetResizeEdge(Point position)
    {
        var left = position.X <= ResizeBorderThickness;
        var right = position.X >= Bounds.Width - ResizeBorderThickness;
        var top = position.Y <= ResizeBorderThickness;
        var bottom = position.Y >= Bounds.Height - ResizeBorderThickness;

        if (left && top)
        {
            return WindowEdge.NorthWest;
        }

        if (right && top)
        {
            return WindowEdge.NorthEast;
        }

        if (left && bottom)
        {
            return WindowEdge.SouthWest;
        }

        if (right && bottom)
        {
            return WindowEdge.SouthEast;
        }

        if (top)
        {
            return WindowEdge.North;
        }

        if (bottom)
        {
            return WindowEdge.South;
        }

        if (left)
        {
            return WindowEdge.West;
        }

        if (right)
        {
            return WindowEdge.East;
        }

        return null;
    }
}
