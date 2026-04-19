namespace TOrbit.Core.Models;

public sealed class AppPreferences
{
    public string LanguageCode { get; set; } = "zh-CN";

    public string FontOptionKey { get; set; } = "system";

    public string ThemeKey { get; set; } = "Dark";

    public string PaletteKey { get; set; } = "tranbok-dark";

    public CloseButtonBehavior CloseButtonBehavior { get; set; } = CloseButtonBehavior.MinimizeToTray;
}

public enum CloseButtonBehavior
{
    MinimizeToTray = 0,
    Exit = 1
}
