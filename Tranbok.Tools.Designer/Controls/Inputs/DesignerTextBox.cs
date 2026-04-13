using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Tranbok.Tools.Designer.Controls.Inputs;

public class DesignerTextBox : DesignerInputControl
{
    private Button? _clearButton;
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<DesignerTextBox, string?>(nameof(Text), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<DesignerTextBox, string?>(nameof(Watermark));

    public static readonly StyledProperty<bool> AcceptsReturnProperty =
        AvaloniaProperty.Register<DesignerTextBox, bool>(nameof(AcceptsReturn));

    public static readonly StyledProperty<int> MaxLinesProperty =
        AvaloniaProperty.Register<DesignerTextBox, int>(nameof(MaxLines), 1);

    public static readonly StyledProperty<string?> InnerLeftTextProperty =
        AvaloniaProperty.Register<DesignerTextBox, string?>(nameof(InnerLeftText));

    public static readonly StyledProperty<string?> InnerRightTextProperty =
        AvaloniaProperty.Register<DesignerTextBox, string?>(nameof(InnerRightText));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public bool AcceptsReturn
    {
        get => GetValue(AcceptsReturnProperty);
        set => SetValue(AcceptsReturnProperty, value);
    }

    public int MaxLines
    {
        get => GetValue(MaxLinesProperty);
        set => SetValue(MaxLinesProperty, value);
    }

    public string? InnerLeftText
    {
        get => GetValue(InnerLeftTextProperty);
        set => SetValue(InnerLeftTextProperty, value);
    }

    public string? InnerRightText
    {
        get => GetValue(InnerRightTextProperty);
        set => SetValue(InnerRightTextProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_clearButton is not null)
        {
            _clearButton.Click -= ClearButton_Click;
        }

        _clearButton = e.NameScope.Find<Button>("PART_ClearButton");
        if (_clearButton is not null)
        {
            _clearButton.Click += ClearButton_Click;
        }
    }

    private void ClearButton_Click(object? sender, RoutedEventArgs e)
    {
        Text = string.Empty;
    }
}
