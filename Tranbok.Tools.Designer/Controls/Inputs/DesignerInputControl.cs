using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace Tranbok.Tools.Designer.Controls.Inputs;

public abstract class DesignerInputControl : TemplatedControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<DesignerInputControl, string?>(nameof(Label));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<DesignerInputControl, string?>(nameof(Description));

    public static readonly StyledProperty<string?> ValidationMessageProperty =
        AvaloniaProperty.Register<DesignerInputControl, string?>(nameof(ValidationMessage));

    public static readonly StyledProperty<object?> PrefixProperty =
        AvaloniaProperty.Register<DesignerInputControl, object?>(nameof(Prefix));

    public static readonly StyledProperty<object?> SuffixProperty =
        AvaloniaProperty.Register<DesignerInputControl, object?>(nameof(Suffix));

    public static readonly StyledProperty<bool> ShowClearButtonProperty =
        AvaloniaProperty.Register<DesignerInputControl, bool>(nameof(ShowClearButton));

    public static readonly StyledProperty<IDataTemplate?> PrefixTemplateProperty =
        AvaloniaProperty.Register<DesignerInputControl, IDataTemplate?>(nameof(PrefixTemplate));

    public static readonly StyledProperty<IDataTemplate?> SuffixTemplateProperty =
        AvaloniaProperty.Register<DesignerInputControl, IDataTemplate?>(nameof(SuffixTemplate));

    public static readonly StyledProperty<IBrush?> ValidationBrushProperty =
        AvaloniaProperty.Register<DesignerInputControl, IBrush?>(nameof(ValidationBrush));

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string? ValidationMessage
    {
        get => GetValue(ValidationMessageProperty);
        set => SetValue(ValidationMessageProperty, value);
    }

    public object? Prefix
    {
        get => GetValue(PrefixProperty);
        set => SetValue(PrefixProperty, value);
    }

    public object? Suffix
    {
        get => GetValue(SuffixProperty);
        set => SetValue(SuffixProperty, value);
    }

    public bool ShowClearButton
    {
        get => GetValue(ShowClearButtonProperty);
        set => SetValue(ShowClearButtonProperty, value);
    }

    public IDataTemplate? PrefixTemplate
    {
        get => GetValue(PrefixTemplateProperty);
        set => SetValue(PrefixTemplateProperty, value);
    }

    public IDataTemplate? SuffixTemplate
    {
        get => GetValue(SuffixTemplateProperty);
        set => SetValue(SuffixTemplateProperty, value);
    }

    public IBrush? ValidationBrush
    {
        get => GetValue(ValidationBrushProperty);
        set => SetValue(ValidationBrushProperty, value);
    }
}
