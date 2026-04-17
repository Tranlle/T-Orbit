using TOrbit.Plugin.Core.Models;

namespace TOrbit.Core.Services;

public sealed class HomeReportRegistry : IHomeReportRegistry
{
    private readonly Dictionary<string, HomeReportDefinition> _reports = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _syncRoot = new();

    public IReadOnlyList<HomeReportDefinition> Reports
    {
        get
        {
            lock (_syncRoot)
            {
                return _reports.Values
                    .OrderBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.Sort)
                    .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }
    }

    public event EventHandler? ReportsChanged;

    public void Register(HomeReportDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        lock (_syncRoot)
            _reports[definition.Id] = definition;

        ReportsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RegisterRange(IEnumerable<HomeReportDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        lock (_syncRoot)
        {
            foreach (var definition in definitions)
                _reports[definition.Id] = definition;
        }

        ReportsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveBySource(string sourcePluginId)
    {
        if (string.IsNullOrWhiteSpace(sourcePluginId))
            return;

        var changed = false;

        lock (_syncRoot)
        {
            foreach (var id in _reports.Values
                         .Where(x => string.Equals(x.SourcePluginId, sourcePluginId, StringComparison.OrdinalIgnoreCase))
                         .Select(x => x.Id)
                         .ToArray())
            {
                changed |= _reports.Remove(id);
            }
        }

        if (changed)
            ReportsChanged?.Invoke(this, EventArgs.Empty);
    }
}
