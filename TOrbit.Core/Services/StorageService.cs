using System.Text.Json;
using Microsoft.Data.Sqlite;
using TOrbit.Core.Models;

namespace TOrbit.Core.Services;

/// <summary>
/// <see cref="IStorageService"/> 的 SQLite 实现（Singleton）。
/// <para>
/// 读写锁：<see cref="ReaderWriterLockSlim"/> 允许并发读、独占写。
/// 热路径（GetKv / SetKv）加了 write-through 内存缓存，命中时不再访问磁盘。
/// 启动时自动完成 schema 初始化和旧文件的一次性迁移。
/// </para>
/// </summary>
public sealed class StorageService : IStorageService, IDisposable
{
    private const string DiagnosticsSource = "StorageMigration";
    // ── 旧文件名（迁移用）────────────────────────────────────────────────────
    private const string LegacyKeyMapFile       = "keymap-bindings.json";
    private const string LegacyPreferencesFile  = "app-preferences.json";
    private const string LegacyVariablesFile    = "plugin-variables.json";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly SqliteConnection _conn;
    private readonly ReaderWriterLockSlim _rwLock = new(LockRecursionPolicy.NoRecursion);
    private readonly IAppDiagnosticsService? _diagnostics;

    // write-through 缓存：仅覆盖 GetKv / SetKv / DeleteKv 路径
    // 值为 null 表示 DB 里该行确实是 NULL；不在字典里表示尚未加载
    private readonly Dictionary<(string scope, string key), string?> _kvCache = [];

    // ── 构造：开连接、建表、迁移 ─────────────────────────────────────────────

    public StorageService()
        : this(null)
    {
    }

    public StorageService(IAppDiagnosticsService? diagnostics)
    {
        _diagnostics = diagnostics;
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "T-Orbit");
        Directory.CreateDirectory(dir);

        var dbPath = Path.Combine(dir, "t-orbit.db");
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();

        InitSchema();
        MigrateFromFiles();
    }

    // ── Schema ────────────────────────────────────────────────────────────────

    private void InitSchema()
    {
        Exec("""
            PRAGMA journal_mode=WAL;
            PRAGMA synchronous=NORMAL;

            CREATE TABLE IF NOT EXISTS kv_store (
                scope        TEXT    NOT NULL,
                key          TEXT    NOT NULL,
                value        TEXT,
                is_encrypted INTEGER NOT NULL DEFAULT 0,
                updated_at   INTEGER NOT NULL DEFAULT (unixepoch()),
                PRIMARY KEY (scope, key)
            );

            CREATE TABLE IF NOT EXISTS keymap_bindings (
                id         TEXT    NOT NULL PRIMARY KEY,
                custom_key TEXT,
                is_enabled INTEGER NOT NULL DEFAULT 1,
                updated_at INTEGER NOT NULL DEFAULT (unixepoch())
            );
            """);
    }

    // ── KV 同步 ───────────────────────────────────────────────────────────────

    public string? GetKv(string scope, string key)
    {
        var cacheKey = (scope, key);

        // 1. 读锁快速检查缓存
        _rwLock.EnterReadLock();
        try
        {
            if (_kvCache.TryGetValue(cacheKey, out var cached))
                return cached;
        }
        finally { _rwLock.ExitReadLock(); }

        // 2. 缓存未命中 → 写锁查 DB 并写缓存（双重检查）
        _rwLock.EnterWriteLock();
        try
        {
            if (_kvCache.TryGetValue(cacheKey, out var cached))
                return cached;

            using var cmd = Cmd("SELECT value FROM kv_store WHERE scope=@s AND key=@k");
            cmd.Parameters.AddWithValue("@s", scope);
            cmd.Parameters.AddWithValue("@k", key);
            var result = cmd.ExecuteScalar();
            var value  = result is DBNull or null ? null : (string)result;
            _kvCache[cacheKey] = value;
            return value;
        }
        finally { _rwLock.ExitWriteLock(); }
    }

    public void SetKv(string scope, string key, string? value)
        => SetKvWithMeta(scope, key, value, isEncrypted: false);

    public void DeleteKv(string scope, string key)
    {
        _rwLock.EnterWriteLock();
        try
        {
            using var cmd = Cmd("DELETE FROM kv_store WHERE scope=@s AND key=@k");
            cmd.Parameters.AddWithValue("@s", scope);
            cmd.Parameters.AddWithValue("@k", key);
            cmd.ExecuteNonQuery();
            _kvCache.Remove((scope, key));
        }
        finally { _rwLock.ExitWriteLock(); }
    }

    public IReadOnlyDictionary<string, string?> GetAllKv(string scope)
    {
        _rwLock.EnterWriteLock(); // 写锁：顺便填充缓存
        try
        {
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            using var cmd = Cmd("SELECT key, value FROM kv_store WHERE scope=@s");
            cmd.Parameters.AddWithValue("@s", scope);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var k = reader.GetString(0);
                var v = reader.IsDBNull(1) ? null : reader.GetString(1);
                dict[k] = v;
                _kvCache[(scope, k)] = v;
            }
            return dict;
        }
        finally { _rwLock.ExitWriteLock(); }
    }

    // ── KV 带加密标记 ─────────────────────────────────────────────────────────

    public void SetKvWithMeta(string scope, string key, string? value, bool isEncrypted)
    {
        _rwLock.EnterWriteLock();
        try
        {
            using var cmd = Cmd("""
                INSERT INTO kv_store (scope, key, value, is_encrypted, updated_at)
                VALUES (@s, @k, @v, @e, unixepoch())
                ON CONFLICT(scope, key) DO UPDATE SET
                    value        = excluded.value,
                    is_encrypted = excluded.is_encrypted,
                    updated_at   = excluded.updated_at
                """);
            cmd.Parameters.AddWithValue("@s", scope);
            cmd.Parameters.AddWithValue("@k", key);
            cmd.Parameters.AddWithValue("@v", (object?)value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@e", isEncrypted ? 1 : 0);
            cmd.ExecuteNonQuery();
            _kvCache[(scope, key)] = value;
        }
        finally { _rwLock.ExitWriteLock(); }
    }

    public void SetKvBatch(IEnumerable<(string scope, string key, string? value, bool isEncrypted)> entries)
    {
        _rwLock.EnterWriteLock();
        try
        {
            using var tx  = _conn.BeginTransaction();
            using var cmd = Cmd("""
                INSERT INTO kv_store (scope, key, value, is_encrypted, updated_at)
                VALUES (@s, @k, @v, @e, unixepoch())
                ON CONFLICT(scope, key) DO UPDATE SET
                    value        = excluded.value,
                    is_encrypted = excluded.is_encrypted,
                    updated_at   = excluded.updated_at
                """);
            cmd.Transaction = tx;
            var pS = cmd.Parameters.Add("@s", SqliteType.Text);
            var pK = cmd.Parameters.Add("@k", SqliteType.Text);
            var pV = cmd.Parameters.Add("@v", SqliteType.Text);
            var pE = cmd.Parameters.Add("@e", SqliteType.Integer);
            cmd.Prepare();

            foreach (var (scope, key, value, isEncrypted) in entries)
            {
                pS.Value = scope;
                pK.Value = key;
                pV.Value = (object?)value ?? DBNull.Value;
                pE.Value = isEncrypted ? 1 : 0;
                cmd.ExecuteNonQuery();
                _kvCache[(scope, key)] = value;
            }
            tx.Commit();
        }
        finally { _rwLock.ExitWriteLock(); }
    }

    public KvEntry? GetKvWithMeta(string scope, string key)
    {
        _rwLock.EnterReadLock();
        try
        {
            using var cmd = Cmd(
                "SELECT key, value, is_encrypted FROM kv_store WHERE scope=@s AND key=@k");
            cmd.Parameters.AddWithValue("@s", scope);
            cmd.Parameters.AddWithValue("@k", key);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            return new KvEntry(
                Key:         reader.GetString(0),
                Value:       reader.IsDBNull(1) ? null : reader.GetString(1),
                IsEncrypted: reader.GetInt32(2) == 1);
        }
        finally { _rwLock.ExitReadLock(); }
    }

    public IReadOnlyList<KvEntry> GetAllKvWithMeta(string scope)
    {
        _rwLock.EnterReadLock();
        try
        {
            var list = new List<KvEntry>();
            using var cmd = Cmd(
                "SELECT key, value, is_encrypted FROM kv_store WHERE scope=@s");
            cmd.Parameters.AddWithValue("@s", scope);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new KvEntry(
                    Key:         reader.GetString(0),
                    Value:       reader.IsDBNull(1) ? null : reader.GetString(1),
                    IsEncrypted: reader.GetInt32(2) == 1));
            }
            return list;
        }
        finally { _rwLock.ExitReadLock(); }
    }

    public IReadOnlyDictionary<string, IReadOnlyList<KvEntry>> GetAllKvWithMetaByPrefix(string scopePrefix)
    {
        _rwLock.EnterReadLock();
        try
        {
            var result = new Dictionary<string, List<KvEntry>>(StringComparer.OrdinalIgnoreCase);
            using var cmd = Cmd(
                "SELECT scope, key, value, is_encrypted FROM kv_store WHERE scope LIKE @prefix ESCAPE '\\'");
            // LIKE 转义：把 prefix 里的 % _ \ 字符转义掉，再拼 %
            cmd.Parameters.AddWithValue("@prefix", EscapeLikePrefix(scopePrefix) + "%");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var scope = reader.GetString(0);
                if (!result.TryGetValue(scope, out var list))
                    result[scope] = list = [];
                list.Add(new KvEntry(
                    Key:         reader.GetString(1),
                    Value:       reader.IsDBNull(2) ? null : reader.GetString(2),
                    IsEncrypted: reader.GetInt32(3) == 1));
            }
            return result.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<KvEntry>)kvp.Value,
                StringComparer.OrdinalIgnoreCase);
        }
        finally { _rwLock.ExitReadLock(); }
    }

    private static string EscapeLikePrefix(string prefix)
        => prefix.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    // ── KeyMap ────────────────────────────────────────────────────────────────

    public IReadOnlyList<KeyMapStoreEntry> LoadKeyMapBindings()
    {
        _rwLock.EnterReadLock();
        try
        {
            var list = new List<KeyMapStoreEntry>();
            using var cmd = Cmd(
                "SELECT id, custom_key, is_enabled FROM keymap_bindings");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new KeyMapStoreEntry
                {
                    Id        = reader.GetString(0),
                    CustomKey = reader.IsDBNull(1) ? null : reader.GetString(1),
                    IsEnabled = reader.GetInt32(2) == 1
                });
            }
            return list;
        }
        finally { _rwLock.ExitReadLock(); }
    }

    public void SaveKeyMapBindings(IEnumerable<KeyMapStoreEntry> entries)
    {
        _rwLock.EnterWriteLock();
        try
        {
            using var tx  = _conn.BeginTransaction();
            using var cmd = Cmd("""
                INSERT INTO keymap_bindings (id, custom_key, is_enabled, updated_at)
                VALUES (@id, @ck, @en, unixepoch())
                ON CONFLICT(id) DO UPDATE SET
                    custom_key = excluded.custom_key,
                    is_enabled = excluded.is_enabled,
                    updated_at = excluded.updated_at
                """);
            cmd.Transaction = tx;

            // 预编译：提前创建参数，循环内只改值
            var pId = cmd.Parameters.Add("@id", SqliteType.Text);
            var pCk = cmd.Parameters.Add("@ck", SqliteType.Text);
            var pEn = cmd.Parameters.Add("@en", SqliteType.Integer);
            cmd.Prepare();

            foreach (var e in entries)
            {
                pId.Value = e.Id;
                pCk.Value = (object?)e.CustomKey ?? DBNull.Value;
                pEn.Value = e.IsEnabled ? 1 : 0;
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        finally { _rwLock.ExitWriteLock(); }
    }

    // ── 异步 KV（Task 包装同步实现，SQLite 本地 IO 无需真正异步）──────────────

    public Task<string?> GetAsync(string scope, string key)
        => Task.FromResult(GetKv(scope, key));

    public Task SetAsync(string scope, string key, string? value)
    {
        SetKv(scope, key, value);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string scope, string key)
    {
        DeleteKv(scope, key);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<string, string?>> GetAllAsync(string scope)
        => Task.FromResult(GetAllKv(scope));

    // ── 一次性文件迁移 ────────────────────────────────────────────────────────

    private void MigrateFromFiles()
    {
        MigrateKeyMapBindings();
        MigrateAppPreferences();
        MigratePluginVariables();
    }

    private void MigrateKeyMapBindings()
    {
        var path = LegacyPath(LegacyKeyMapFile);
        if (!File.Exists(path)) return;

        // 表非空说明已迁移过，跳过
        _rwLock.EnterReadLock();
        long count;
        try
        {
            using var check = Cmd("SELECT COUNT(*) FROM keymap_bindings");
            count = (long)check.ExecuteScalar()!;
        }
        finally { _rwLock.ExitReadLock(); }
        if (count > 0) return;

        try
        {
            var store = JsonSerializer.Deserialize<KeyMapStore>(
                File.ReadAllText(path), JsonOpts);
            if (store?.Entries is { Count: > 0 })
                SaveKeyMapBindings(store.Entries);
            File.Delete(path);
        }
        catch (Exception ex)
        {
            _diagnostics?.ReportWarning(
                DiagnosticsSource,
                $"Failed to migrate legacy key map bindings from '{path}'. The legacy file has been left in place.",
                ex);
        }
    }

    private void MigrateAppPreferences()
    {
        var path = LegacyPath(LegacyPreferencesFile);
        if (!File.Exists(path)) return;

        const string scope = AppPreferencesService.StorageScope;
        _rwLock.EnterReadLock();
        long count;
        try
        {
            using var check = Cmd("SELECT COUNT(*) FROM kv_store WHERE scope=@s");
            check.Parameters.AddWithValue("@s", scope);
            count = (long)check.ExecuteScalar()!;
        }
        finally { _rwLock.ExitReadLock(); }
        if (count > 0) return;

        try
        {
            var prefs = JsonSerializer.Deserialize<AppPreferences>(
                File.ReadAllText(path), JsonOpts);
            if (prefs is not null)
            {
                SetKv(scope, AppPreferencesService.KeyFontOption, prefs.FontOptionKey);
                SetKv(scope, AppPreferencesService.KeyTheme, prefs.ThemeKey);
                SetKv(scope, AppPreferencesService.KeyPalette, prefs.PaletteKey);
                SetKv(scope, AppPreferencesService.KeyCloseButtonBehavior, prefs.CloseButtonBehavior.ToString());
            }
            File.Delete(path);
        }
        catch (Exception ex)
        {
            _diagnostics?.ReportWarning(
                DiagnosticsSource,
                $"Failed to migrate app preferences from '{path}'. The legacy file has been left in place.",
                ex);
        }
    }

    private void MigratePluginVariables()
    {
        var path = LegacyPath(LegacyVariablesFile);
        if (!File.Exists(path)) return;

        try
        {
            var store = JsonSerializer.Deserialize<PluginVariableStore>(
                File.ReadAllText(path), JsonOpts);

            if (store?.Entries is { Count: > 0 })
            {
                _rwLock.EnterWriteLock();
                try
                {
                    using var tx  = _conn.BeginTransaction();
                    using var cmd = Cmd("""
                        INSERT OR IGNORE INTO kv_store
                            (scope, key, value, is_encrypted, updated_at)
                        VALUES (@s, @k, @v, @e, unixepoch())
                        """);
                    cmd.Transaction = tx;
                    var pS = cmd.Parameters.Add("@s", SqliteType.Text);
                    var pK = cmd.Parameters.Add("@k", SqliteType.Text);
                    var pV = cmd.Parameters.Add("@v", SqliteType.Text);
                    var pE = cmd.Parameters.Add("@e", SqliteType.Integer);
                    cmd.Prepare();

                    foreach (var entry in store.Entries)
                    {
                        pS.Value = PluginVariableService.ScopeFor(entry.PluginId);
                        pK.Value = entry.Key;
                        pV.Value = (object?)entry.Value ?? DBNull.Value;
                        pE.Value = entry.IsEncrypted ? 1 : 0;
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
                finally { _rwLock.ExitWriteLock(); }
            }
            File.Delete(path);
        }
        catch (Exception ex)
        {
            _diagnostics?.ReportWarning(
                DiagnosticsSource,
                $"Failed to migrate plugin variables from '{path}'. The legacy file has been left in place.",
                ex);
        }
    }

    // ── 辅助 ─────────────────────────────────────────────────────────────────

    private SqliteCommand Cmd(string sql)
    {
        var cmd = _conn.CreateCommand();
        cmd.CommandText = sql;
        return cmd;
    }

    private void Exec(string sql)
    {
        using var cmd = Cmd(sql);
        cmd.ExecuteNonQuery();
    }

    private static string LegacyPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, fileName);

    public void Dispose()
    {
        _conn.Dispose();
        _rwLock.Dispose();
    }
}
