using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace Tranbok.Tools.Designer.Controls.Selectors;

public class DesignerRadioGroup : TemplatedControl
{
    private WrapPanel? _panel;

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<DesignerRadioGroup, string?>(nameof(Label));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<DesignerRadioGroup, string?>(nameof(Description));

    public static readonly StyledProperty<IEnumerable<object>?> ItemsSourceProperty =
        AvaloniaProperty.Register<DesignerRadioGroup, IEnumerable<object>?>(nameof(ItemsSource));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<DesignerRadioGroup, object?>(nameof(SelectedItem), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<DesignerRadioGroup, IDataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<string?> DisplayMemberPathProperty =
        AvaloniaProperty.Register<DesignerRadioGroup, string?>(nameof(DisplayMemberPath));

    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public IEnumerable<object>? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public object? SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }
    public IDataTemplate? ItemTemplate { get => GetValue(ItemTemplateProperty); set => SetValue(ItemTemplateProperty, value); }
    public string? DisplayMemberPath { get => GetValue(DisplayMemberPathProperty); set => SetValue(DisplayMemberPathProperty, value); }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _panel = e.NameScope.Find<WrapPanel>("PART_Panel");
        BuildOptions();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemsSourceProperty || change.Property == SelectedItemProperty || change.Property == ItemTemplateProperty)
        {
            BuildOptions();
        }
    }

    private void BuildOptions()
    {
        if (_panel is null)
        {
            return;
        }

        _panel.Children.Clear();

        if (ItemsSource is null)
        {
            return;
        }

        foreach (var item in ItemsSource)
        {
            Control content;
            if (ItemTemplate is not null)
            {
                content = (ItemTemplate.Build(item) as Control) ?? new TextBlock { Text = ResolveLabel(item) };
            }
            else
            {
                content = new TextBlock { Text = ResolveLabel(item) };
            }

            var radio = new RadioButton
            {
                Margin = new Thickness(0, 0, 8, 8),
                Content = content,
                Tag = item,
                IsChecked = Equals(item, SelectedItem)
            };
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true)
                {
                    SelectedItem = radio.Tag;
                }
            };
            _panel.Children.Add(radio);
        }
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
