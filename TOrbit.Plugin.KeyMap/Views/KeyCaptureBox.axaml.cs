using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using TOrbit.Designer.Services;

namespace TOrbit.Plugin.KeyMap.Views;

public partial class KeyCaptureBox : UserControl
{
    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<KeyCaptureBox, string>(nameof(Value), string.Empty);

    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private bool _isCapturing;

    private Border? _container;
    private TextBlock? _displayText;
    private TextBlock? _hintText;

    public KeyCaptureBox()
    {
        AvaloniaXamlLoader.Load(this);

        _container = this.FindControl<Border>("CaptureContainer");
        _displayText = this.FindControl<TextBlock>("DisplayText");
        _hintText = this.FindControl<TextBlock>("HintText");

        ValueProperty.Changed.AddClassHandler<KeyCaptureBox>((s, _) => s.UpdateDisplay());
        UpdateDisplay();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AddHandler(KeyDownEvent, OnCaptureKeyDown, handledEventsToo: false);
        AddHandler(LostFocusEvent, OnLostFocus);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        RemoveHandler(KeyDownEvent, OnCaptureKeyDown);
        RemoveHandler(LostFocusEvent, OnLostFocus);
        base.OnDetachedFromVisualTree(e);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_isCapturing)
            BeginCapture();

        Focus();
        e.Handled = true;
    }

    private void BeginCapture()
    {
        _isCapturing = true;

        if (_displayText is not null)
            _displayText.Text = L("keymap.capture.pressShortcut");

        if (_hintText is not null)
            _hintText.IsVisible = false;

        if (_container is not null)
        {
            _container.BorderBrush = Application.Current?.FindResource("TOrbitAccentBrush") as Avalonia.Media.IBrush
                ?? _container.BorderBrush;
        }
    }

    private void EndCapture()
    {
        _isCapturing = false;
        UpdateDisplay();

        if (_container is not null)
            _container.ClearValue(Border.BorderBrushProperty);
    }

    private void OnCaptureKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_isCapturing)
            return;

        if (e.Key == Key.Escape)
        {
            EndCapture();
            e.Handled = true;
            return;
        }

        if (e.Key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt
            or Key.LWin or Key.RWin)
            return;

        Value = FormatKey(e.Key, e.KeyModifiers);
        EndCapture();
        e.Handled = true;
    }

    private void OnLostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isCapturing)
            EndCapture();
    }

    private void UpdateDisplay()
    {
        if (_displayText is null)
            return;

        _displayText.Text = string.IsNullOrWhiteSpace(Value) ? L("keymap.capture.unset") : Value;

        if (_hintText is not null)
            _hintText.IsVisible = true;
    }

    public static string FormatKey(Key key, KeyModifiers modifiers)
    {
        var parts = new List<string>();

        if (modifiers.HasFlag(KeyModifiers.Control)) parts.Add(L("keymap.keys.ctrl"));
        if (modifiers.HasFlag(KeyModifiers.Alt)) parts.Add(L("keymap.keys.alt"));
        if (modifiers.HasFlag(KeyModifiers.Shift)) parts.Add(L("keymap.keys.shift"));
        if (modifiers.HasFlag(KeyModifiers.Meta)) parts.Add(L("keymap.keys.meta"));

        var keyName = key switch
        {
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemSemicolon => ";",
            Key.OemQuotes => "'",
            Key.OemOpenBrackets => "[",
            Key.Oem6 => "]",
            Key.OemBackslash => "\\",
            Key.OemMinus => "-",
            Key.OemPlus => "=",
            Key.Space => L("keymap.keys.space"),
            Key.Return => L("keymap.keys.enter"),
            Key.Back => L("keymap.keys.backspace"),
            Key.Tab => L("keymap.keys.tab"),
            Key.Delete => L("keymap.keys.delete"),
            Key.Insert => L("keymap.keys.insert"),
            Key.Home => L("keymap.keys.home"),
            Key.End => L("keymap.keys.end"),
            Key.PageUp => L("keymap.keys.pageUp"),
            Key.PageDown => L("keymap.keys.pageDown"),
            Key.Up => L("keymap.keys.up"),
            Key.Down => L("keymap.keys.down"),
            Key.Left => L("keymap.keys.left"),
            Key.Right => L("keymap.keys.right"),
            _ => key.ToString()
        };

        parts.Add(keyName);
        return string.Join("+", parts);
    }

    private static string L(string key) => LocalizationService.Current?.GetString(key) ?? key;
}
