using Avalonia.Controls;
using TOrbit.Designer.Abstractions;
using TOrbit.Designer.Services;
using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Tools;
using TOrbit.Plugin.Promptor.ViewModels;
using TOrbit.Plugin.Promptor.Views;

namespace TOrbit.Plugin.Promptor;

public sealed class PromptorPlugin : BasePlugin, IVisualPlugin, IPluginVariableReceiver
{
    private PromptorView?      _view;
    private PromptorViewModel? _viewModel;

    // 最后一次注入后的解密明文变量（在视图创建前暂存）
    private IReadOnlyDictionary<string, string> _resolvedVariables =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public override PluginDescriptor Descriptor { get; } =
        CreateDescriptor<PromptorPlugin>(PromptorPluginMetadata.Instance);

    // ── IPluginVariableReceiver ───────────────────────────────────────────────
    //
    //  基座调用此方法传入原始存储值（加密字段为密文）。
    //  插件通过 Context.GetTool<IPluginEncryptionTool>() 获取基座加密 Tool 自行解密。

    public void OnVariablesInjected(IReadOnlyDictionary<string, string> rawValues)
    {
        var tool      = Context.GetTool<IPluginEncryptionTool>();
        var resolved  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var defs      = Descriptor.VariableDefinitions ?? [];

        foreach (var def in defs)
        {
            rawValues.TryGetValue(def.Key, out var raw);

            string value;
            if (def.IsEncrypted && !string.IsNullOrEmpty(raw) && tool is not null)
                value = tool.TryDecrypt(raw) ?? def.DefaultValue;
            else
                value = string.IsNullOrEmpty(raw) ? def.DefaultValue : raw;

            resolved[def.Key] = value;
        }

        _resolvedVariables = resolved;
        _viewModel?.UpdateVariables(resolved);
    }

    // ── IVisualPlugin ─────────────────────────────────────────────────────────

    public override Control GetMainView()
    {
        EnsureView();
        return _view!;
    }

    private void EnsureView()
    {
        if (_viewModel is null)
        {
            var dialogService = Context.GetTool<IDesignerDialogService>();
            _viewModel = new PromptorViewModel(dialogService, _resolvedVariables);
        }

        _view ??= new PromptorView { DataContext = _viewModel };
    }

    protected override ValueTask OnDisposeAsync()
    {
        _view = null;
        _viewModel?.Dispose();
        _viewModel = null;
        return ValueTask.CompletedTask;
    }
}
