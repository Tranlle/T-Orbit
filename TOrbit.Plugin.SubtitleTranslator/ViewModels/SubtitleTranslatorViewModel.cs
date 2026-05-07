using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TOrbit.Core.Services;
using TOrbit.Designer.Services;
using TOrbit.Designer.ViewModels.Dialogs;
using TOrbit.Designer.ViewModels;
using TOrbit.Plugin.Core.Enums;
using TOrbit.Plugin.Core.Models;
using TOrbit.Plugin.SubtitleTranslator.Models;
using TOrbit.Plugin.SubtitleTranslator.Services;
using TOrbit.Plugin.SubtitleTranslator.Views;

namespace TOrbit.Plugin.SubtitleTranslator.ViewModels;

[SupportedOSPlatform("windows")]
public sealed partial class SubtitleTranslatorViewModel : PluginBaseViewModel, IDisposable
{
    private readonly string _pluginId;
    private readonly ILocalizationService _localizationService;
    private readonly IPluginVariableService _pluginVariableService;
    private readonly IDesignerDialogService _dialogService;
    private readonly RegionSelectionService _regionSelectionService = new();
    private readonly SubtitlePipelineService _pipelineService;
    private CancellationTokenSource? _runCts;
    private SubtitleTranslatorVariables _variables;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSourceText))]
    private string currentSourceText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTranslatedText))]
    private string currentTranslatedText = string.Empty;

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private string workspaceHint = string.Empty;

    [ObservableProperty]
    private string selectedRegionText = string.Empty;

    [ObservableProperty]
    private string lastUpdatedText = string.Empty;

    [ObservableProperty]
    private bool hasRegionSelection;

    [ObservableProperty]
    private bool isRunning;

    [ObservableProperty]
    private int sampleIntervalMs = 500;

    [ObservableProperty]
    private SubtitleRegion? selectedRegion;

    [ObservableProperty]
    private string translationStatusText = string.Empty;

    [ObservableProperty]
    private string pipelineStateText = string.Empty;

    [ObservableProperty]
    private string ocrStatusText = string.Empty;

    [ObservableProperty]
    private string emptyStateTitle = string.Empty;

    [ObservableProperty]
    private string emptyStateDescription = string.Empty;

    public event EventHandler? HeaderSummaryChanged;

    public bool HasSourceText => !string.IsNullOrWhiteSpace(CurrentSourceText);

    public bool HasTranslatedText => !string.IsNullOrWhiteSpace(CurrentTranslatedText);

    public string RegionStateText => HasRegionSelection ? L("subtitle.state.ready") : L("subtitle.state.noRegion");

    public string RunStateText => IsRunning ? L("subtitle.state.sampling") : L("subtitle.state.idle");

    public bool HasSelectionIssue => !HasRegionSelection;

    public bool HasTranslationConfigured => !string.Equals(EffectiveProvider(_variables), TranslationProviders.Disabled, StringComparison.OrdinalIgnoreCase);

    public ObservableCollection<string> LogEntries { get; } = [];

    public string LogSummaryText => LogEntries.Count == 0
        ? L("subtitle.log.empty")
        : string.Join(Environment.NewLine, LogEntries.Take(8));

    public IAsyncRelayCommand StartCommand { get; }

    public IRelayCommand PauseCommand { get; }

    public IAsyncRelayCommand SelectRegionCommand { get; }

    public IAsyncRelayCommand ShowTranslationSettingsCommand { get; }

    public SubtitleTranslatorViewModel(
        string pluginId,
        SubtitleTranslatorVariables variables,
        ILocalizationService localizationService,
        IPluginVariableService pluginVariableService,
        IDesignerDialogService dialogService)
    {
        _pluginId = pluginId;
        _localizationService = localizationService;
        _pluginVariableService = pluginVariableService;
        _dialogService = dialogService;
        _pipelineService = new SubtitlePipelineService(localizationService);
        _variables = variables;
        ApplyVariables();
        StartCommand = new AsyncRelayCommand(StartWorkspaceAsync);
        PauseCommand = new RelayCommand(PauseWorkspace);
        SelectRegionCommand = new AsyncRelayCommand(SelectRegionAsync);
        ShowTranslationSettingsCommand = new AsyncRelayCommand(ShowTranslationSettingsAsync);

        LogEntries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(LogSummaryText));
        RefreshLocalization();
    }

    public void UpdateVariables(SubtitleTranslatorVariables variables)
    {
        _variables = variables;
        ApplyVariables();
        AddLog($"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.variablesUpdated")} {SampleIntervalMs} ms, {_variables.TargetLanguage}, {EffectiveProvider(_variables)}");
    }

    public void RefreshLocalization()
    {
        if (!HasRegionSelection)
            SelectedRegionText = L("subtitle.status.noRegion");

        if (string.IsNullOrWhiteSpace(CurrentSourceText) || CurrentSourceText == L("subtitle.initial.source"))
            CurrentSourceText = L("subtitle.initial.source");

        if (string.IsNullOrWhiteSpace(CurrentTranslatedText) || CurrentTranslatedText == L("subtitle.initial.translation"))
            CurrentTranslatedText = L("subtitle.initial.translation");

        if (string.IsNullOrWhiteSpace(StatusText) || StatusText == L("subtitle.initial.status"))
            StatusText = L("subtitle.initial.status");

        PipelineStateText = IsRunning ? L("subtitle.state.sampling") : HasRegionSelection ? L("subtitle.state.ready") : L("subtitle.state.idle");
        OcrStatusText = string.IsNullOrWhiteSpace(OcrStatusText) || OcrStatusText == L("subtitle.state.ocrNotInitialized")
            ? L("subtitle.state.ocrNotInitialized")
            : OcrStatusText;
        WorkspaceHint = ResolveDefaultWorkspaceHint();
        LastUpdatedText = ResolveDefaultLastUpdatedText();

        ApplyVariables();
        UpdateEmptyState();
        RaiseSummaryChanged();
    }

    partial void OnStatusTextChanged(string value) => RaiseSummaryChanged();

    partial void OnSelectedRegionTextChanged(string value) => RaiseSummaryChanged();

    partial void OnLastUpdatedTextChanged(string value) => RaiseSummaryChanged();

    partial void OnHasRegionSelectionChanged(bool value) => RaiseSummaryChanged();

    partial void OnIsRunningChanged(bool value) => RaiseSummaryChanged();

    partial void OnTranslationStatusTextChanged(string value) => RaiseSummaryChanged();

    partial void OnPipelineStateTextChanged(string value) => RaiseSummaryChanged();

    partial void OnOcrStatusTextChanged(string value) => RaiseSummaryChanged();

    [SupportedOSPlatform("windows")]
    private async Task StartWorkspaceAsync()
    {
        if (IsRunning)
            return;

        _runCts?.Cancel();
        _runCts?.Dispose();
        _runCts = new CancellationTokenSource();

        IsRunning = true;
        StatusText = L("subtitle.status.running");
        PipelineStateText = L("subtitle.state.sampling");
        CurrentTranslatedText = L("subtitle.translation.waiting");

        if (SelectedRegion is null || !SelectedRegion.IsValid)
            await SelectRegionAsync();

        if (SelectedRegion is null || !SelectedRegion.IsValid)
        {
            StatusText = L("subtitle.status.regionSelectionCancelled");
            PipelineStateText = L("subtitle.state.idle");
            IsRunning = false;
            return;
        }

        try
        {
            await _pipelineService.RunAsync(SelectedRegion!, ApplyPipelineUpdate, _runCts.Token);
        }
        catch (OperationCanceledException)
        {
            StatusText = L("subtitle.status.paused");
            PipelineStateText = L("subtitle.state.paused");
            WorkspaceHint = L("subtitle.hint.paused");
            AddLog($"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.capturePaused")}");
        }
        catch (Exception ex)
        {
            CurrentSourceText = L("subtitle.error.captureFailed");
            StatusText = L("subtitle.error.pipelineFailed");
            PipelineStateText = L("subtitle.state.error");
            LastUpdatedText = L("subtitle.state.error");
            WorkspaceHint = L("subtitle.hint.pipelineFailed");
            AddLog($"[{DateTime.Now:HH:mm:ss}] {L("subtitle.error.pipelineFailed")}: {ex.Message}");
        }
        finally
        {
            IsRunning = false;
        }
    }

    private void PauseWorkspace()
    {
        _runCts?.Cancel();
        IsRunning = false;
        StatusText = L("subtitle.status.paused");
        PipelineStateText = L("subtitle.state.paused");
        WorkspaceHint = L("subtitle.hint.manualPause");
    }

    [SupportedOSPlatform("windows")]
    private async Task SelectRegionAsync()
    {
        var region = await _regionSelectionService.SelectRegionAsync();
        if (region is null || !region.IsValid)
        {
            StatusText = L("subtitle.status.regionSelectionCancelled");
            if (!HasRegionSelection)
                PipelineStateText = L("subtitle.state.idle");
            UpdateEmptyState();
            AddLog($"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.regionSelectionCancelled")}");
            return;
        }

        SelectedRegion = region;
        HasRegionSelection = true;
        SelectedRegionText = SelectedRegion.ToString();
        StatusText = L("subtitle.status.regionSelected");
        PipelineStateText = IsRunning ? L("subtitle.state.sampling") : L("subtitle.state.ready");
        WorkspaceHint = L("subtitle.hint.regionSelected");
        UpdateEmptyState();
        AddLog($"[{DateTime.Now:HH:mm:ss}] {L("subtitle.log.regionSelected")}: {SelectedRegionText}");
    }

    public PluginPageHeaderModel CreatePageHeader() => new()
    {
        Context = StatusText,
        Metrics =
        [
            new PluginPageHeaderMetric
            {
                Label = L("subtitle.header.region"),
                Value = RegionStateText,
                Tone = HasRegionSelection ? PluginPageHeaderTone.Success : PluginPageHeaderTone.Warning
            },
            new PluginPageHeaderMetric
            {
                Label = L("subtitle.header.pipeline"),
                Value = PipelineStateText,
                Tone = IsRunning ? PluginPageHeaderTone.Accent : PluginPageHeaderTone.Neutral
            },
            new PluginPageHeaderMetric
            {
                Label = L("subtitle.header.interval"),
                Value = $"{SampleIntervalMs} ms",
                Tone = PluginPageHeaderTone.Neutral
            },
            new PluginPageHeaderMetric
            {
                Label = L("subtitle.header.translation"),
                Value = HasTranslationConfigured ? L("subtitle.header.translationConfigured") : L("subtitle.header.originalOnly"),
                Tone = HasTranslationConfigured ? PluginPageHeaderTone.Success : PluginPageHeaderTone.Warning
            }
        ],
        Badges =
        [
            new PluginPageHeaderBadge
            {
                Text = L("subtitle.badge.preview"),
                Tone = PluginPageHeaderTone.Accent
            },
            new PluginPageHeaderBadge
            {
                Text = HasRegionSelection ? SelectedRegionText : L("subtitle.badge.awaitingRegion"),
                Tone = PluginPageHeaderTone.Neutral
            },
            new PluginPageHeaderBadge
            {
                Text = TranslationStatusText,
                Tone = HasTranslationConfigured ? PluginPageHeaderTone.Success : PluginPageHeaderTone.Warning
            }
        ]
    };

    private void RaiseSummaryChanged()
    {
        OnPropertyChanged(nameof(HasSourceText));
        OnPropertyChanged(nameof(HasTranslatedText));
        OnPropertyChanged(nameof(RegionStateText));
        OnPropertyChanged(nameof(RunStateText));
        OnPropertyChanged(nameof(HasSelectionIssue));
        OnPropertyChanged(nameof(HasTranslationConfigured));
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddLog(string message)
    {
        LogEntries.Insert(0, message);
        while (LogEntries.Count > 24)
            LogEntries.RemoveAt(LogEntries.Count - 1);
    }

    private async Task ShowTranslationSettingsAsync()
    {
        var owner = TryGetOwnerWindow();
        if (owner is null)
            return;

        var sheetViewModel = new TranslationSettingsSheetViewModel(
            _pluginId,
            _variables,
            _pluginVariableService,
            _localizationService);

        await _dialogService.ShowSheetAsync(owner, new DesignerSheetViewModel
        {
            Title = L("subtitle.settings.title"),
            Description = L("subtitle.settings.description"),
            Content = new TranslationSettingsSheetView { DataContext = sheetViewModel },
            ConfirmText = L("dialog.close"),
            CancelText = string.Empty,
            DialogWidth = 920,
            DialogHeight = 720,
            LockSize = false
        });
    }

    private static string Summarize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "<empty>";

        var normalized = text.Replace(Environment.NewLine, " ").Trim();
        return normalized.Length <= 48 ? normalized : $"{normalized[..48]}...";
    }

    public void Dispose()
    {
        _runCts?.Cancel();
        _runCts?.Dispose();
        _pipelineService.Reset();
        _pipelineService.Dispose();
    }

    private void ApplyVariables()
    {
        SampleIntervalMs = Math.Clamp(_variables.CaptureIntervalMs, 150, 5000);
        TranslationStatusText = string.Equals(EffectiveProvider(_variables), TranslationProviders.Disabled, StringComparison.OrdinalIgnoreCase)
            ? L("subtitle.translation.notConfigured")
            : $"{L("subtitle.translation.providerPrefix")}: {EffectiveProvider(_variables)}";
        _pipelineService.UpdateVariables(_variables);
        UpdateEmptyState();
    }

    private static string EffectiveProvider(SubtitleTranslatorVariables variables)
        => string.IsNullOrWhiteSpace(variables.TranslationProvider)
            ? TranslationProviders.Disabled
            : variables.TranslationProvider;

    private void UpdateEmptyState()
    {
        if (!HasRegionSelection)
        {
            EmptyStateTitle = L("subtitle.empty.selectRegionTitle");
            EmptyStateDescription = L("subtitle.empty.selectRegionDescription");
            return;
        }

        if (string.Equals(EffectiveProvider(_variables), TranslationProviders.Disabled, StringComparison.OrdinalIgnoreCase))
        {
            EmptyStateTitle = L("subtitle.empty.originalOnlyTitle");
            EmptyStateDescription = L("subtitle.empty.originalOnlyDescription");
            return;
        }

        EmptyStateTitle = L("subtitle.empty.readyTitle");
        EmptyStateDescription = L("subtitle.empty.readyDescription");
    }

    private void ApplyPipelineUpdate(SubtitlePipelineUpdate update)
    {
        StatusText = update.StatusText;
        PipelineStateText = update.PipelineStateText;
        OcrStatusText = update.OcrStatusText;
        TranslationStatusText = update.TranslationStatusText;

        if (!string.IsNullOrWhiteSpace(update.SourceText))
            CurrentSourceText = update.SourceText;

        if (!string.IsNullOrWhiteSpace(update.TranslatedText))
            CurrentTranslatedText = update.TranslatedText;

        if (!string.IsNullOrWhiteSpace(update.LastUpdatedText))
            LastUpdatedText = update.LastUpdatedText;

        if (!string.IsNullOrWhiteSpace(update.WorkspaceHint))
            WorkspaceHint = update.WorkspaceHint;

        if (!string.IsNullOrWhiteSpace(update.LogMessage))
            AddLog(update.LogMessage);
    }

    private string L(string key) => _localizationService.GetString(key);

    private string ResolveDefaultWorkspaceHint()
    {
        if (IsRunning)
            return L("subtitle.hint.running");

        if (!HasRegionSelection)
            return L("subtitle.hint.selectRegion");

        return HasTranslationConfigured
            ? L("subtitle.hint.ready")
            : L("subtitle.hint.originalOnly");
    }

    private string ResolveDefaultLastUpdatedText()
        => string.IsNullOrWhiteSpace(LastUpdatedText) || LastUpdatedText == L("subtitle.initial.lastUpdate")
            ? L("subtitle.initial.lastUpdate")
            : LastUpdatedText;

    private static Window? TryGetOwnerWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;

        return null;
    }
}
