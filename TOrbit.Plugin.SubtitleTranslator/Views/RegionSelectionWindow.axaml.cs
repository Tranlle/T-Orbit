using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using TOrbit.Plugin.SubtitleTranslator.Models;

namespace TOrbit.Plugin.SubtitleTranslator.Views;

public partial class RegionSelectionWindow : Window
{
    private Border? _selectionBorder;
    private Point? _dragStart;

    public RegionSelectionWindow()
    {
        InitializeComponent();

        _selectionBorder = this.FindControl<Border>("SelectionBorder");

        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsRightButtonPressed)
        {
            Close(null);
            e.Handled = true;
            return;
        }

        if (!point.Properties.IsLeftButtonPressed)
            return;

        _dragStart = point.Position;
        UpdateSelection(point.Position);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragStart is null)
            return;

        UpdateSelection(e.GetPosition(this));
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragStart is null)
            return;

        var end = e.GetPosition(this);
        var region = BuildRegion(_dragStart.Value, end);
        _dragStart = null;

        if (region is null)
        {
            if (_selectionBorder is not null)
                _selectionBorder.IsVisible = false;
            return;
        }

        Close(region);
        e.Handled = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        Close(null);
        e.Handled = true;
    }

    private void UpdateSelection(Point end)
    {
        if (_selectionBorder is null || _dragStart is null)
            return;

        var rect = NormalizeRect(_dragStart.Value, end);
        _selectionBorder.IsVisible = true;
        _selectionBorder.Width = rect.Width;
        _selectionBorder.Height = rect.Height;
        Canvas.SetLeft(_selectionBorder, rect.X);
        Canvas.SetTop(_selectionBorder, rect.Y);
    }

    private SubtitleRegion? BuildRegion(Point start, Point end)
    {
        var rect = NormalizeRect(start, end);
        if (rect.Width < 24 || rect.Height < 24)
            return null;

        return new SubtitleRegion(
            X: Position.X + (int)Math.Round(rect.X),
            Y: Position.Y + (int)Math.Round(rect.Y),
            Width: (int)Math.Round(rect.Width),
            Height: (int)Math.Round(rect.Height));
    }

    private static Rect NormalizeRect(Point start, Point end)
    {
        var x = Math.Min(start.X, end.X);
        var y = Math.Min(start.Y, end.Y);
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);
        return new Rect(x, y, width, height);
    }
}
