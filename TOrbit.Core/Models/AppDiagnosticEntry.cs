namespace TOrbit.Core.Models;

public sealed record AppDiagnosticEntry(
    AppDiagnosticSeverity Severity,
    string Source,
    string Message,
    DateTimeOffset Timestamp,
    Exception? Exception = null);

public enum AppDiagnosticSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}
