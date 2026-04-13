using Tranbok.Tools.Core.Constants;

namespace Tranbok.Tools.Core.Services;

public sealed class AppShellService : IAppShellService
{
    public string AppName => ToolHostConstants.HostName;
    public string WorkspaceRoot => AppContext.BaseDirectory;
}
