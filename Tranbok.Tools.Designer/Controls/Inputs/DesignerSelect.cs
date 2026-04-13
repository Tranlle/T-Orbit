using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Tranbok.Tools.Designer.Controls.Inputs;

public class DesignerSelect : DesignerComboBox
{
    private ComboBox? _comboBox;

    public static readonly StyledProperty<bool> IsSearchEnabledProperty =
        AvaloniaProperty.Register<DesignerSelect, bool>(nameof(IsSearchEnabled), true);

    public static readonly StyledProperty<string?> SearchTextProperty =
        AvaloniaProperty.Register<DesignerSelect, string?>(nameof(SearchText), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string?> EmptyTextProperty =
        AvaloniaProperty.Register<DesignerSelect, string?>(nameof(EmptyText), "没有匹配项");

    public bool IsSearchEnabled
    {
        get => GetValue(IsSearchEnabledProperty);
        set => SetValue(IsSearchEnabledProperty, value);
    }

    public string? SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public string? EmptyText
    {
        get => GetValue(EmptyTextProperty);
        set => SetValue(EmptyTextProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _comboBox = e.NameScope.Find<ComboBox>("PART_ComboBox");
        if (_comboBox is not null)
        {
            _comboBox.KeyDown -= ComboBox_KeyDown;
            _comboBox.KeyDown += ComboBox_KeyDown;
        }
        ApplyFilteredItems();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SearchTextProperty || change.Property == ItemsSourceProperty || change.Property == DisplayMemberPathProperty)
        {
            ApplyFilteredItems();
        }
    }

    private void ComboBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_comboBox is null)
        {
            return;
        }

        if (e.Key == Key.Down)
        {
            _comboBox.IsDropDownOpen = true;
        }
        else if (e.Key == Key.Escape)
        {
            _comboBox.IsDropDownOpen = false;
        }
    }

    private void ApplyFilteredItems()
    {
        if (_comboBox is null)
        {
            return;
        }

        var source = ItemsSource?.ToList() ?? [];
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            _comboBox.ItemsSource = source;
            return;
        }

        var keyword = SearchText.Trim();
        _comboBox.ItemsSource = source.Where(item => ResolveLabel(item)?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true).ToList();
    }

    private string? ResolveLabel(object? item)
    {
        if (item is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(DisplayMemberPath))
        {
            var property = item.GetType().GetProperty(DisplayMemberPath);
            if (property is not null)
            {
                return property.GetValue(item)?.ToString();
            }
        }

        return item.ToString();
    }
}
