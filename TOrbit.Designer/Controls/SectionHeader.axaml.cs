using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TOrbit.Designer.Controls;

public partial class SectionHeader : UserControl
{
    public static readonly StyledProperty<string?> EyebrowProperty =
        AvaloniaProperty.Register<SectionHeader, string?>(nameof(Eyebrow));

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<SectionHeader, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SectionHeader, string?>(nameof(Description));

    public static readonly StyledProperty<object?> ActionContentProperty =
        AvaloniaProperty.Register<SectionHeader, object?>(nameof(ActionContent));

    private TextBlock? _eyebrowText;

    public SectionHeader()
    {
        InitializeComponent();
    }

    public string? Eyebrow
    {
        get => GetValue(EyebrowProperty);
        set => SetValue(EyebrowProperty, value);
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

    public object? ActionContent
    {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == EyebrowProperty)
            UpdateEyebrowVisibility();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _eyebrowText = this.FindControl<TextBlock>("EyebrowText");
        UpdateEyebrowVisibility();
    }

    private void UpdateEyebrowVisibility()
    {
        if (_eyebrowText is not null)
            _eyebrowText.IsVisible = !string.IsNullOrWhiteSpace(Eyebrow);
    }
}
