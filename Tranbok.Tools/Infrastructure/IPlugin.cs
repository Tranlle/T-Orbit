using System.Windows;

namespace Tranbok.Tools.Infrastructure;

/// <summary>
/// Plugin capability contract.
/// Each plugin encapsulates a self-contained feature that can be hot-toggled in the sidebar.
/// </summary>
public interface IPlugin
{
    /// <summary>Unique identifier (e.g. "migration")</summary>
    string Id { get; }

    /// <summary>Display name shown in the sidebar</summary>
    string Name { get; }

    /// <summary>Segoe MDL2 Assets glyph code (e.g. "\uE1D3")</summary>
    string IconGlyph { get; }

    /// <summary>Short description shown as tooltip</summary>
    string Description { get; }

    /// <summary>Sort order in sidebar, 0-100. Smaller comes first.</summary>
    int Sort { get; }

    /// <summary>
    /// Create (or return cached) the plugin's main view.
    /// The view should be self-contained with its own ViewModel.
    /// </summary>
    FrameworkElement CreateView();
}
