using Avalonia;
using Avalonia.Controls.Primitives;

namespace Tranbok.Tools.Designer.Controls.Selectors;

public class DesignerToggleSwitch : TemplatedControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<DesignerToggleSwitch, string?>(nameof(Label));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<DesignerToggleSwitch, string?>(nameof(Description));

    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<DesignerToggleSwitch, bool>(nameof(IsChecked), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public bool IsChecked { get => GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
}
