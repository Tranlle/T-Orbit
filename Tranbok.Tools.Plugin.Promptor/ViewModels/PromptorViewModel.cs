using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tranbok.Tools.Core.Services;
using Tranbok.Tools.Designer.Models;
using Tranbok.Tools.Designer.Services;
using Tranbok.Tools.Designer.ViewModels;
using Tranbok.Tools.Designer.ViewModels.Dialogs;
using Tranbok.Tools.Plugin.Promptor.Models;
using Tranbok.Tools.Plugin.Promptor.Services;

namespace Tranbok.Tools.Plugin.Promptor.ViewModels;

public sealed partial class PromptorViewModel : PluginBaseViewModel, IDisposable
{
    private readonly IDesignerDialogService? _dialogService;
    private readonly IPluginVariableService? _variableService;
    private readonly string _pluginId;
    private readonly PromptOptimizationService _service = new();
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string rawInput = string.Empty;

    [ObservableProperty]
    private string optimizedOutput = string.Empty;

    [ObservableProperty]
    private DesignerOptionItem? selectedStrategyOption;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = "就绪";

    [ObservableProperty]
    private string outputLog = string.Empty;

    public IReadOnlyList<DesignerOptionItem> StrategyOptions { get; } =
    [
        new DesignerOptionItem { Key = "Structured",     Label = "结构化", Value = OptimizationStrategy.Structured,     Description = "角色定义 + 任务描述 + 约束条件" },
        new DesignerOptionItem { Key = "FewShot",        Label = "少样本", Value = OptimizationStrategy.FewShot,        Description = "自动补充典型输入输出示例" },
        new DesignerOptionItem { Key = "ChainOfThought", Label = "思维链", Value = OptimizationStrategy.ChainOfThought, Description = "引导模型逐步分析推理" },
        new DesignerOptionItem { Key = "Concise",        Label = "精简版", Value = OptimizationStrategy.Concise,        Description = "去冗余、保核心" },
        new DesignerOptionItem { Key = "Technical",      Label = "技术向", Value = OptimizationStrategy.Technical,      Description = "代码规范 + 输出格式" }
    ];

    public bool HasRawInput        => !string.IsNullOrWhiteSpace(RawInput);
    public bool HasOptimizedOutput => !string.IsNullOrWhiteSpace(OptimizedOutput);
    public bool IsIdle             => !IsBusy;
    public string StrategyDescription => SelectedStrategyOption?.Description ?? string.Empty;

    public IRelayCommand OptimizeCommand     { get; }
    public IRelayCommand CopyCommand         { get; }
    public IRelayCommand ClearAllCommand     { get; }
    public IRelayCommand ClearOutputCommand  { get; }
    public IRelayCommand CancelCommand       { get; }
    public IRelayCommand ClearLogCommand     { get; }

    public PromptorViewModel(
        IDesignerDialogService? dialogService,
        IPluginVariableService? variableService,
        string pluginId)
    {
        _dialogService   = dialogService;
        _variableService = variableService;
        _pluginId        = pluginId;

        SelectedStrategyOption = StrategyOptions[0];

        OptimizeCommand    = new AsyncRelayCommand(OptimizeAsync);
        CopyCommand        = new AsyncRelayCommand(CopyToClipboardAsync);
        ClearAllCommand    = new RelayCommand(ClearAll);
        ClearOutputCommand = new RelayCommand(() => OptimizedOutput = string.Empty);
        CancelCommand      = new RelayCommand(() => _cts?.Cancel());
        ClearLogCommand    = new RelayCommand(() => OutputLog = string.Empty);
    }

    partial void OnRawInputChanged(string value)              => RaiseDerivedProperties();
    partial void OnOptimizedOutputChanged(string value)       => RaiseDerivedProperties();
    partial void OnIsBusyChanged(bool value)                  => RaiseDerivedProperties();
    partial void OnSelectedStrategyOptionChanged(DesignerOptionItem? value) => RaiseDerivedProperties();

    private void RaiseDerivedProperties()
    {
        OnPropertyChanged(nameof(HasRawInput));
        OnPropertyChanged(nameof(HasOptimizedOutput));
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(StrategyDescription));
    }

    private async Task OptimizeAsync()
    {
        if (string.IsNullOrWhiteSpace(RawInput))
        {
            await ShowAlertAsync("输入为空", "请先在左侧输入需要优化的提示词内容。");
            return;
        }

        PromptorConfig config;
        try
        {
            config = ReadConfig();
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("配置错误", ex.Message);
            return;
        }

        var strategy      = SelectedStrategyOption?.Value is OptimizationStrategy s ? s : OptimizationStrategy.Structured;
        var strategyLabel = SelectedStrategyOption?.Label ?? "结构化";

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsBusy          = true;
        OptimizedOutput = string.Empty;
        StatusMessage   = $"正在优化（{strategyLabel}）…";
        AppendLog($"\n▶  策略：{strategyLabel}  模型：{config.ModelName}  提供商：{config.Provider}");
        AppendLog(new string('─', 50));

        try
        {
            await foreach (var chunk in _service.OptimizeStreamAsync(RawInput, strategy, config, _cts.Token))
            {
                var captured = chunk;
                await Dispatcher.UIThread.InvokeAsync(() => OptimizedOutput += captured);
            }

            StatusMessage = $"✓ 优化完成（{strategyLabel}）";
            AppendLog("✓ 完成");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "已取消";
            AppendLog("✗ 已取消");
        }
        catch (Exception ex)
        {
            StatusMessage = "✗ 优化失败";
            AppendLog($"✗ 错误：{ex.Message}");
            await ShowAlertAsync("优化失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
            RaiseDerivedProperties();
        }
    }

    private async Task CopyToClipboardAsync()
    {
        if (string.IsNullOrEmpty(OptimizedOutput))
            return;

        try
        {
            if (TryGetOwnerWindow()?.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(OptimizedOutput);
                StatusMessage = "✓ 已复制到剪贴板";
                AppendLog("✓ 已复制优化结果到剪贴板");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"复制失败：{ex.Message}");
        }
    }

    private void ClearAll()
    {
        RawInput        = string.Empty;
        OptimizedOutput = string.Empty;
        StatusMessage   = "就绪";
        RaiseDerivedProperties();
    }

    private PromptorConfig ReadConfig()
    {
        var provider    = GetVar("PROMPTOR_PROVIDER",    "openai");
        var endpoint    = GetVar("PROMPTOR_API_ENDPOINT", "");
        var apiKey      = GetVar("PROMPTOR_API_KEY",      "");
        var model       = GetVar("PROMPTOR_MODEL_NAME",   "gpt-4o");
        var maxTokensRaw = GetVar("PROMPTOR_MAX_TOKENS", "2048");
        var tempRaw      = GetVar("PROMPTOR_TEMPERATURE", "1.0");

        if (!int.TryParse(maxTokensRaw, out var maxTokens) || maxTokens <= 0)
            maxTokens = 2048;

        if (!double.TryParse(tempRaw, CultureInfo.InvariantCulture, out var temperature))
            temperature = 1.0;

        var needsKey = !string.Equals(provider, "ollama", StringComparison.OrdinalIgnoreCase)
                       && string.IsNullOrWhiteSpace(endpoint);

        if (needsKey && string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "尚未配置 API 密钥。\n请在「设置 → 插件变量管理」中添加 PROMPTOR_API_KEY 变量。");
        }

        return new PromptorConfig(provider, endpoint, apiKey, model, maxTokens, temperature);
    }

    private string GetVar(string key, string defaultValue)
    {
        if (_variableService is null) return defaultValue;
        return _variableService.GetValue(_pluginId, key) ?? defaultValue;
    }

    private void AppendLog(string message)
    {
        OutputLog += message + Environment.NewLine;
    }

    private async Task ShowAlertAsync(string title, string message)
    {
        if (_dialogService is null || TryGetOwnerWindow() is not { } owner)
            return;

        await _dialogService.ShowConfirmAsync(owner, new DesignerConfirmDialogViewModel
        {
            Title       = title,
            Message     = message,
            ConfirmText = "知道了",
            CancelText  = string.Empty,
            Icon        = DesignerDialogIcon.Info
        });
    }

    private static Window? TryGetOwnerWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _service.Dispose();
    }
}
