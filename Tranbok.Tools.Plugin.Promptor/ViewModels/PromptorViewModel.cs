using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
using Tranbok.Tools.Plugin.Promptor.Views;

namespace Tranbok.Tools.Plugin.Promptor.ViewModels;

public sealed partial class PromptorViewModel : PluginBaseViewModel, IDisposable
{
    private readonly IDesignerDialogService? _dialogService;
    private readonly IPluginVariableService? _variableService;
    private readonly string _pluginId;
    private readonly PromptOptimizationService _service = new();
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _copyCts;

    [ObservableProperty]
    private string rawInput = string.Empty;

    [ObservableProperty]
    private string optimizedOutput = string.Empty;

    [ObservableProperty]
    private DesignerOptionItem? selectedStrategyOption;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isCopied;

    [ObservableProperty]
    private string statusMessage = "就绪";

    public ObservableCollection<PromptorLogEntry> LogEntries { get; } = [];

    public string FormattedLogText => LogEntries.Count == 0
        ? "（暂无操作日志）\n\n提示：完成一次提示词优化后，此处将显示结构化的调用记录。"
        : string.Join(Environment.NewLine + Environment.NewLine,
            LogEntries.Select(e => e.FormatAsText()));

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
    public bool CanOptimize        => HasRawInput && !IsBusy;
    public string StrategyDescription => SelectedStrategyOption?.Description ?? string.Empty;
    public string CopyButtonText   => IsCopied ? "✓ 已复制" : "复制结果";

    public IRelayCommand OptimizeCommand    { get; }
    public IRelayCommand CopyCommand        { get; }
    public IRelayCommand ClearAllCommand    { get; }
    public IRelayCommand ClearOutputCommand { get; }
    public IRelayCommand CancelCommand      { get; }
    public IRelayCommand ShowLogCommand     { get; }
    public IRelayCommand ClearLogCommand    { get; }

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
        ShowLogCommand     = new AsyncRelayCommand(ShowLogDialogAsync);
        ClearLogCommand    = new RelayCommand(ClearLog);

        LogEntries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(FormattedLogText));
    }

    partial void OnRawInputChanged(string value)              => RaiseDerivedProperties();
    partial void OnOptimizedOutputChanged(string value)       => RaiseDerivedProperties();
    partial void OnIsBusyChanged(bool value)                  => RaiseDerivedProperties();
    partial void OnIsCopiedChanged(bool value)                => OnPropertyChanged(nameof(CopyButtonText));
    partial void OnSelectedStrategyOptionChanged(DesignerOptionItem? value) => RaiseDerivedProperties();

    private void RaiseDerivedProperties()
    {
        OnPropertyChanged(nameof(HasRawInput));
        OnPropertyChanged(nameof(HasOptimizedOutput));
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(CanOptimize));
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
        var inputPreview  = RawInput.Length > 80
            ? RawInput[..80].Replace('\n', ' ').Replace('\r', ' ') + "…"
            : RawInput.Replace('\n', ' ').Replace('\r', ' ');

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsBusy          = true;
        OptimizedOutput = string.Empty;
        StatusMessage   = $"正在优化（{strategyLabel}）…";

        var sw      = Stopwatch.StartNew();
        var success = false;
        string? errorMessage = null;

        try
        {
            await foreach (var chunk in _service.OptimizeStreamAsync(RawInput, strategy, config, _cts.Token))
            {
                var captured = chunk;
                await Dispatcher.UIThread.InvokeAsync(() => OptimizedOutput += captured);
            }

            sw.Stop();
            success       = true;
            StatusMessage = $"✓ 优化完成（{strategyLabel}）";
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            StatusMessage = "已取消";
            errorMessage  = "用户取消";
        }
        catch (Exception ex)
        {
            sw.Stop();
            StatusMessage = "✗ 优化失败";
            errorMessage  = ex.Message;
            await ShowAlertAsync("优化失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
            RaiseDerivedProperties();

            // 写入结构化日志
            LogEntries.Insert(0, new PromptorLogEntry
            {
                Time          = DateTime.Now,
                StrategyLabel = strategyLabel,
                Model         = config.ModelName,
                Provider      = config.Provider,
                IsSuccess     = success,
                Duration      = sw.Elapsed,
                InputPreview  = inputPreview,
                ErrorMessage  = errorMessage
            });
        }
    }

    private async Task ShowLogDialogAsync()
    {
        if (_dialogService is null || TryGetOwnerWindow() is not { } owner)
            return;

        var content = new LogDialogView { DataContext = this };

        await _dialogService.ShowSheetAsync(owner, new DesignerSheetViewModel
        {
            Title               = "操作日志",
            Description         = LogEntries.Count > 0 ? $"共 {LogEntries.Count} 条记录" : "暂无记录",
            Content             = content,
            ConfirmText         = "关闭",
            CancelText          = string.Empty,
            Icon                = DesignerDialogIcon.Info,
            BaseFontSize        = 13,
            DialogWidth         = 860,
            DialogHeight        = 560,
            LockSize            = true,
            HideSystemDecorations = true
        });
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

                // 按钮文案反馈：1.5s 后恢复
                _copyCts?.Cancel();
                _copyCts = new CancellationTokenSource();
                var token = _copyCts.Token;
                IsCopied = true;
                try
                {
                    await Task.Delay(1500, token);
                    IsCopied = false;
                }
                catch (OperationCanceledException) { /* 新一次复制触发，不重置 */ }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"复制失败：{ex.Message}";
        }
    }

    private void ClearAll()
    {
        RawInput        = string.Empty;
        OptimizedOutput = string.Empty;
        StatusMessage   = "就绪";
        RaiseDerivedProperties();
    }

    private void ClearLog()
    {
        LogEntries.Clear();
    }

    private PromptorConfig ReadConfig()
    {
        var provider    = GetVar("PROMPTOR_PROVIDER",     "openai");
        var endpoint    = GetVar("PROMPTOR_API_ENDPOINT", "");
        var apiKey      = GetVar("PROMPTOR_API_KEY",      "");
        var model       = GetVar("PROMPTOR_MODEL_NAME",   "gpt-4o");
        var maxTokensRaw = GetVar("PROMPTOR_MAX_TOKENS",  "2048");
        var tempRaw      = GetVar("PROMPTOR_TEMPERATURE", "1.0");

        if (!int.TryParse(maxTokensRaw, out var maxTokens) || maxTokens <= 0)
            maxTokens = 2048;

        if (!double.TryParse(tempRaw, CultureInfo.InvariantCulture, out var temperature))
            temperature = 1.0;

        var isOllama = string.Equals(provider, "ollama", StringComparison.OrdinalIgnoreCase);
        var hasEndpoint = !string.IsNullOrWhiteSpace(endpoint);

        if (!isOllama && !hasEndpoint && string.IsNullOrWhiteSpace(apiKey))
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
        _copyCts?.Cancel();
        _copyCts?.Dispose();
        _service.Dispose();
    }
}
