using TOrbit.Plugin.Core.Models;

namespace TOrbit.Core.Services;

public interface IHomeReportRegistry
{
    IReadOnlyList<HomeReportDefinition> Reports { get; }
    event EventHandler? ReportsChanged;

    void Register(HomeReportDefinition definition);
    void RegisterRange(IEnumerable<HomeReportDefinition> definitions);
    void RemoveBySource(string sourcePluginId);
}
