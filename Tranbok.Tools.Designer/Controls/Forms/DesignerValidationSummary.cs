using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System.Collections.ObjectModel;

namespace Tranbok.Tools.Designer.Controls.Forms;

public class DesignerValidationSummary : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<DesignerValidationSummary, string?>(nameof(Title), "请先处理以下问题");

    public static readonly StyledProperty<ObservableCollection<string>> MessagesProperty =
        AvaloniaProperty.Register<DesignerValidationSummary, ObservableCollection<string>>(nameof(Messages), new ObservableCollection<string>());

    public static readonly StyledProperty<bool> IsCompactProperty =
        AvaloniaProperty.Register<DesignerValidationSummary, bool>(nameof(IsCompact));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public ObservableCollection<string> Messages
    {
        get => GetValue(MessagesProperty);
        set => SetValue(MessagesProperty, value);
    }

    public bool IsCompact
    {
        get => GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    public bool HasMessages => Messages.Count > 0;

    public DesignerValidationSummary()
    {
        Messages.CollectionChanged += (_, _) => InvalidateVisual();
    }
}
