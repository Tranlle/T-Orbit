using System.Collections.ObjectModel;

namespace TOrbit.Plugin.Home.ViewModels;

public sealed class HomeReportGroupViewModel
{
    public required string Category { get; init; }
    public ObservableCollection<HomeReportItemViewModel> Reports { get; } = [];
}
