using System.Text.Json;
using Microsoft.Data.Sqlite;
using Tranbok.Tools.Core.Models;

namespace Tranbok.Tools.Core.Services;

/// <summary>
/// <see cref="IStorageService"/> 的 SQLite 实现（Singleton）。
/// <para>
/// 持有单个 <see cref="SqliteConnection"/>，通过 <c>lock</c> 序列化访问保证线程安全。
/// 启动时自动完成 schema 初始化和旧文件的一次性迁移。
/// </para>
/// </summary>
public sealed class StorageService : IStorageService, IDisposable
{
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
    private readonly object _lock = new();

    // ── 构造：开连接、建表、迁移 ─────────────────────────────────────────────

    public StorageService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TranbokTools");
        Directory.CreateDirectory(dir);

        var dbPath = Path.Combine(dir, "tranbok-tools.db");
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
        lock (_lock)
        {
            using var cmd = Cmd(
                "SELECT value FROM kv_store WHERE scope=@s AND key=@k");
            cmd.Parameters.AddWithValue("@s", scope);
            cmd.Parameters.AddWithValue("@k", key);
            var result = cmd.ExecuteScalar();
            return result is DBNull or null ? null : (string)result;
        }
    }

    public void SetKv(string scope, string key, string? value)
        => SetKvWithMeta(scope, key, value, isEncrypted: false);

    public void DeleteKv(string scope, string key)
    {
        lock (_lock)
        {
            using var cmd = Cmd(
                "DELETE FROM kv_store WHERE scope=@s AND key=@k");
            cmd.Parameters.AddWithValue("@s", scope);
            cmd.Parameters.AddWithValue("@k", key);
            cmd.ExecuteNonQuery();
        }
    }

    public IReadOnlyDictionary<string, string?> GetAllKv(string scope)
    {
        lock (_lock)
        {
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            using var cmd = Cmd(
                "SELECT key, value FROM kv_store WHERE scope=@s");
            cmd.Parameters.AddWithValue("@s", scope);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                dict[reader.GetString(0)] = reader.IsDBNull(1) ? null : reader.GetString(1);
            return dict;
        }
    }

    // ── KV 带加密标记 ─────────────────────────────────────────────────────────

    public void SetKvWithMeta(string scope, string key, string? value, bool isEncrypted)
    {
        lock (_lock)
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
        }
    }

    public IReadOnlyList<KvEntry> GetAllKvWithMeta(string scope)
    {
        lock (_lock)
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
    }

    // ── KeyMap ────────────────────────────────────────────────────────────────

    public IReadOnlyList<KeyMapStoreEntry> LoadKeyMapBindings()
    {
        lock (_lock)
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
    }

    public void SaveKeyMapBindings(IEnumerable<KeyMapStoreEntry> entries)
    {
        lock (_lock)
        {
            using var tx = _conn.BeginTransaction();
            foreach (var e in entries)
            {
                using var cmd = Cmd("""
                    INSERT INTO keymap_bindings (id, custom_key, is_enabled, updated_at)
                    VALUES (@id, @ck, @en, unixepoch())
                    ON CONFLICT(id) DO UPDATE SET
                        custom_key = excluded.custom_key,
                        is_enabled = excluded.is_enabled,
                        updated_at = excluded.updated_at
                    """);
                cmd.Transaction = tx;
                cmd.Parameters.AddWithValue("@id", e.Id);
                cmd.Parameters.AddWithValue("@ck", (object?)e.CustomKey ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@en", e.IsEnabled ? 1 : 0);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
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
        lock (_lock)
        {
            using var check = Cmd("SELECT COUNT(*) FROM keymap_bindings");
            if ((long)check.ExecuteScalar()! > 0) return;
        }

        try
        {
            var store = JsonSerializer.Deserialize<KeyMapStore>(
                File.ReadAllText(path), JsonOpts);
            if (store?.Entries is { Count: > 0 })
                SaveKeyMapBindings(store.Entries);
            File.Delete(path);
        }
        catch { /* 文件损坏时静默跳过，旧文件保留，服务从空白状态启动 */ }
    }

    private void MigrateAppPreferences()
    {
        var path = LegacyPath(LegacyPreferencesFile);
        if (!File.Exists(path)) return;

        const string scope = AppPreferencesService.StorageScope;
        lock (_lock)
        {
            using var check = Cmd(
                "SELECT COUNT(*) FROM kv_store WHERE scope=@s");
            check.Parameters.AddWithValue("@s", scope);
            if ((long)check.ExecuteScalar()! > 0) return;
        }

        try
        {
            var prefs = JsonSerializer.Deserialize<AppPreferences>(
                File.ReadAllText(path), JsonOpts);
            if (prefs is not null)
                SetKv(scope, AppPreferencesService.KeyFontOption, prefs.FontOptionKey);
            File.Delete(path);
        }
        catch { }
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
                lock (_lock)
                {
                    using var tx = _conn.BeginTransaction();
                    foreach (var entry in store.Entries)
                    {
                        var scope = PluginVariableService.ScopeFor(entry.PluginId);
                        using var cmd = Cmd("""
                            INSERT OR IGNORE INTO kv_store
                                (scope, key, value, is_encrypted, updated_at)
                            VALUES (@s, @k, @v, @e, unixepoch())
                            """);
                        cmd.Transaction = tx;
                        cmd.Parameters.AddWithValue("@s", scope);
                        cmd.Parameters.AddWithValue("@k", entry.Key);
                        cmd.Parameters.AddWithValue("@v", (object?)entry.Value ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@e", entry.IsEncrypted ? 1 : 0);
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
            }
            File.Delete(path);
        }
        catch { }
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

    public void Dispose() => _conn.Dispose();
}
