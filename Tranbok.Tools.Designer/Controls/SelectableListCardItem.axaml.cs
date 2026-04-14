using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Tranbok.Tools.Designer.Controls;

public partial class SelectableListCardItem : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<SelectableListCardItem, string?>(nameof(Title));

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<SelectableListCardItem, string?>(nameof(Subtitle));

    public static readonly StyledProperty<string?> MetaProperty =
        AvaloniaProperty.Register<SelectableListCardItem, string?>(nameof(Meta));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<SelectableListCardItem, bool>(nameof(IsSelected));

    public static readonly StyledProperty<object?> BodyContentProperty =
        AvaloniaProperty.Register<SelectableListCardItem, object?>(nameof(BodyContent));

    public static readonly StyledProperty<object?> ActionContentProperty =
        AvaloniaProperty.Register<SelectableListCardItem, object?>(nameof(ActionContent));

    public SelectableListCardItem()
    {
        InitializeComponent();
        UpdateSelectedClass(IsSelected);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string? Meta
    {
        get => GetValue(MetaProperty);
        set => SetValue(MetaProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public object? BodyContent
    {
        get => GetValue(BodyContentProperty);
        set => SetValue(BodyContentProperty, value);
    }

    public object? ActionContent
    {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsSelectedProperty && change.NewValue is bool isSelected)
            UpdateSelectedClass(isSelected);
    }

    private void UpdateSelectedClass(bool isSelected)
    {
        Classes.Set("selected", isSelected);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
