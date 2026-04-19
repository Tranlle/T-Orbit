using System.Windows.Input;
using TOrbit.Plugin.Core.Models;

namespace TOrbit.Plugin.Core.Abstractions;

public interface IPluginViewProvider
{
    object GetMainView();
}

public interface IPluginDisplayInfoProvider
{
    event EventHandler? DisplayInfoChanged;

    string DisplayName { get; }

    string DisplayDescription { get; }
}

public interface IPluginHeaderActionsProvider
{
    IReadOnlyList<PluginHeaderAction> GetHeaderActions();
}

public interface IPluginPageHeaderProvider
{
    event EventHandler? HeaderChanged;

    PluginPageHeaderModel? GetPageHeader();
}

public interface IPluginHeaderSearchProvider
{
    string SearchText { get; set; }

    string SearchPlaceholder { get; }
}

public sealed record PluginHeaderAction(
    string Label,
    ICommand Command,
    bool IsVisible = true,
    bool IsPrimary = false);
