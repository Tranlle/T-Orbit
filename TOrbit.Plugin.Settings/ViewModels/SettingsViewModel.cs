using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using TOrbit.Core.Models;
using TOrbit.Core.Services;
using TOrbit.Designer.Models;
using TOrbit.Designer.Services;
using TOrbit.Plugin.Core.Enums;
using TOrbit.Plugin.Core.Models;

namespace TOrbit.Plugin.Settings.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly IAppPreferencesService _preferencesService;
    private readonly IPluginCatalogService _pluginCatalog;
    private readonly IPluginVariableService _variableService;
    private readonly IPluginValidationStatusService _validationStatusService;
    private readonly ObservableCollection<PluginVariableItemViewModel> _pluginVariableItems = [];
    private readonly ObservableCollection<PluginVariableGroupViewModel> _pluginVariableGroups = [];

    public event EventHandler? HeaderSummaryChanged;

    [ObservableProperty]
    private string appName = "T-Orbit";

    [ObservableProperty]
    private DesignerOptionItem? selectedLanguageOption;

    [ObservableProperty]
    private DesignerOptionItem? selectedPaletteOption;

    [ObservableProperty]
    private DesignerOptionItem? selectedAdvancedPaletteOption;

    [ObservableProperty]
    private DesignerOptionItem? selectedFontOption;

    [ObservableProperty]
    private bool showAdvancedThemeSettings;

    [ObservableProperty]
    private string customThemeDirectory = Path.Combine(AppContext.BaseDirectory, "themes");

    [ObservableProperty]
    private string workspaceRoot = AppContext.BaseDirectory;

    [ObservableProperty]
    private bool useWorkspaceForMigrations = true;

    [ObservableProperty]
    private bool minimizeToTrayOnClose = true;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAddFormKeyHints))]
    private ObservableCollection<DesignerOptionItem> addFormKeyHints = [];

    [ObservableProperty]
    private bool showAddVariableForm;

    [ObservableProperty]
    private DesignerOptionItem? addFormSelectedPlugin;

    [ObservableProperty]
    private string addFormKey = string.Empty;

    [ObservableProperty]
    private string addFormValue = string.Empty;

    public ObservableCollection<DesignerOptionItem> LanguageOptions { get; } = [];
    public ObservableCollection<DesignerOptionItem> FontOptions { get; } = [];
    public ObservableCollection<DesignerOptionItem> PaletteOptions { get; } = [];
    public ObservableCollection<DesignerOptionItem> AdvancedPaletteOptions { get; } = [];
    public ObservableCollection<DesignerOptionItem> PluginOptions { get; } = [];
    public IReadOnlyList<PluginVariableGroupViewModel> PluginVariableGroups => _pluginVariableGroups;
    public int PluginCount => _pluginCatalog.Plugins.Count;
    public int VariableCount => _pluginVariableItems.Count;
    public int VariablePluginCount => _pluginVariableGroups.Count;
    public int ValidationIssuePluginCount => _pluginCatalog.Plugins.Count(plugin =>
        _validationStatusService.Get(plugin.Id, plugin.Name).HasIssues);

    public bool IsInterFontWarningVisible => SelectedFontOption?.Key == "inter";
    public string FontWarningMessage => _localizationService.GetString("settings.messages.interWarning");
    public bool HasPluginVariables => _pluginVariableItems.Count > 0;
    public bool HasAddFormKeyHints => AddFormKeyHints.Count > 0;
    public string PluginVariableSummary => _pluginVariableItems.Count == 0
        ? _localizationService.GetString("settings.sections.variables.empty")
        : $"{_pluginVariableItems.Count} / {_pluginVariableItems.Select(x => x.PluginId).Distinct().Count()}";

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand ResetCommand { get; }
    public IRelayCommand ShowAddVariableFormCommand { get; }
    public IRelayCommand AddVariableCommand { get; }
    public IRelayCommand CancelAddVariableCommand { get; }
    public IRelayCommand<DesignerOptionItem> FillKeyFromHintCommand { get; }

    public SettingsViewModel(
        IAppShellService shellService,
        IThemeService themeService,
        ILocalizationService localizationService,
        IAppPreferencesService preferencesService,
        IPluginCatalogService pluginCatalog,
        IPluginVariableService variableService,
        IPluginValidationStatusService validationStatusService)
    {
        _themeService = themeService;
        _localizationService = localizationService;
        _preferencesService = preferencesService;
        _pluginCatalog = pluginCatalog;
        _variableService = variableService;
        _validationStatusService = validationStatusService;
        _localizationService.LanguageChanged += LocalizationServiceOnLanguageChanged;

        var preferences = _preferencesService.Load();

        InitializeLanguageOptions();
        InitializeFontOptions();
        InitializePaletteOptions(themeService);
        InitializePluginOptions();
        LoadPluginVariables();

        AppName = shellService.AppName;
        WorkspaceRoot = shellService.WorkspaceRoot;
        SelectedLanguageOption = LanguageOptions.FirstOrDefault(option => option.Key == preferences.LanguageCode)
            ?? LanguageOptions.FirstOrDefault(option => option.Key == _localizationService.CurrentLanguageCode)
            ?? LanguageOptions.FirstOrDefault();
        SelectedFontOption = FontOptions.FirstOrDefault(option => option.Key == preferences.FontOptionKey)
            ?? FontOptions.FirstOrDefault(option => option.Key == themeService.CurrentFontOptionKey)
            ?? FontOptions.FirstOrDefault();
        SelectedPaletteOption = PaletteOptions.FirstOrDefault(option => option.Key == themeService.CurrentPaletteKey)
            ?? PaletteOptions.FirstOrDefault();
        SelectedAdvancedPaletteOption = AdvancedPaletteOptions.FirstOrDefault(option => option.Key == themeService.CurrentPaletteKey);
        MinimizeToTrayOnClose = preferences.CloseButtonBehavior == CloseButtonBehavior.MinimizeToTray;
        StatusMessage = _localizationService.GetString("settings.messages.saved");

        PublishPluginValidationStates();

        SaveCommand = new RelayCommand(() =>
        {
            var paletteKey = ShowAdvancedThemeSettings
                ? SelectedAdvancedPaletteOption?.Key ?? SelectedPaletteOption?.Key ?? _themeService.CurrentPaletteKey
                : SelectedPaletteOption?.Key ?? _themeService.CurrentPaletteKey;
            var fontOptionKey = SelectedFontOption?.Key ?? "system";
            var languageCode = SelectedLanguageOption?.Key ?? _localizationService.CurrentLanguageCode;

            _themeService.SetPalette(paletteKey);
            _themeService.SetFontOption(fontOptionKey);
            _localizationService.SetLanguage(languageCode);

            var appliedPaletteKey = _themeService.CurrentPaletteKey;
            SelectedPaletteOption = PaletteOptions.FirstOrDefault(option => option.Key == appliedPaletteKey) ?? SelectedPaletteOption;
            SelectedAdvancedPaletteOption = AdvancedPaletteOptions.FirstOrDefault(option => option.Key == appliedPaletteKey);

            _preferencesService.Save(new AppPreferences
            {
                LanguageCode = languageCode,
                FontOptionKey = fontOptionKey,
                PaletteKey = appliedPaletteKey,
                CloseButtonBehavior = MinimizeToTrayOnClose ? CloseButtonBehavior.MinimizeToTray : CloseButtonBehavior.Exit
            });

            SavePluginVariables();
            PublishPluginValidationStates();

            StatusMessage = _localizationService.GetString("settings.messages.saved");
            HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
        });

        ResetCommand = new RelayCommand(() =>
        {
            AppName = shellService.AppName;
            SelectedLanguageOption = LanguageOptions.FirstOrDefault(option => option.Key == "zh-CN") ?? LanguageOptions.FirstOrDefault();
            SelectedPaletteOption = PaletteOptions.FirstOrDefault(option => option.Key == "torbit-dark") ?? PaletteOptions.FirstOrDefault();
            SelectedAdvancedPaletteOption = null;
            SelectedFontOption = FontOptions.FirstOrDefault(option => option.Key == "system") ?? FontOptions.FirstOrDefault();
            MinimizeToTrayOnClose = true;
            ShowAdvancedThemeSettings = false;
            WorkspaceRoot = shellService.WorkspaceRoot;
            UseWorkspaceForMigrations = true;

            _themeService.SetPalette(SelectedPaletteOption?.Key ?? "torbit-dark");
            _themeService.SetFontOption("system");
            _localizationService.SetLanguage("zh-CN");
            _preferencesService.Save(new AppPreferences
            {
                LanguageCode = "zh-CN",
                FontOptionKey = "system",
                PaletteKey = SelectedPaletteOption?.Key ?? "torbit-dark",
                CloseButtonBehavior = CloseButtonBehavior.MinimizeToTray
            });

            PublishPluginValidationStates();
            StatusMessage = _localizationService.GetString("settings.messages.reset");
            HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
        });

        ShowAddVariableFormCommand = new RelayCommand(() =>
        {
            InitializePluginOptions();
            AddFormSelectedPlugin = PluginOptions.FirstOrDefault();
            AddFormKey = string.Empty;
            AddFormValue = string.Empty;
            ShowAddVariableForm = true;
        });

        AddVariableCommand = new RelayCommand(() =>
        {
            var pluginId = AddFormSelectedPlugin?.Key ?? string.Empty;
            var key = AddFormKey.Trim();
            var value = AddFormValue;

            if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(key))
                return;

            var existing = _pluginVariableItems.FirstOrDefault(item =>
                string.Equals(item.PluginId, pluginId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                existing.Value = value;
            }
            else
            {
                var pluginName = AddFormSelectedPlugin?.Label ?? pluginId;
                var hint = AddFormKeyHints.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
                var hintData = hint?.Value as KeyHintData;

                AddPluginVariableItem(new PluginVariableItemViewModel(
                    pluginId: pluginId,
                    pluginName: pluginName,
                    key: key,
                    value: value,
                    defaultValue: hintData?.DefaultValue ?? string.Empty,
                    description: hintData?.Description ?? string.Empty,
                    isFromMetadata: hint is not null,
                    isEncrypted: hintData?.IsEncrypted ?? false,
                    onDelete: RemovePluginVariable));

                SyncPluginVariableGroups();
                PublishPluginValidationStates();
            }

            ShowAddVariableForm = false;
            AddFormKey = string.Empty;
            AddFormValue = string.Empty;
            OnPropertyChanged(nameof(PluginVariableSummary));
        });

        CancelAddVariableCommand = new RelayCommand(() =>
        {
            ShowAddVariableForm = false;
            AddFormKey = string.Empty;
            AddFormValue = string.Empty;
        });

        FillKeyFromHintCommand = new RelayCommand<DesignerOptionItem>(hint =>
        {
            if (hint is null)
                return;

            AddFormKey = hint.Key;
            AddFormValue = hint.Value is KeyHintData data ? data.DefaultValue : hint.Value as string ?? string.Empty;
        });
    }

    partial void OnSelectedLanguageOptionChanged(DesignerOptionItem? value) => HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    partial void OnSelectedFontOptionChanged(DesignerOptionItem? value)
    {
        OnPropertyChanged(nameof(IsInterFontWarningVisible));
        OnPropertyChanged(nameof(FontWarningMessage));
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnSelectedPaletteOptionChanged(DesignerOptionItem? value) => HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    partial void OnSelectedAdvancedPaletteOptionChanged(DesignerOptionItem? value) => HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    partial void OnShowAdvancedThemeSettingsChanged(bool value) => HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    partial void OnWorkspaceRootChanged(string value) => HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    partial void OnMinimizeToTrayOnCloseChanged(bool value) => HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);

    partial void OnAddFormSelectedPluginChanged(DesignerOptionItem? value)
    {
        AddFormKeyHints.Clear();
        if (value is null)
            return;

        var entry = _pluginCatalog.Plugins.FirstOrDefault(plugin => plugin.Id == value.Key);
        var definitions = entry?.Plugin.Descriptor.VariableDefinitions;
        if (definitions is null)
            return;

        foreach (var definition in definitions)
        {
            AddFormKeyHints.Add(new DesignerOptionItem
            {
                Key = definition.Key,
                Label = string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.Key : definition.DisplayName,
                Description = definition.Description,
                Value = new KeyHintData(definition.DefaultValue, definition.Description, definition.IsEncrypted)
            });
        }

        OnPropertyChanged(nameof(HasAddFormKeyHints));
    }

    private void LocalizationServiceOnLanguageChanged(object? sender, EventArgs e)
    {
        var selectedLanguage = SelectedLanguageOption?.Key;
        var selectedPalette = SelectedPaletteOption?.Key;
        var selectedAdvancedPalette = SelectedAdvancedPaletteOption?.Key;
        var selectedFont = SelectedFontOption?.Key;

        InitializeLanguageOptions();
        InitializeFontOptions();
        InitializePaletteOptions(_themeService);
        InitializePluginOptions();

        SelectedLanguageOption = LanguageOptions.FirstOrDefault(option => option.Key == selectedLanguage)
            ?? LanguageOptions.FirstOrDefault(option => option.Key == _localizationService.CurrentLanguageCode)
            ?? LanguageOptions.FirstOrDefault();
        SelectedPaletteOption = PaletteOptions.FirstOrDefault(option => option.Key == selectedPalette) ?? PaletteOptions.FirstOrDefault();
        SelectedAdvancedPaletteOption = AdvancedPaletteOptions.FirstOrDefault(option => option.Key == selectedAdvancedPalette);
        SelectedFontOption = FontOptions.FirstOrDefault(option => option.Key == selectedFont) ?? FontOptions.FirstOrDefault();
        StatusMessage = _localizationService.GetString("settings.messages.saved");
        OnPropertyChanged(nameof(FontWarningMessage));
        OnPropertyChanged(nameof(PluginVariableSummary));
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddPluginVariableItem(PluginVariableItemViewModel item)
    {
        item.PropertyChanged += PluginVariableItemChanged;
        _pluginVariableItems.Add(item);
    }

    private void RemovePluginVariable(PluginVariableItemViewModel item)
    {
        item.PropertyChanged -= PluginVariableItemChanged;
        _pluginVariableItems.Remove(item);
        SyncPluginVariableGroups();
        PublishPluginValidationStates();
    }

    private void PluginVariableItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PluginVariableItemViewModel.Value))
            return;

        PublishPluginValidationStates();
    }

    private void LoadPluginVariables()
    {
        foreach (var item in _pluginVariableItems)
            item.PropertyChanged -= PluginVariableItemChanged;

        _pluginVariableItems.Clear();
        var store = _variableService.Load();

        foreach (var entry in store.Entries)
        {
            var plugin = _pluginCatalog.Plugins.FirstOrDefault(item => item.Id == entry.PluginId);
            var pluginName = plugin?.Name ?? entry.PluginId;
            var definition = plugin?.Plugin.Descriptor.VariableDefinitions?.FirstOrDefault(item => item.Key == entry.Key);

            var displayValue = entry.IsEncrypted
                ? _variableService.GetValue(entry.PluginId, entry.Key) ?? entry.Value
                : entry.Value;

            AddPluginVariableItem(new PluginVariableItemViewModel(
                pluginId: entry.PluginId,
                pluginName: pluginName,
                key: entry.Key,
                value: displayValue,
                defaultValue: definition?.DefaultValue ?? string.Empty,
                description: definition?.Description ?? string.Empty,
                isFromMetadata: definition is not null,
                isEncrypted: definition?.IsEncrypted ?? entry.IsEncrypted,
                onDelete: RemovePluginVariable));
        }

        var loadedKeys = _pluginVariableItems
            .Select(item => (item.PluginId, item.Key.ToLowerInvariant()))
            .ToHashSet();

        foreach (var pluginEntry in _pluginCatalog.Plugins)
        {
            var definitions = pluginEntry.Plugin.Descriptor.VariableDefinitions;
            if (definitions is null or { Count: 0 })
                continue;

            foreach (var definition in definitions)
            {
                if (!loadedKeys.Add((pluginEntry.Id, definition.Key.ToLowerInvariant())))
                    continue;

                AddPluginVariableItem(new PluginVariableItemViewModel(
                    pluginId: pluginEntry.Id,
                    pluginName: pluginEntry.Name,
                    key: definition.Key,
                    value: definition.DefaultValue,
                    defaultValue: definition.DefaultValue,
                    description: definition.Description,
                    isFromMetadata: true,
                    isEncrypted: definition.IsEncrypted,
                    onDelete: RemovePluginVariable));
            }
        }

        SyncPluginVariableGroups();
    }

    private void SyncPluginVariableGroups()
    {
        var groupedItems = _pluginVariableItems
            .GroupBy(item => item.PluginId)
            .OrderBy(group => group.First().PluginName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var activeIds = groupedItems.Select(group => group.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var staleGroup in _pluginVariableGroups.Where(group => !activeIds.Contains(group.PluginId)).ToList())
            _pluginVariableGroups.Remove(staleGroup);

        for (var groupIndex = 0; groupIndex < groupedItems.Count; groupIndex++)
        {
            var groupedItem = groupedItems[groupIndex];
            var group = _pluginVariableGroups.FirstOrDefault(item => item.PluginId == groupedItem.Key);

            if (group is null)
            {
                group = new PluginVariableGroupViewModel
                {
                    PluginId = groupedItem.Key,
                    PluginName = groupedItem.First().PluginName,
                    CapabilityTags = _pluginCatalog.Plugins
                        .FirstOrDefault(plugin => string.Equals(plugin.Id, groupedItem.Key, StringComparison.OrdinalIgnoreCase))?
                        .Capabilities
                        .Select(FormatCapability)
                        .ToArray() ?? []
                };
                _pluginVariableGroups.Insert(groupIndex, group);
            }
            else
            {
                var currentGroupIndex = _pluginVariableGroups.IndexOf(group);
                if (currentGroupIndex != groupIndex)
                    _pluginVariableGroups.Move(currentGroupIndex, groupIndex);
            }

            var desiredVariables = groupedItem.OrderBy(item => item.Key, StringComparer.CurrentCultureIgnoreCase).ToList();
            var desiredKeys = desiredVariables.Select(item => item.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var staleVariable in group.Variables.Where(item => !desiredKeys.Contains(item.Key)).ToList())
                group.Variables.Remove(staleVariable);

            for (var variableIndex = 0; variableIndex < desiredVariables.Count; variableIndex++)
            {
                var variable = desiredVariables[variableIndex];
                var currentVariableIndex = group.Variables.IndexOf(variable);

                if (currentVariableIndex < 0)
                {
                    group.Variables.Insert(variableIndex, variable);
                    continue;
                }

                if (currentVariableIndex != variableIndex)
                    group.Variables.Move(currentVariableIndex, variableIndex);
            }
        }

        OnPropertyChanged(nameof(PluginVariableGroups));
        OnPropertyChanged(nameof(HasPluginVariables));
        OnPropertyChanged(nameof(PluginVariableSummary));
        OnPropertyChanged(nameof(VariableCount));
        OnPropertyChanged(nameof(VariablePluginCount));
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void PublishPluginValidationStates()
    {
        foreach (var plugin in _pluginCatalog.Plugins.OrderBy(item => item.Name))
        {
            var definitions = plugin.Plugin.Descriptor.VariableDefinitions;
            if (definitions is null or { Count: 0 })
            {
                _validationStatusService.Clear(plugin.Id, plugin.Name);
                continue;
            }

            var values = _pluginVariableItems
                .Where(item => string.Equals(item.PluginId, plugin.Id, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);

            var messages = ValidatePluginVariables(plugin.Name, definitions, values).ToArray();
            _validationStatusService.Set(plugin.Id, plugin.Name, messages);
        }

        OnPropertyChanged(nameof(ValidationIssuePluginCount));
        HeaderSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SavePluginVariables()
    {
        var store = new PluginVariableStore
        {
            Entries = _pluginVariableItems
                .Select(item => new PluginVariableEntry
                {
                    PluginId = item.PluginId,
                    Key = item.Key,
                    Value = item.Value,
                    IsEncrypted = item.IsEncrypted
                })
                .ToList()
        };

        _variableService.Save(store);
        _variableService.InjectAll();
    }

    private void InitializeLanguageOptions()
    {
        LanguageOptions.Clear();
        foreach (var option in _localizationService.GetSupportedLanguages())
            LanguageOptions.Add(new DesignerOptionItem { Key = option.Key, Label = option.Label, Description = option.Description });
    }

    private void InitializePluginOptions()
    {
        PluginOptions.Clear();
        foreach (var plugin in _pluginCatalog.Plugins.OrderBy(item => item.Name))
        {
            PluginOptions.Add(new DesignerOptionItem
            {
                Key = plugin.Id,
                Label = plugin.Name,
                Description = plugin.Description
            });
        }
    }

    private void InitializePaletteOptions(IThemeService themeService)
    {
        PaletteOptions.Clear();
        AdvancedPaletteOptions.Clear();

        foreach (var palette in themeService.GetAvailablePalettes())
        {
            var option = new DesignerOptionItem
            {
                Key = palette.Key,
                Label = palette.Label,
                Description = palette.Description,
                Value = palette
            };

            if (palette.IsBuiltIn)
                PaletteOptions.Add(option);
            else
                AdvancedPaletteOptions.Add(option);
        }
    }

    private void InitializeFontOptions()
    {
        FontOptions.Clear();
        FontOptions.Add(CreateFontOption("system"));
        FontOptions.Add(CreateFontOption("inter"));

        if (OperatingSystem.IsWindows())
        {
            FontOptions.Add(CreateFontOption("segoe-ui"));
            FontOptions.Add(CreateFontOption("microsoft-yahei-ui"));
            FontOptions.Add(CreateFontOption("arial"));
            FontOptions.Add(CreateFontOption("bahnschrift"));
        }
    }

    private DesignerOptionItem CreateFontOption(string key)
        => new()
        {
            Key = key,
            Label = _localizationService.GetString($"settings.fonts.{key}.label"),
            Description = _localizationService.GetString($"settings.fonts.{key}.description")
        };

    private IReadOnlyList<string> ValidatePluginVariables(
        string pluginName,
        IReadOnlyList<PluginVariableDefinition> definitions,
        IReadOnlyDictionary<string, string> values)
    {
        var messages = new List<string>();

        foreach (var definition in definitions)
        {
            values.TryGetValue(definition.Key, out var value);
            var normalizedValue = value?.Trim() ?? string.Empty;
            var displayName = string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.Key : definition.DisplayName;

            if (definition.IsRequired && string.IsNullOrWhiteSpace(normalizedValue))
            {
                messages.Add(string.Format(_localizationService.GetString("settings.validation.required"), pluginName, displayName));
                continue;
            }

            if (string.IsNullOrWhiteSpace(normalizedValue))
                continue;

            if (definition.AllowedValues is { Count: > 0 }
                && !definition.AllowedValues.Contains(normalizedValue, StringComparer.OrdinalIgnoreCase))
            {
                messages.Add(string.Format(
                    _localizationService.GetString("settings.validation.allowedValues"),
                    pluginName,
                    displayName,
                    string.Join(" / ", definition.AllowedValues)));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(definition.ValidationPattern)
                && !System.Text.RegularExpressions.Regex.IsMatch(normalizedValue, definition.ValidationPattern, System.Text.RegularExpressions.RegexOptions.CultureInvariant))
            {
                messages.Add(string.Format(
                    _localizationService.GetString("settings.validation.invalid"),
                    pluginName,
                    displayName,
                    string.IsNullOrWhiteSpace(definition.ValidationMessage)
                        ? _localizationService.GetString("settings.validation.invalidDefault")
                        : definition.ValidationMessage));
            }
        }

        return messages;
    }

    private sealed record KeyHintData(string DefaultValue, string Description, bool IsEncrypted);

    private static string FormatCapability(PluginCapability capability) => capability switch
    {
        PluginCapability.FileSystem => "FileSystem",
        PluginCapability.LocalProcess => "LocalProcess",
        PluginCapability.Network => "Network",
        PluginCapability.Secrets => "Secrets",
        _ => capability.ToString()
    };
}
