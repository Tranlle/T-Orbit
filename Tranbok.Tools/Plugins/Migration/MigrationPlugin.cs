using System.Windows;
using Tranbok.Tools.Infrastructure;
using Tranbok.Tools.Plugins.Migration.ViewModels;
using Tranbok.Tools.Plugins.Migration.Views;

namespace Tranbok.Tools.Plugins.Migration;

public sealed class MigrationPlugin : IPlugin
{
    private MigrationView? _view;

    public string Id          => "migration";
    public string Name        => "数据库迁移";
    public string IconGlyph   => "\uE1D3";   // Segoe MDL2 Assets — Database
    public string Description => "管理 EF Core 迁移文件：新增、编辑、执行、撤回，支持 SqlServer / PostgreSQL / MySQL";
    public int Sort           => 10;

    public FrameworkElement CreateView()
    {
        if (_view is not null) return _view;

        var vm = new MigrationViewModel();
        _view  = new MigrationView { DataContext = vm };
        return _view;
    }
}
