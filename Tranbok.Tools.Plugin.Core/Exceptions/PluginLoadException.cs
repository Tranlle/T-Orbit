namespace Tranbok.Tools.Plugin.Core.Exceptions;

public sealed class PluginLoadException : PluginException
{
    public PluginLoadException(string message) : base(message)
    {
    }

    public PluginLoadException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
