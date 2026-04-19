using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Windows.Input;

namespace TOrbit.Designer.Controls;

public partial class PathPickerField : UserControl
{
    public static readonly StyledProperty<string?> PathProperty =
        AvaloniaProperty.Register<PathPickerField, string?>(nameof(Path));

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<PathPickerField, string?>(nameof(PlaceholderText));

    public static readonly StyledProperty<string> BrowseTextProperty =
        AvaloniaProperty.Register<PathPickerField, string>(nameof(BrowseText), "Browse");

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<PathPickerField, bool>(nameof(IsReadOnly), true);

    public static readonly StyledProperty<ICommand?> BrowseCommandProperty =
        AvaloniaProperty.Register<PathPickerField, ICommand?>(nameof(BrowseCommand));

    public PathPickerField()
    {
        InitializeComponent();
    }

    public string? Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public string BrowseText
    {
        get => GetValue(BrowseTextProperty);
        set => SetValue(BrowseTextProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public ICommand? BrowseCommand
    {
        get => GetValue(BrowseCommandProperty);
        set => SetValue(BrowseCommandProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
