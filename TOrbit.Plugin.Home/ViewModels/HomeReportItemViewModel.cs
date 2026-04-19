using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TOrbit.Designer.Services;
using TOrbit.Designer.Views;
using TOrbit.Designer.ViewModels.Pages;
using TOrbit.Plugin.Core.Base;
using TOrbit.Plugin.Core.Models;

namespace TOrbit.Plugin.Home.ViewModels;

public sealed partial class HomeReportItemViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isLoaded;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private Control? content;

    public HomeReportDefinition Definition { get; }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasContent => Content is not null;
    public bool IsPending => !IsLoading && !HasContent && !HasError;
    public IAsyncRelayCommand LoadCommand { get; }

    public HomeReportItemViewModel(HomeReportDefinition definition)
    {
        Definition = definition;
        LoadCommand = new AsyncRelayCommand(EnsureLoadedAsync, () => !IsLoading && !IsLoaded);
    }

    public async Task EnsureLoadedAsync()
    {
        if (IsLoading || IsLoaded || Definition.ViewFactory is null)
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var view = await Definition.ViewFactory(CancellationToken.None);
            Content = ResolveReportView(view, Definition);
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            LoadCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(HasContent));
            OnPropertyChanged(nameof(IsPending));
        }
    }

    private static Control ResolveReportView(object view, HomeReportDefinition definition)
        => view switch
        {
            Control control => control,
            PluginDefaultViewModel model => new DefaultPluginView
            {
                DataContext = new DefaultPluginViewModel(model.Title, model.Description)
            },
            string text => new TextBlock
            {
                Text = text,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            },
            _ => new TextBlock
            {
                Text = string.Format(LocalizationService.Current?.GetString("home.report.emptyContent") ?? "{0} has no content to display.", definition.Title),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            }
        };
}
