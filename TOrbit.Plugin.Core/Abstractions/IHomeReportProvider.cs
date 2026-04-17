using TOrbit.Plugin.Core.Models;

namespace TOrbit.Plugin.Core.Abstractions;

public interface IHomeReportProvider
{
    HomeReportDefinition GetDefinition();
}
