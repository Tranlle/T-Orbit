using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace Tranbok.Tools.Designer.Controls.Inputs;

public class DesignerComboBox : DesignerInputControl
{
    private ComboBox? _comboBox;

    public static readonly StyledProperty<IEnumerable<object>?> ItemsSourceProperty =
        AvaloniaProperty.Register<DesignerComboBox, IEnumerable<object>?>(nameof(ItemsSource));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<DesignerComboBox, object?>(nameof(SelectedItem), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<DesignerComboBox, string?>(nameof(Placeholder));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<DesignerComboBox, IDataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<string?> DisplayMemberPathProperty =
        AvaloniaProperty.Register<DesignerComboBox, string?>(nameof(DisplayMemberPath));

    public IEnumerable<object>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public string? DisplayMemberPath
    {
        get => GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _comboBox = e.NameScope.Find<ComboBox>("PART_ComboBox");
        ApplyItemTemplate();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DisplayMemberPathProperty || change.Property == ItemTemplateProperty)
        {
            ApplyItemTemplate();
        }
    }

    private void ApplyItemTemplate()
    {
        if (_comboBox is null)
        {
            return;
        }

        _comboBox.ItemTemplate = ItemTemplate ?? CreateDisplayTemplate();
    }

    private IDataTemplate CreateDisplayTemplate()
    {
        return new FuncDataTemplate<object?>((item, _) =>
        {
            var text = item?.ToString();
            if (!string.IsNullOrWhiteSpace(DisplayMemberPath) && item is not null)
            {
                var property = item.GetType().GetProperty(DisplayMemberPath);
                if (property is not null)
                {
                    text = property.GetValue(item)?.ToString();
                }
            }

            return new TextBlock { Text = text };
        });
    }
}
