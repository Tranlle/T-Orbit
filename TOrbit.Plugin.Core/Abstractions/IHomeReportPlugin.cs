namespace TOrbit.Plugin.Core.Abstractions;

public interface IHomeReportPlugin
{
    IEnumerable<IHomeReportProvider> GetHomeReports();
}
