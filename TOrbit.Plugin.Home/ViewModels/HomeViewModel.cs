using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TOrbit.Core.Services;

namespace TOrbit.Plugin.Home.ViewModels;

public sealed partial class HomeViewModel : ObservableObject, IDisposable
{
    private readonly IHomeReportRegistry _registry;
    private CancellationTokenSource? _deferredLoadCts;

    public ObservableCollection<HomeReportGroupViewModel> ReportGroups { get; } = [];
    public bool HasReports => ReportGroups.Count > 0;

    public HomeViewModel(IHomeReportRegistry registry)
    {
        _registry = registry;
        _registry.ReportsChanged += ReportsChanged;
        SyncReports();
    }

    private void ReportsChanged(object? sender, EventArgs e)
    {
        SyncReports();
    }

    private void SyncReports()
    {
        var existing = ReportGroups
            .SelectMany(group => group.Reports)
            .ToDictionary(x => x.Definition.Id, StringComparer.OrdinalIgnoreCase);

        var next = _registry.Reports;

        ReportGroups.Clear();

        foreach (var group in next
                     .GroupBy(x => string.IsNullOrWhiteSpace(x.Category) ? "通用" : x.Category)
                     .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var groupViewModel = new HomeReportGroupViewModel
            {
                Category = group.Key
            };

            foreach (var definition in group)
            {
                if (existing.TryGetValue(definition.Id, out var current))
                {
                    groupViewModel.Reports.Add(current);
                    continue;
                }

                groupViewModel.Reports.Add(new HomeReportItemViewModel(definition));
            }

            ReportGroups.Add(groupViewModel);
        }

        OnPropertyChanged(nameof(HasReports));
        ScheduleDeferredLoading();
    }

    private void ScheduleDeferredLoading()
    {
        _deferredLoadCts?.Cancel();
        _deferredLoadCts?.Dispose();
        _deferredLoadCts = new CancellationTokenSource();
        _ = DeferredLoadReportsAsync(_deferredLoadCts.Token);
    }

    private async Task DeferredLoadReportsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(180, cancellationToken);

            foreach (var item in ReportGroups.SelectMany(x => x.Reports))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await item.EnsureLoadedAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose()
    {
        _registry.ReportsChanged -= ReportsChanged;
        _deferredLoadCts?.Cancel();
        _deferredLoadCts?.Dispose();
    }
}
