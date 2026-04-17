using Avalonia.Controls;
using Avalonia.Media;
using TOrbit.Core.Services;
using TOrbit.Plugin.Core.Enums;

namespace TOrbit.Plugin.Home;

internal sealed class PluginOverviewReportView : UserControl
{
    public PluginOverviewReportView(IPluginCatalogService pluginCatalog)
    {
        var plugins = pluginCatalog.Plugins;

        Content = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 12,
            Children =
            {
                CreateMetric("已启用", plugins.Count(x => x.IsEnabled).ToString()),
                CreateMetric("运行中", plugins.Count(x => x.State == PluginState.Running).ToString(), 1),
                CreateMetric("故障", plugins.Count(x => x.State == PluginState.Faulted).ToString(), 0, 1),
                CreateMetric("可视插件", plugins.Count(x => x.Kind == PluginKind.Visual).ToString(), 1, 1)
            }
        };
    }

    private static Control CreateMetric(string label, string value, int column = 0, int row = 0)
    {
        var border = new Border
        {
            Classes = { "panel" },
            Padding = new Avalonia.Thickness(14, 12),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock
                    {
                        Text = label,
                        Classes = { "caption-muted" },
                        MinHeight = 0
                    },
                    new TextBlock
                    {
                        Text = value,
                        FontSize = 22,
                        FontWeight = FontWeight.SemiBold
                    }
                }
            }
        };

        Grid.SetColumn(border, column);
        Grid.SetRow(border, row);
        return border;
    }
}
