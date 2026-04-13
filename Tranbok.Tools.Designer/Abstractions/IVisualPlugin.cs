using Avalonia.Controls;

namespace Tranbok.Tools.Designer.Abstractions;

public interface IVisualPlugin
{
    Control GetMainView();
}
