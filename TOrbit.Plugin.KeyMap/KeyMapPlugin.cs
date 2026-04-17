using Avalonia.Controls;
using TOrbit.Core.Services;
using TOrbit.Designer.Abstractions;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Models;
using TOrbit.Plugin.KeyMap.ViewModels;
using TOrbit.Plugin.KeyMap.Views;

namespace TOrbit.Plugin.KeyMap;

public sealed class KeyMapPlugin : BasePlugin, IVisualPlugin, IPluginHeaderActionsProvider, IPluginPageHeaderProvider
{
    private readonly IKeyMapService _keyMapService;

    private KeyMapView? _view;
    private KeyMapViewModel? _viewModel;
    private IReadOnlyList<PluginHeaderAction>? _headerActions;

    public KeyMapPlugin(IKeyMapService keyMapService)
        => _keyMapService = keyMapService;

    public override PluginDescriptor Descriptor { get; } =
        CreateDescriptor<KeyMapPlugin>(KeyMapPluginMetadata.Instance);

    public event EventHandler? HeaderChanged;

    public override Control GetMainView()
    {
        EnsureView();
        return _view!;
    }

    public IReadOnlyList<PluginHeaderAction> GetHeaderActions()
    {
        EnsureView();
        return _headerActions ?? [];
    }

    public PluginPageHeaderModel? GetPageHeader()
    {
        EnsureView();
        if (_viewModel is null)
            return null;

        return new PluginPageHeaderModel
        {
            Context = string.IsNullOrWhiteSpace(_viewModel.SearchText)
                ? "快捷键按插件分组展示，可直接浏览和编辑。"
                : $"当前正在按“{_viewModel.SearchText}”筛选快捷键列表。",
            Metrics =
            [
                new PluginPageHeaderMetric
                {
                    Label = "绑定数",
                    Value = _viewModel.TotalBindingCount.ToString(),
                    Tone = PluginPageHeaderTone.Neutral
                },
                new PluginPageHeaderMetric
                {
                    Label = "插件数",
                    Value = _viewModel.GroupCount.ToString(),
                    Tone = PluginPageHeaderTone.Accent
                },
                new PluginPageHeaderMetric
                {
                    Label = "已修改",
                    Value = _viewModel.ModifiedBindingCount.ToString(),
                    Tone = _viewModel.ModifiedBindingCount > 0 ? PluginPageHeaderTone.Warning : PluginPageHeaderTone.Success
                },
                new PluginPageHeaderMetric
                {
                    Label = "已禁用",
                    Value = _viewModel.DisabledBindingCount.ToString(),
                    Tone = _viewModel.DisabledBindingCount > 0 ? PluginPageHeaderTone.Warning : PluginPageHeaderTone.Success
                }
            ],
            Badges =
            [
                new PluginPageHeaderBadge
                {
                    Text = _viewModel.SelectedBinding is null ? "未选择绑定" : _viewModel.SelectedBinding.Name,
                    Tone = _viewModel.SelectedBinding is null ? PluginPageHeaderTone.Neutral : PluginPageHeaderTone.Accent
                },
                new PluginPageHeaderBadge
                {
                    Text = _viewModel.SelectedBinding?.PluginName ?? "浏览全部插件",
                    Tone = PluginPageHeaderTone.Neutral
                }
            ]
        };
    }

    private void EnsureView()
    {
        if (_viewModel is not null)
            return;

        _viewModel = new KeyMapViewModel(_keyMapService);
        _viewModel.HeaderSummaryChanged += ViewModelOnHeaderSummaryChanged;
        _headerActions =
        [
            new PluginHeaderAction("重置全部", _viewModel.ResetAllCommand),
            new PluginHeaderAction("保存", _viewModel.SaveCommand, IsPrimary: true)
        ];
        _view = new KeyMapView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        if (_viewModel is not null)
        {
            _viewModel.HeaderSummaryChanged -= ViewModelOnHeaderSummaryChanged;
            _viewModel.Dispose();
        }

        _view = null;
        _viewModel = null;
        _headerActions = null;
        return ValueTask.CompletedTask;
    }

    private void ViewModelOnHeaderSummaryChanged(object? sender, EventArgs e)
        => HeaderChanged?.Invoke(this, EventArgs.Empty);
}
