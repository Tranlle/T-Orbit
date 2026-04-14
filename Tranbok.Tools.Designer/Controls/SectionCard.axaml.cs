using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Tranbok.Tools.Designer.Controls;

public partial class SectionCard : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<SectionCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SectionCard, string?>(nameof(Description));

    public static readonly StyledProperty<object?> BodyProperty =
        AvaloniaProperty.Register<SectionCard, object?>(nameof(Body));

    private TextBlock? _descriptionText;

    public SectionCard()
    {
        InitializeComponent();
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public object? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DescriptionProperty)
            UpdateDescriptionVisibility();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _descriptionText = this.FindControl<TextBlock>("DescriptionText");
        UpdateDescriptionVisibility();
    }

    private void UpdateDescriptionVisibility()
    {
        if (_descriptionText is not null)
            _descriptionText.IsVisible = !string.IsNullOrWhiteSpace(Description);
    }
}
