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
    private readonly ILocalizationService _localizationService;
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
    private string statusMessage;

    public ObservableCollection<PromptorLogEntry> LogEntries { get; } = [];

    public string FormattedLogText => LogEntries.Count == 0
        ? $"{L("promptor.logEmpty")}{Environment.NewLine}{Environment.NewLine}{L("promptor.logEmptyDescription")}"
        : string.Join(Environment.NewLine + Environment.NewLine, LogEntries.Select(entry => entry.FormatAsText()));

    public IReadOnlyList<DesignerOptionItem> StrategyOptions => BuildStrategyOptions();

    public bool HasRawInput => !string.IsNullOrWhiteSpace(RawInput);
    public bool HasOptimizedOutput => !string.IsNullOrWhiteSpace(OptimizedOutput);
    public bool IsIdle => !IsBusy;
    public bool CanOptimize => HasRawInput && !IsBusy;
    public string StrategyDescription => SelectedStrategyOption?.Description ?? string.Empty;
    public string CopyButtonText => IsCopied ? L("promptor.copied") : L("promptor.copy");
    public int LogCount => LogEntries.Count;
    public string LogEntriesSummary => string.Format(L("promptor.logEntriesFormat"), LogEntries.Count);

    public IRelayCommand OptimizeCommand { get; }
    public IRelayCommand CopyCommand { get; }
    public IRelayCommand ClearAllCommand { get; }
    public IRelayCommand ClearOutputCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand ShowLogCommand { get; }
    public IRelayCommand ClearLogCommand { get; }

    public PromptorViewModel(IDesignerDialogService? dialogService, PromptorVariables variables, ILocalizationService localizationService)
    {
        _dialogService = dialogService;
        _variables = variables;
        _localizationService = localizationService;
        statusMessage = L("runtime.ready");

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
            OnPropertyChanged(nameof(LogEntriesSummary));
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
            await ShowAlertAsync(L("promptor.messages.inputRequiredTitle"), L("promptor.messages.inputRequired"));
            return;
        }

        PromptorConfig config;
        try
        {
            config = ReadConfig();
        }
        catch (Exception ex)
        {
            await ShowAlertAsync(L("promptor.messages.configError"), ex.Message);
            return;
        }

        var strategy = SelectedStrategyOption?.Value is OptimizationStrategy selected
            ? selected
            : OptimizationStrategy.Structured;
        var strategyLabel = SelectedStrategyOption?.Label ?? L("promptor.strategy.structured.label");
        var inputPreview = RawInput.Length > 80
            ? RawInput[..80].Replace('\n', ' ').Replace('\r', ' ') + "..."
            : RawInput.Replace('\n', ' ').Replace('\r', ' ');

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsBusy = true;
        OptimizedOutput = string.Empty;
        StatusMessage = L("promptor.messages.optimizing");

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
            StatusMessage = L("promptor.messages.completed");
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            StatusMessage = L("promptor.messages.cancelled");
            errorMessage = L("promptor.messages.cancelledByUser");
        }
        catch (Exception ex)
        {
            sw.Stop();
            StatusMessage = L("promptor.messages.failed");
            errorMessage = ex.Message;
            await ShowAlertAsync(L("promptor.messages.optimizeFailed"), ex.Message);
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
            throw new InvalidOperationException(L("promptor.messages.apiKeyRequired"));

        return new PromptorConfig(provider, endpoint, apiKey, model, maxTokens, temperature);
    }

    private async Task ShowLogDialogAsync()
    {
        if (_dialogService is null || TryGetOwnerWindow() is not { } owner)
            return;

        var content = new LogDialogView { DataContext = this };

        await _dialogService.ShowSheetAsync(owner, new DesignerSheetViewModel
        {
            Title = L("promptor.log"),
            Description = LogEntries.Count > 0
                ? string.Format(L("promptor.logEntriesFormat"), LogEntries.Count)
                : L("promptor.logEmpty"),
            Content = content,
            ConfirmText = L("dialog.close"),
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
                StatusMessage = L("promptor.messages.copied");

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
        catch
        {
            StatusMessage = L("promptor.messages.copyFailed");
        }
    }

    private void ClearAll()
    {
        RawInput = string.Empty;
        OptimizedOutput = string.Empty;
        StatusMessage = L("runtime.ready");
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

    private IReadOnlyList<DesignerOptionItem> BuildStrategyOptions() =>
    [
        new DesignerOptionItem
        {
            Key = "Structured",
            Label = L("promptor.strategy.structured.label"),
            Value = OptimizationStrategy.Structured,
            Description = L("promptor.strategy.structured.description")
        },
        new DesignerOptionItem
        {
            Key = "FewShot",
            Label = L("promptor.strategy.fewShot.label"),
            Value = OptimizationStrategy.FewShot,
            Description = L("promptor.strategy.fewShot.description")
        },
        new DesignerOptionItem
        {
            Key = "ChainOfThought",
            Label = L("promptor.strategy.chainOfThought.label"),
            Value = OptimizationStrategy.ChainOfThought,
            Description = L("promptor.strategy.chainOfThought.description")
        },
        new DesignerOptionItem
        {
            Key = "Concise",
            Label = L("promptor.strategy.concise.label"),
            Value = OptimizationStrategy.Concise,
            Description = L("promptor.strategy.concise.description")
        },
        new DesignerOptionItem
        {
            Key = "Technical",
            Label = L("promptor.strategy.technical.label"),
            Value = OptimizationStrategy.Technical,
            Description = L("promptor.strategy.technical.description")
        }
    ];

    private string L(string key) => _localizationService.GetString(key);

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
