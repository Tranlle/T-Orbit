using Avalonia;
using Avalonia.Controls;

namespace Tranbok.Tools.Designer.Controls.Inputs;

public class DesignerDatePicker : DesignerInputControl
{
    public static readonly StyledProperty<DateTimeOffset?> SelectedDateProperty =
        AvaloniaProperty.Register<DesignerDatePicker, DateTimeOffset?>(nameof(SelectedDate), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<DesignerDatePicker, string?>(nameof(Watermark));

    public DateTimeOffset? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }
}
