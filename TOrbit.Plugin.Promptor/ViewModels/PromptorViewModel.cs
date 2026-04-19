using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TOrbit.Designer.Models;
using TOrbit.Designer.Services;
using TOrbit.Designer.ViewModels;
using TOrbit.Designer.ViewModels.Dialogs;
using TOrbit.Plugin.Promptor.Models;
using TOrbit.Plugin.Promptor.Services;
using TOrbit.Plugin.Promptor.Views;

namespace TOrbit.Plugin.Promptor.ViewModels;

public sealed partial class PromptorViewModel : PluginBaseViewModel, IDisposable
{
    private readonly IDesignerDialogService? _dialogService;
    private readonly PromptOptimizationService _service = new();
    private PromptorVariables _variables;
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _copyCts;

    public event EventHandler? HeaderSummaryChanged;

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
        ? "暂无日志\n\n运行后显示"
        : string.Join(Environment.NewLine + Environment.NewLine, LogEntries.Select(entry => entry.FormatAsText()));

    public IReadOnlyList<DesignerOptionItem> StrategyOptions { get; } =
    [
        new DesignerOptionItem
        {
            Key = "Structured",
            Label = "结构化",
            Value = OptimizationStrategy.Structured,
            Description = "结构化改写"
        },
        new DesignerOptionItem
        {
            Key = "FewShot",
            Label = "少样本",
            Value = OptimizationStrategy.FewShot,
            Description = "补充示例"
        },
        new DesignerOptionItem
        {
            Key = "ChainOfThought",
            Label = "推理链",
            Value = OptimizationStrategy.ChainOfThought,
            Description = "引导推理"
        },
        new DesignerOptionItem
        {
            Key = "Concise",
            Label = "精简",
            Value = OptimizationStrategy.Concise,
            Description = "压缩冗词"
        },
        new DesignerOptionItem
        {
            Key = "Technical",
            Label = "技术向",
            Value = OptimizationStrategy.Technical,
            Description = "强化约束"
        }
    ];

    public bool HasRawInput => !string.IsNullOrWhiteSpace(RawInput);
    public bool HasOptimizedOutput => !string.IsNullOrWhiteSpace(OptimizedOutput);
    public bool IsIdle => !IsBusy;
    public bool CanOptimize => HasRawInput && !IsBusy;
    public string StrategyDescription => SelectedStrategyOption?.Description ?? string.Empty;
    public string CopyButtonText => IsCopied ? "已复制" : "复制";
    public int LogCount => LogEntries.Count;

    public IRelayCommand OptimizeCommand { get; }
    public IRelayCommand CopyCommand { get; }
    public IRelayCommand ClearAllCommand { get; }
    public IRelayCommand ClearOutputCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand ShowLogCommand { get; }
    public IRelayCommand ClearLogCommand { get; }

    public PromptorViewModel(IDesignerDialogService? dialogService, PromptorVariables variables)
    {
        _dialogService = dialogService;
        _variables = variables;

        SelectedStrategyOption = StrategyOptions[0];

        OptimizeCommand = new AsyncRelayCommand(OptimizeAsync);
        CopyCommand = new AsyncRelayCommand(CopyToClipboardAsync);
        ClearAllCommand = new RelayCommand(ClearAll);
        ClearOutputCommand = new RelayCommand(() => OptimizedOutput = string.Empty);
        CancelCommand = new RelayCommand(() => _cts?.Cancel());
        ShowLogCommand = new AsyncRelayCommand(ShowLogDialogAsync);
        ClearLogCommand = new RelayCommand(ClearLog);

        LogEntries.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(FormattedLogText));
            OnPropertyChanged(nameof(LogCount));
            HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    partial void OnRawInputChanged(string value) => RaiseDerivedProperties();
    partial void OnOptimizedOutputChanged(string value) => RaiseDerivedProperties();
    partial void OnIsBusyChanged(bool value) => RaiseDerivedProperties();

    partial void OnIsCopiedChanged(bool value)
    {
        OnPropertyChanged(nameof(CopyButtonText));
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnSelectedStrategyOptionChanged(DesignerOptionItem? value) => RaiseDerivedProperties();

    public void UpdateVariables(PromptorVariables variables)
    {
        _variables = variables;
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseDerivedProperties()
    {
        OnPropertyChanged(nameof(HasRawInput));
        OnPropertyChanged(nameof(HasOptimizedOutput));
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(CanOptimize));
        OnPropertyChanged(nameof(StrategyDescription));
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task OptimizeAsync()
    {
        if (string.IsNullOrWhiteSpace(RawInput))
        {
            await ShowAlertAsync("需输入", "先输入提示词");
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

        var strategy = SelectedStrategyOption?.Value is OptimizationStrategy selected
            ? selected
            : OptimizationStrategy.Structured;
        var strategyLabel = SelectedStrategyOption?.Label ?? "结构化";
        var inputPreview = RawInput.Length > 80
            ? RawInput[..80].Replace('\n', ' ').Replace('\r', ' ') + "..."
            : RawInput.Replace('\n', ' ').Replace('\r', ' ');

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsBusy = true;
        OptimizedOutput = string.Empty;
        StatusMessage = "优化中";

        var sw = Stopwatch.StartNew();
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
            success = true;
            StatusMessage = "已完成";
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            StatusMessage = "已取消";
            errorMessage = "用户取消";
        }
        catch (Exception ex)
        {
            sw.Stop();
            StatusMessage = "已失败";
            errorMessage = ex.Message;
            await ShowAlertAsync("优化失败", ex.Message);
        }
        finally
        {
            IsBusy = false;
            RaiseDerivedProperties();

            LogEntries.Insert(0, new PromptorLogEntry
            {
                Time = DateTime.Now,
                StrategyLabel = strategyLabel,
                Model = config.ModelName,
                Provider = config.Provider,
                IsSuccess = success,
                Duration = sw.Elapsed,
                InputPreview = inputPreview,
                ErrorMessage = errorMessage
            });
        }
    }

    private PromptorConfig ReadConfig()
    {
        var provider = string.IsNullOrWhiteSpace(_variables.Provider) ? "openai" : _variables.Provider.Trim();
        var endpoint = _variables.ApiEndpoint?.Trim() ?? string.Empty;
        var apiKey = _variables.ApiKey?.Trim() ?? string.Empty;
        var model = string.IsNullOrWhiteSpace(_variables.ModelName) ? "gpt-4o" : _variables.ModelName.Trim();
        var maxTokens = _variables.MaxTokens > 0 ? _variables.MaxTokens : 2048;
        var temperature = double.IsFinite(_variables.Temperature) ? _variables.Temperature : 1.0;

        var isOllama = string.Equals(provider, "ollama", StringComparison.OrdinalIgnoreCase);
        var hasEndpoint = !string.IsNullOrWhiteSpace(endpoint);

        if (!isOllama && !hasEndpoint && string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "请配置 API Key");
        }

        return new PromptorConfig(provider, endpoint, apiKey, model, maxTokens, temperature);
    }

    private async Task ShowLogDialogAsync()
    {
        if (_dialogService is null || TryGetOwnerWindow() is not { } owner)
            return;

        var content = new LogDialogView { DataContext = this };

        await _dialogService.ShowSheetAsync(owner, new DesignerSheetViewModel
        {
            Title = "运行日志",
            Description = LogEntries.Count > 0 ? $"{LogEntries.Count} 条日志" : "暂无日志",
            Content = content,
            ConfirmText = "关闭",
            CancelText = string.Empty,
            Icon = DesignerDialogIcon.Info,
            BaseFontSize = 13,
            DialogWidth = 860,
            DialogHeight = 560,
            LockSize = true,
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
                StatusMessage = "已复制";

                _copyCts?.Cancel();
                _copyCts = new CancellationTokenSource();
                var token = _copyCts.Token;
                IsCopied = true;

                try
                {
                    await Task.Delay(1500, token);
                    IsCopied = false;
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "复制失败";
        }
    }

    private void ClearAll()
    {
        RawInput = string.Empty;
        OptimizedOutput = string.Empty;
        StatusMessage = "就绪";
        RaiseDerivedProperties();
    }

    private void ClearLog()
    {
        LogEntries.Clear();
    }

    private async Task ShowAlertAsync(string title, string message)
    {
        if (_dialogService is null || TryGetOwnerWindow() is not { } owner)
            return;

        await _dialogService.ShowConfirmAsync(owner, new DesignerConfirmDialogViewModel
        {
            Title = title,
            Message = message,
            ConfirmText = "OK",
            CancelText = string.Empty,
            Icon = DesignerDialogIcon.Info
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
