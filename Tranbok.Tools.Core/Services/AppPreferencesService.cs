using Tranbok.Tools.Core.Models;

namespace Tranbok.Tools.Core.Services;

public sealed class AppPreferencesService : IAppPreferencesService
{
    // 作用域常量供 StorageService 迁移时引用
    internal const string StorageScope  = "tranbok.app";
    internal const string KeyFontOption = "fontOptionKey";

    private readonly IStorageService _storage;

    public AppPreferencesService(IStorageService storage) => _storage = storage;

    public AppPreferences Load() => new()
    {
        FontOptionKey = _storage.GetKv(StorageScope, KeyFontOption) ?? "system"
    };

    public void Save(AppPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        _storage.SetKv(StorageScope, KeyFontOption, preferences.FontOptionKey);
    }
}
