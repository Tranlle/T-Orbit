using TOrbit.Plugin.Core.Models;

namespace TOrbit.Plugin.Migration.Models;

public sealed class MigrationVariables
{
    [PluginVariableKey("TORBIT_DB_CONNECTION")]
    public string DefaultConnectionString { get; set; } = string.Empty;
}
