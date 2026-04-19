using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using System.Windows.Input;

namespace TOrbit.Designer.Controls;

/// <summary>
/// A card-style multi-line text editor.  The header displays a gradient accent pill,
/// title, optional description and an optional action slot.  The editing area fills
/// the remaining card height; content that exceeds the visible area is accessible via
/// the TextBox's built-in vertical ScrollViewer.
///
/// When <see cref="IsReadOnly"/> is <c>true</c> and <see cref="Text"/> changes,
/// the editor automatically scrolls to the bottom — useful for streaming output.
/// </summary>
public partial class CardTextEditor : UserControl
{
    // ── Styled properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<CardTextEditor, string?>(nameof(Title));

    public static readonly StyledProperty<string?> EyebrowProperty =
        AvaloniaProperty.Register<CardTextEditor, string?>(nameof(Eyebrow));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<CardTextEditor, string?>(nameof(Description));

    public static readonly StyledProperty<object?> ActionContentProperty =
        AvaloniaProperty.Register<CardTextEditor, object?>(nameof(ActionContent));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<CardTextEditor, string?>(nameof(Text));

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<CardTextEditor, string?>(nameof(PlaceholderText));

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<CardTextEditor, bool>(nameof(IsReadOnly), defaultValue: false);

    public static readonly StyledProperty<bool> AcceptsReturnProperty =
        AvaloniaProperty.Register<CardTextEditor, bool>(nameof(AcceptsReturn), defaultValue: true);

    public static readonly StyledProperty<bool> AcceptsTabProperty =
        AvaloniaProperty.Register<CardTextEditor, bool>(nameof(AcceptsTab), defaultValue: false);

    public static readonly StyledProperty<ICommand?> SubmitCommandProperty =
        AvaloniaProperty.Register<CardTextEditor, ICommand?>(nameof(SubmitCommand));

    // ── Internal refs ────────────────────────────────────────────────────────

    private Border?    _cardBorder;
    private TextBox?   _editorBox;
    private TextBlock? _eyebrowText;
    private TextBlock? _descriptionText;

    // ── Constructor ──────────────────────────────────────────────────────────

    public CardTextEditor()
    {
        InitializeComponent();
    }

    // ── Public properties ────────────────────────────────────────────────────

    /// <summary>Card header title.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Optional eyebrow shown above the title. Hidden when null or empty.</summary>
    public string? Eyebrow
    {
        get => GetValue(EyebrowProperty);
        set => SetValue(EyebrowProperty, value);
    }

    /// <summary>Optional subtitle shown below the title. Hidden when null/empty.</summary>
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>Content placed in the right side of the header (e.g., a Button).</summary>
    public object? ActionContent
    {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }

    /// <summary>The text content. Supports two-way binding.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Placeholder text shown when the editor is empty.</summary>
    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>
    /// When <c>true</c>, text editing is disabled and the editor auto-scrolls
    /// to the bottom whenever <see cref="Text"/> changes (streaming output mode).
    /// </summary>
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>Whether Enter inserts a newline. Defaults to <c>true</c>.</summary>
    public bool AcceptsReturn
    {
        get => GetValue(AcceptsReturnProperty);
        set => SetValue(AcceptsReturnProperty, value);
    }

    /// <summary>Whether Tab inserts a tab character. Defaults to <c>false</c>.</summary>
    public bool AcceptsTab
    {
        get => GetValue(AcceptsTabProperty);
        set => SetValue(AcceptsTabProperty, value);
    }

    /// <summary>
    /// Command executed when the user presses Ctrl+Enter inside the editor.
    /// Useful for binding a "submit" or "optimize" action.
    /// </summary>
    public ICommand? SubmitCommand
    {
        get => GetValue(SubmitCommandProperty);
        set => SetValue(SubmitCommandProperty, value);
    }

    // ── Property change handling ─────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == EyebrowProperty)
            UpdateEyebrowVisibility();

        if (change.Property == DescriptionProperty)
            UpdateDescriptionVisibility();

        // Auto-scroll to bottom when text updates in read-only mode (streaming).
        if (change.Property == TextProperty && IsReadOnly && _editorBox is { } box)
        {
            Dispatcher.UIThread.Post(
                () => box.CaretIndex = box.Text?.Length ?? 0,
                DispatcherPriority.Render);
        }
    }

    // ── Initialisation ───────────────────────────────────────────────────────

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _cardBorder      = this.FindControl<Border>("CardBorder");
        _editorBox       = this.FindControl<TextBox>("EditorBox");
        _eyebrowText     = this.FindControl<TextBlock>("EyebrowText");
        _descriptionText = this.FindControl<TextBlock>("DescriptionText");

        if (_editorBox is not null)
        {
            _editorBox.GotFocus  += (_, _) => OnEditorGotFocus();
            _editorBox.LostFocus += (_, _) => OnEditorLostFocus();
        }

        // Add border-brush transition in code-behind to avoid Avalonia 12 XAML IL crash
        // with BrushTransition declared directly in XAML.
        if (_cardBorder is not null)
        {
            _cardBorder.Transitions =
            [
                new BrushTransition
                {
                    Property = Border.BorderBrushProperty,
                    Duration  = TimeSpan.FromSeconds(0.15)
                }
            ];
        }

        UpdateEyebrowVisibility();
        UpdateDescriptionVisibility();
    }

    // ── Focus — card border transitions to accent blue ───────────────────────

    private void OnEditorGotFocus()
        => ApplyBorderBrush("TOrbitBorderFocusBrush");

    private void OnEditorLostFocus()
        => ApplyBorderBrush("TOrbitBorderBrush");

    /// <summary>
    /// Looks up <paramref name="resourceKey"/> from application resources and applies it
    /// to the card border.  The <see cref="BrushTransition"/> registered in code-behind
    /// smoothly animates the colour change.
    /// </summary>
    private void ApplyBorderBrush(string resourceKey)
    {
        if (_cardBorder is null) return;

        var theme = Application.Current?.ActualThemeVariant;
        if (Application.Current?.TryGetResource(resourceKey, theme, out var value) == true
            && value is IBrush brush)
        {
            _cardBorder.BorderBrush = brush;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void UpdateDescriptionVisibility()
    {
        if (_descriptionText is not null)
            _descriptionText.IsVisible = !string.IsNullOrWhiteSpace(Description);
    }

    private void UpdateEyebrowVisibility()
    {
        if (_eyebrowText is not null)
            _eyebrowText.IsVisible = !string.IsNullOrWhiteSpace(Eyebrow);
    }
}
