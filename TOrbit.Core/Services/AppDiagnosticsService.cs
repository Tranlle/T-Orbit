using TOrbit.Core.Models;

namespace TOrbit.Core.Services;

public sealed class AppDiagnosticsService : IAppDiagnosticsService
{
    private readonly object _syncRoot = new();
    private readonly List<AppDiagnosticEntry> _entries = [];

    public IReadOnlyList<AppDiagnosticEntry> Entries
    {
        get
        {
            lock (_syncRoot)
            {
                return _entries.ToArray();
            }
        }
    }

    public event EventHandler<AppDiagnosticEntry>? EntryRecorded;

    public void ReportInfo(string source, string message)
        => Record(AppDiagnosticSeverity.Info, source, message);

    public void ReportWarning(string source, string message, Exception? exception = null)
        => Record(AppDiagnosticSeverity.Warning, source, message, exception);

    public void ReportError(string source, string message, Exception? exception = null)
        => Record(AppDiagnosticSeverity.Error, source, message, exception);

    private void Record(AppDiagnosticSeverity severity, string source, string message, Exception? exception = null)
    {
        var entry = new AppDiagnosticEntry(severity, source, message, DateTimeOffset.Now, exception);

        lock (_syncRoot)
            _entries.Add(entry);

        EntryRecorded?.Invoke(this, entry);
    }
}
