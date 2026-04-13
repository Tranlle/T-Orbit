using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Tranbok.Tools.Designer.Controls.Selectors;

public class DesignerMultiSelect : TemplatedControl
{
    private ListBox? _listBox;
    private WrapPanel? _chipsPanel;

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<DesignerMultiSelect, string?>(nameof(Label));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<DesignerMultiSelect, string?>(nameof(Description));

    public static readonly StyledProperty<IEnumerable<object>?> ItemsSourceProperty =
        AvaloniaProperty.Register<DesignerMultiSelect, IEnumerable<object>?>(nameof(ItemsSource));

    public static readonly StyledProperty<IList<object>?> SelectedItemsProperty =
        AvaloniaProperty.Register<DesignerMultiSelect, IList<object>?>(nameof(SelectedItems), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<DesignerMultiSelect, IDataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<string?> DisplayMemberPathProperty =
        AvaloniaProperty.Register<DesignerMultiSelect, string?>(nameof(DisplayMemberPath));

    public static readonly StyledProperty<string?> EmptyTextProperty =
        AvaloniaProperty.Register<DesignerMultiSelect, string?>(nameof(EmptyText), "暂无可选项");

    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public IEnumerable<object>? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public IList<object>? SelectedItems { get => GetValue(SelectedItemsProperty); set => SetValue(SelectedItemsProperty, value); }
    public IDataTemplate? ItemTemplate { get => GetValue(ItemTemplateProperty); set => SetValue(ItemTemplateProperty, value); }
    public string? DisplayMemberPath { get => GetValue(DisplayMemberPathProperty); set => SetValue(DisplayMemberPathProperty, value); }
    public string? EmptyText { get => GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_listBox is not null)
        {
            _listBox.SelectionChanged -= ListBox_SelectionChanged;
        }

        _listBox = e.NameScope.Find<ListBox>("PART_ListBox");
        _chipsPanel = e.NameScope.Find<WrapPanel>("PART_ChipsPanel");
        if (_listBox is not null)
        {
            _listBox.SelectionChanged += ListBox_SelectionChanged;
            SyncListSelection();
        }

        RefreshChips();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedItemsProperty)
        {
            SyncListSelection();
            RefreshChips();
        }
    }

    private void ListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_listBox is null)
        {
            return;
        }

        SelectedItems = _listBox.SelectedItems?.Cast<object>().ToList();
        RefreshChips();
    }

    private void SyncListSelection()
    {
        if (_listBox?.SelectedItems is null || SelectedItems is null)
        {
            return;
        }

        _listBox.SelectedItems.Clear();
        foreach (var item in SelectedItems)
        {
            _listBox.SelectedItems.Add(item);
        }
    }

    private void RefreshChips()
    {
        if (_chipsPanel is null)
        {
            return;
        }

        _chipsPanel.Children.Clear();
        if (SelectedItems is null)
        {
            return;
        }

        foreach (var item in SelectedItems.ToList())
        {
            var chipPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                VerticalAlignment = VerticalAlignment.Center
            };
            chipPanel.Children.Add(new TextBlock { Text = ResolveLabel(item), VerticalAlignment = VerticalAlignment.Center });

            var removeButton = new Button
            {
                Classes = { "ghost" },
                Content = "✕",
                Padding = new Thickness(4, 0)
            };
            removeButton.Click += (_, _) => RemoveSelectedItem(item);
            chipPanel.Children.Add(removeButton);

            _chipsPanel.Children.Add(new Border
            {
                Classes = { "panel" },
                Padding = new Thickness(10, 4),
                Margin = new Thickness(0, 0, 6, 6),
                Child = chipPanel
            });
        }
    }

    private void RemoveSelectedItem(object item)
    {
        if (SelectedItems is null)
        {
            return;
        }

        var newItems = SelectedItems.Where(selected => !Equals(selected, item)).ToList<object>();
        SelectedItems = newItems;
        SyncListSelection();
        RefreshChips();
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
