using TOrbit.Core.Models;

namespace TOrbit.Core.Services;

public interface IAppDiagnosticsService
{
    IReadOnlyList<AppDiagnosticEntry> Entries { get; }

    event EventHandler<AppDiagnosticEntry>? EntryRecorded;

    void ReportInfo(string source, string message);

    void ReportWarning(string source, string message, Exception? exception = null);

    void ReportError(string source, string message, Exception? exception = null);
}
