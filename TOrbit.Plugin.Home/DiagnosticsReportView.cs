using Avalonia.Controls;
using Avalonia.Media;
using TOrbit.Core.Models;
using TOrbit.Core.Services;
using TOrbit.Designer.Services;

namespace TOrbit.Plugin.Home;

internal sealed class DiagnosticsReportView : UserControl
{
    public DiagnosticsReportView(IAppDiagnosticsService diagnosticsService, ILocalizationService localizationService)
    {
        var entries = diagnosticsService.Entries
            .Where(x => x.Severity != AppDiagnosticSeverity.Info)
            .OrderByDescending(x => x.Timestamp)
            .Take(5)
            .ToArray();

        if (entries.Length == 0)
        {
            Content = new TextBlock
            {
                Text = localizationService.GetString("home.report.noRecentDiagnostics"),
                Classes = { "caption-muted" },
                TextWrapping = TextWrapping.Wrap
            };
            return;
        }

        var stack = new StackPanel { Spacing = 10 };

        foreach (var entry in entries)
        {
            stack.Children.Add(new Border
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
                            Text = $"{entry.Source} · {entry.Timestamp:MM-dd HH:mm}",
                            Classes = { "caption-muted" },
                            MinHeight = 0
                        },
                        new TextBlock
                        {
                            Text = entry.Message,
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                }
            });
        }

        Content = stack;
    }
}
