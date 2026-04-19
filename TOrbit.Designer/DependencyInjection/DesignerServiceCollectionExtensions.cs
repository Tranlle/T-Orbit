using Microsoft.Extensions.DependencyInjection;
using TOrbit.Designer.Abstractions;
using TOrbit.Designer.Services;

namespace TOrbit.Designer.DependencyInjection;

public static class DesignerServiceCollectionExtensions
{
    public static IServiceCollection AddTOrbitDesigner(this IServiceCollection services)
    {
        services.AddSingleton<IThemePaletteProvider, BuiltInThemePaletteProvider>();
        services.AddSingleton<IThemePaletteProvider>(_ => new JsonThemePaletteProvider(Path.Combine(AppContext.BaseDirectory, "themes")));
        services.AddSingleton<ThemePaletteRegistry>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ILocalizationService>(_ => LocalizationService.Shared);
        services.AddSingleton<IDesignerDialogService, DesignerDialogService>();
        return services;
    }
}
