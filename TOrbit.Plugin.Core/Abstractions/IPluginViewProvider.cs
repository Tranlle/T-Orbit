using System.Windows.Input;
using TOrbit.Plugin.Core.Models;

namespace TOrbit.Plugin.Core.Abstractions;

public interface IPluginViewProvider
{
    object GetMainView();
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

public sealed record PluginHeaderAction(
    string Label,
    ICommand Command,
    bool IsVisible = true,
    bool IsPrimary = false);
