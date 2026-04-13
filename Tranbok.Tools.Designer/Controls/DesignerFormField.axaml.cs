using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Tranbok.Tools.Designer.Controls;

public partial class DesignerFormField : ContentControl
{
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly Avalonia.StyledProperty<string?> LabelProperty =
        Avalonia.AvaloniaProperty.Register<DesignerFormField, string?>(nameof(Label));

    public string? Hint
    {
        get => GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    public static readonly Avalonia.StyledProperty<string?> HintProperty =
        Avalonia.AvaloniaProperty.Register<DesignerFormField, string?>(nameof(Hint));

    public bool HasLabel => !string.IsNullOrWhiteSpace(Label);
    public bool HasHint => !string.IsNullOrWhiteSpace(Hint);

    public DesignerFormField()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
