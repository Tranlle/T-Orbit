using System.Collections.ObjectModel;
using TOrbit.Core.Models;

namespace TOrbit.Plugin.Monitor.ViewModels;

public sealed class PluginMonitorDetailsViewModel
{
    public PluginMonitorDetailsViewModel(
        PluginMonitorItemViewModel plugin,
        IReadOnlyList<AppDiagnosticEntry> diagnostics)
    {
        Plugin = plugin;
        Diagnostics = new ObservableCollection<AppDiagnosticEntry>(diagnostics);
    }

    public PluginMonitorItemViewModel Plugin { get; }
    public ObservableCollection<AppDiagnosticEntry> Diagnostics { get; }
    public bool HasDiagnostics => Diagnostics.Count > 0;
}
