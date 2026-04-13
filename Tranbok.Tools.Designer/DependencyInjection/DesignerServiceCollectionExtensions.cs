using Microsoft.Extensions.DependencyInjection;
using Tranbok.Tools.Designer.Abstractions;
using Tranbok.Tools.Designer.Services;

namespace Tranbok.Tools.Designer.DependencyInjection;

public static class DesignerServiceCollectionExtensions
{
    public static IServiceCollection AddTranbokDesigner(this IServiceCollection services)
    {
        services.AddSingleton<IThemePaletteProvider, BuiltInThemePaletteProvider>();
        services.AddSingleton<IThemePaletteProvider>(_ => new JsonThemePaletteProvider(Path.Combine(AppContext.BaseDirectory, "themes")));
        services.AddSingleton<ThemePaletteRegistry>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IDesignerDialogService, DesignerDialogService>();
        return services;
    }
}