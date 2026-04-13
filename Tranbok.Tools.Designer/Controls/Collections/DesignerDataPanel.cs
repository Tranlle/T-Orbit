using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace Tranbok.Tools.Designer.Controls.Collections;

public class DesignerDataPanel : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<DesignerDataPanel, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<DesignerDataPanel, string?>(nameof(Description));

    public static readonly StyledProperty<object?> HeaderContentProperty =
        AvaloniaProperty.Register<DesignerDataPanel, object?>(nameof(HeaderContent));

    public static readonly StyledProperty<IDataTemplate?> HeaderContentTemplateProperty =
        AvaloniaProperty.Register<DesignerDataPanel, IDataTemplate?>(nameof(HeaderContentTemplate));

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<DesignerDataPanel, object?>(nameof(Content));

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<DesignerDataPanel, IDataTemplate?>(nameof(ContentTemplate));

    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public object? HeaderContent { get => GetValue(HeaderContentProperty); set => SetValue(HeaderContentProperty, value); }
    public IDataTemplate? HeaderContentTemplate { get => GetValue(HeaderContentTemplateProperty); set => SetValue(HeaderContentTemplateProperty, value); }
    public object? Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }
    public IDataTemplate? ContentTemplate { get => GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }
}
