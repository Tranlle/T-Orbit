using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tranbok.Tools.Plugins.Migration.Models;

namespace Tranbok.Tools.Plugins.Migration.Services;

public sealed record ProcessResult(bool Success, string Output, string Error);

internal sealed class WorkspaceDesignSettings
{
    public string DotnetVersion { get; set; } = string.Empty;
    public List<WorkspacePackageVersion> Packages { get; set; } = [];
}

internal sealed class WorkspacePackageVersion
{
    public string PackageName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

public sealed class MigrationService
{
    private const string ConfigFileName = ".tranbok-tools.json";
    private const string SettingsDirectoryName = "Migration";
    private const string SettingsFileName = "setting.json";
    private const string WorkspaceDirectoryName = "WorkSpace";

    private static string ToolsMigrationRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Plugins", "Migration"));
    private static string ToolsConfigFilePath => Path.Combine(ToolsMigrationRoot, ConfigFileName);
    private static string ToolsSettingsFilePath => Path.Combine(ToolsMigrationRoot, SettingsFileName);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // ─────────────────────────────────────────────────────────
    // Helpers — project path can be either a .csproj file or a directory
    // ─────────────────────────────────────────────────────────

    /// <summary>Returns the directory containing the project, whether a .csproj file or folder was given.</summary>
    private static string GetProjectDir(string projectPath)
        => File.Exists(projectPath)
            ? Path.GetDirectoryName(projectPath) ?? projectPath
            : projectPath;

    // ─────────────────────────────────────────────────────────
    // Config persistence
    // ─────────────────────────────────────────────────────────

    public MigrationToolConfig LoadConfig(string? projectPath = null)
    {
        if (File.Exists(ToolsConfigFilePath))
        {
            try
            {
                var json = File.ReadAllText(ToolsConfigFilePath);
                var config = JsonSerializer.Deserialize<MigrationToolConfig>(json, JsonOptions);
                if (config is not null)
                {
                    if (string.IsNullOrWhiteSpace(config.ProjectPath) && !string.IsNullOrWhiteSpace(projectPath))
                        config.ProjectPath = projectPath;
                    return config;
                }
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(projectPath))
        {
            var legacyConfigFile = Path.Combine(GetProjectDir(projectPath), ConfigFileName);
            if (File.Exists(legacyConfigFile))
            {
                try
                {
                    var json = File.ReadAllText(legacyConfigFile);
                    var config = JsonSerializer.Deserialize<MigrationToolConfig>(json, JsonOptions);
                    if (config is not null)
                    {
                        if (string.IsNullOrWhiteSpace(config.ProjectPath))
                            config.ProjectPath = projectPath;
                        return config;
                    }
                }
                catch { }
            }
        }

        return new MigrationToolConfig { ProjectPath = projectPath ?? string.Empty };
    }

    public void SaveConfig(MigrationToolConfig config)
    {
        Directory.CreateDirectory(ToolsMigrationRoot);
        File.WriteAllText(ToolsConfigFilePath, JsonSerializer.Serialize(config, JsonOptions));
    }

    public MigrationSettings LoadSettings(string? projectPath = null)
    {
        if (File.Exists(ToolsSettingsFilePath))
        {
            try
            {
                var json = File.ReadAllText(ToolsSettingsFilePath);
                var settings = JsonSerializer.Deserialize<MigrationSettings>(json, JsonOptions);
                if (settings is not null)
                {
                    if (string.IsNullOrWhiteSpace(settings.ProjectPath) && !string.IsNullOrWhiteSpace(projectPath))
                        settings.ProjectPath = projectPath;
                    return settings;
                }
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(projectPath))
        {
            var legacySettingsFile = Path.Combine(GetProjectDir(projectPath), SettingsDirectoryName, SettingsFileName);
            if (File.Exists(legacySettingsFile))
            {
                try
                {
                    var json = File.ReadAllText(legacySettingsFile);
                    var settings = JsonSerializer.Deserialize<MigrationSettings>(json, JsonOptions);
                    if (settings is not null)
                    {
                        if (string.IsNullOrWhiteSpace(settings.ProjectPath))
                            settings.ProjectPath = projectPath;
                        return settings;
                    }
                }
                catch { }
            }
        }

        return new MigrationSettings { ProjectPath = projectPath ?? string.Empty };
    }

    public void SaveSettings(MigrationSettings settings)
    {
        Directory.CreateDirectory(ToolsMigrationRoot);
        File.WriteAllText(ToolsSettingsFilePath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    // ─────────────────────────────────────────────────────────
    // dotnet ef migrations list
    //   Primary source of truth — returns migrations with Applied status.
    //   After parsing, enriches each entry with its .cs file path from the
    //   Migrations/{dbFolder}/ directory so the editor can open it.
    // ─────────────────────────────────────────────────────────

    public async Task<(List<MigrationEntry> Migrations, ProcessResult Raw)> ListMigrationsAsync(
        string projectPath,
        string startupProjectPath,
        DbConnectionProfile profile,
        CancellationToken ct = default)
    {
        var workspace = EnsureWorkspace(projectPath, startupProjectPath, profile);
        var args = BuildEfArgs(
            "migrations list", null,
            workspace.WorkspaceProjectPath, workspace.StartupProjectPath, profile.ContextName,
            "--no-color");

        var result = await RunWorkspaceEfAsync(workspace, args, ct, profile.ConnectionString);

        var migrations = ParseMigrationsList(result.Output);
        EnrichWithFilePaths(migrations, projectPath, profile);

        // If ef list failed but we still got some output lines that look like migrations, keep them.
        // Otherwise fall back to filesystem scan.
        if (!result.Success && migrations.Count == 0)
        {
            var fallback = ScanMigrationFiles(projectPath, profile);
            return (fallback, result);
        }

        return (migrations, result);
    }

    /// <summary>
    /// Parse `dotnet ef migrations list` stdout.
    /// Each line is one of:
    ///   20240101000000_Name
    ///   20240101000000_Name (Applied)
    ///   20240101000000_Name (Pending)
    /// Build/info lines are skipped.
    /// </summary>
    private static List<MigrationEntry> ParseMigrationsList(string output)
    {
        var linePattern = new Regex(
            @"^\s*(?<ts>\d{14})_(?<name>[A-Za-z0-9_]+)\s*(?:\((?<status>Applied|Pending)\))?\s*$",
            RegexOptions.Compiled);

        var result = new List<MigrationEntry>();
        foreach (var raw in output.Split('\n'))
        {
            var m = linePattern.Match(raw.Trim());
            if (!m.Success) continue;

            var ts     = m.Groups["ts"].Value;
            var name   = m.Groups["name"].Value;
            var status = m.Groups["status"].Value;

            result.Add(new MigrationEntry
            {
                TimestampId   = ts,
                MigrationName = name,
                Status        = status switch
                {
                    "Applied" => MigrationStatus.Applied,
                    "Pending" => MigrationStatus.Pending,
                    _         => MigrationStatus.Unknown
                },
                CreatedAt = ParseTimestamp(ts) ?? default
            });
        }
        return result;
    }

    /// <summary>
    /// Attach .cs file paths to entries parsed from `ef migrations list`.
    /// Files live at Migrations/{SqlServer|Pgsql|Mysql}/{ts}_{name}.cs
    /// </summary>
    private static void EnrichWithFilePaths(List<MigrationEntry> entries, string projectPath, DbConnectionProfile profile)
    {
        var dir = GetOutputDirectory(projectPath, profile);
        if (!Directory.Exists(dir)) return;

        // Build a lookup: fullName → file path
        var fileMap = Directory
            .GetFiles(dir, "*.cs")
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                     && !f.EndsWith("Snapshot.cs",  StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                f => Path.GetFileNameWithoutExtension(f),   // key = "20240101_Name"
                f => f,
                StringComparer.OrdinalIgnoreCase);

        foreach (var e in entries)
        {
            if (fileMap.TryGetValue(e.FullName, out var path))
                e.FilePath = path;
        }
    }

    /// <summary>
    /// Fallback: scan Migrations/{dbFolder}/ without running dotnet ef.
    /// Used when dotnet ef tools aren't available or the project can't build.
    /// </summary>
    private static List<MigrationEntry> ScanMigrationFiles(string projectPath, DbConnectionProfile profile)
    {
        var dir = GetOutputDirectory(projectPath, profile);
        if (!Directory.Exists(dir)) return [];

        var pattern = new Regex(@"^(\d{14})_(.+)\.cs$", RegexOptions.Compiled);

        return Directory
            .GetFiles(dir, "*.cs")
            .Select(f => (file: f, name: Path.GetFileName(f)))
            .Where(x => !x.name.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                     && !x.name.EndsWith("Snapshot.cs",  StringComparison.OrdinalIgnoreCase))
            .Select(x =>
            {
                var match = pattern.Match(x.name);
                if (!match.Success) return null;
                var ts   = match.Groups[1].Value;
                var name = match.Groups[2].Value;
                return new MigrationEntry
                {
                    TimestampId   = ts,
                    MigrationName = name,
                    FilePath      = x.file,
                    Status        = MigrationStatus.Unknown,
                    CreatedAt     = ParseTimestamp(ts) ?? new FileInfo(x.file).CreationTime
                };
            })
            .OfType<MigrationEntry>()
            .OrderBy(e => e.TimestampId)
            .ToList();
    }

    // ─────────────────────────────────────────────────────────
    // File I/O
    // ─────────────────────────────────────────────────────────

    public string ReadMigrationFile(string filePath)
        => File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;

    public void SaveMigrationFile(string filePath, string content)
        => File.WriteAllText(filePath, content, Encoding.UTF8);

    // ─────────────────────────────────────────────────────────
    // dotnet ef migrations add
    // ─────────────────────────────────────────────────────────

    public Task<ProcessResult> AddMigrationAsync(
        string projectPath,
        string startupProjectPath,
        string migrationName,
        DbConnectionProfile profile,
        CancellationToken ct = default)
    {
        var workspace = EnsureWorkspace(projectPath, startupProjectPath, profile);

        var args = BuildEfArgs(
            "migrations add", migrationName,
            workspace.WorkspaceProjectPath, workspace.StartupProjectPath, profile.ContextName,
            $"--output-dir \"{workspace.RelativeOutputDirectory}\"");

        return RunWorkspaceEfAsync(workspace, args, ct, profile.ConnectionString);
    }

    // ─────────────────────────────────────────────────────────
    // dotnet ef database update
    // ─────────────────────────────────────────────────────────

    public Task<ProcessResult> UpdateDatabaseAsync(
        string projectPath,
        string startupProjectPath,
        DbConnectionProfile profile,
        string? targetMigration = null,
        CancellationToken ct = default)
    {
        var workspace = EnsureWorkspace(projectPath, startupProjectPath, profile);
        var target = string.IsNullOrWhiteSpace(targetMigration) ? string.Empty : $" {targetMigration}";

        var args = BuildEfArgs(
            $"database update{target}", null,
            workspace.WorkspaceProjectPath, workspace.StartupProjectPath, profile.ContextName, null);

        return RunWorkspaceEfAsync(workspace, args, ct, profile.ConnectionString);
    }

    // ─────────────────────────────────────────────────────────
    // dotnet ef migrations remove
    //   EF Core's official way to remove the last migration.
    //   Removes the .cs file, the .Designer.cs file, and updates ModelSnapshot.
    //
    //   Rollback workflow (EF Core way):
    //     1. If the migration is Applied → database update <prevMigration>
    //     2. dotnet ef migrations remove   (always removes the LAST one)
    //
    //   Constraint: EF Core only supports removing the LAST migration.
    //   The caller must validate that `migration` is the last in the list.
    // ─────────────────────────────────────────────────────────

    public async Task<ProcessResult> RollbackLastMigrationAsync(
        string projectPath,
        string startupProjectPath,
        DbConnectionProfile profile,
        MigrationEntry migration,
        IReadOnlyList<MigrationEntry> allMigrations,
        CancellationToken ct = default)
    {
        // Step 1 — if applied, roll back the database to the previous migration
        if (migration.Status == MigrationStatus.Applied)
        {
            var ordered = allMigrations.OrderBy(m => m.TimestampId).ToList();
            var idx     = ordered.FindIndex(m => m.FullName == migration.FullName);
            var target  = idx > 0 ? ordered[idx - 1].FullName : "0";

            var dbResult = await UpdateDatabaseAsync(
                projectPath, startupProjectPath, profile, target, ct);

            if (!dbResult.Success)
                return dbResult with { Error = $"[DB rollback failed — {migration.FullName} ← {target}]\n{dbResult.Error}" };
        }

        // Step 2 — dotnet ef migrations remove  (EF Core removes files + updates snapshot)
        var workspace = EnsureWorkspace(projectPath, startupProjectPath, profile);

        var removeArgs = BuildEfArgs(
            "migrations remove", null,
            workspace.WorkspaceProjectPath, workspace.StartupProjectPath, profile.ContextName,
            "--force");

        var removeResult = await RunWorkspaceEfAsync(workspace, removeArgs, ct, profile.ConnectionString);

        if (removeResult.Success)
            return removeResult with
            {
                Output = removeResult.Output
                    + $"\n✓ Removed migration {migration.FullName} (files + ModelSnapshot updated by EF Core)"
            };

        return removeResult;
    }

    // ─────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────

    private static string DbTypeFolder(DbType dbType) => dbType switch
    {
        DbType.SqlServer  => "SqlServer",
        DbType.PostgreSQL => "Pgsql",
        DbType.MySQL      => "Mysql",
        _                 => "SqlServer"
    };

    public static string GetOutputDirectory(string projectPath, DbConnectionProfile profile)
    {
        var domainProjectPath = Path.GetFullPath(projectPath);
        var projectName = Path.GetFileNameWithoutExtension(domainProjectPath);
        var contextName = string.IsNullOrWhiteSpace(profile.ContextName) ? "DefaultDbContext" : profile.ContextName;
        var toolsMigrationRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Plugins", "Migration"));

        return Path.Combine(
            toolsMigrationRoot,
            "Output",
            projectName,
            contextName,
            DbTypeFolder(profile.DbType));
    }

    private static string BuildEfArgs(
        string command,
        string? positional,
        string projectPath,
        string startupProjectPath,
        string? contextName,
        string? extra = null)
    {
        var sb = new StringBuilder("ef ");
        sb.Append(command);
        if (!string.IsNullOrWhiteSpace(positional))
            sb.Append(' ').Append(positional);

        sb.Append($" --project \"{projectPath}\"");
        if (!string.IsNullOrWhiteSpace(startupProjectPath))
            sb.Append($" --startup-project \"{startupProjectPath}\"");
        if (!string.IsNullOrWhiteSpace(contextName))
            sb.Append($" --context {contextName}");
        if (!string.IsNullOrWhiteSpace(extra))
            sb.Append(' ').Append(extra.Trim());

        return sb.ToString();
    }

    private static WorkspaceInfo EnsureWorkspace(string projectPath, string startupProjectPath, DbConnectionProfile profile)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new InvalidOperationException("Domain project path is required.");
        if (string.IsNullOrWhiteSpace(startupProjectPath))
            throw new InvalidOperationException("Provider project path is required.");
        if (string.IsNullOrWhiteSpace(profile.ContextName))
            throw new InvalidOperationException("DbContext name is required.");

        var domainProjectPath = Path.GetFullPath(projectPath);
        var providerProjectPath = Path.GetFullPath(startupProjectPath);
        var domainProjectName = Path.GetFileNameWithoutExtension(domainProjectPath);
        var toolsMigrationRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Plugins", "Migration"));
        var workspaceDirectory = Path.Combine(toolsMigrationRoot, WorkspaceDirectoryName, domainProjectName, profile.ContextName, DbTypeFolder(profile.DbType));
        Directory.CreateDirectory(workspaceDirectory);

        var workspaceProjectName = $"{domainProjectName}.{profile.ContextName}.{DbTypeFolder(profile.DbType)}.Workspace";
        var workspaceProjectPath = Path.Combine(workspaceDirectory, workspaceProjectName + ".csproj");
        var outputDirectory = GetOutputDirectory(domainProjectPath, profile);
        Directory.CreateDirectory(outputDirectory);
        var relativeOutputDirectory = Path.GetRelativePath(workspaceDirectory, outputDirectory);

        File.WriteAllText(workspaceProjectPath, BuildWorkspaceProjectXml(workspaceDirectory, domainProjectPath, providerProjectPath));
        File.WriteAllText(Path.Combine(workspaceDirectory, "DesignTimeFactory.cs"), BuildWorkspaceFactoryCode(profile));

        return new WorkspaceInfo
        {
            WorkspaceDirectory = workspaceDirectory,
            WorkspaceProjectPath = workspaceProjectPath,
            StartupProjectPath = workspaceProjectPath,
            RelativeOutputDirectory = relativeOutputDirectory,
            DomainProjectPath = domainProjectPath,
            ProviderProjectPath = providerProjectPath
        };
    }

    private static string BuildWorkspaceProjectXml(string workspaceDirectory, string domainProjectPath, string providerProjectPath)
    {
        static string Normalize(string value) => value.Replace('\\', '/');

        var settings = LoadRequiredDesignSettings(domainProjectPath);
        var targetFramework = ResolveTargetFramework(settings);

        var project = new XElement("Project",
            new XAttribute("Sdk", "Microsoft.NET.Sdk"),
            new XElement("PropertyGroup",
                new XElement("TargetFramework", targetFramework),
                new XElement("ImplicitUsings", "enable"),
                new XElement("Nullable", "enable")));

        var references = new XElement("ItemGroup",
            new XElement("ProjectReference", new XAttribute("Include", Normalize(Path.GetRelativePath(workspaceDirectory, domainProjectPath)))),
            new XElement("ProjectReference", new XAttribute("Include", Normalize(Path.GetRelativePath(workspaceDirectory, providerProjectPath)))));

        var packages = new XElement("ItemGroup");
        foreach (var package in ResolveRequiredWorkspacePackages(settings))
        {
            var packageReference = new XElement("PackageReference",
                new XAttribute("Include", package.PackageName),
                new XAttribute("Version", package.Version));

            if (string.Equals(package.PackageName, "Microsoft.EntityFrameworkCore.Design", StringComparison.OrdinalIgnoreCase))
            {
                packageReference.Add(
                    new XElement("PrivateAssets", "all"),
                    new XElement("IncludeAssets", "runtime; build; native; contentfiles; analyzers; buildtransitive"));
            }

            packages.Add(packageReference);
        }

        project.Add(references, packages);

        return new XDocument(project).ToString();
    }

    private static string ResolveTargetFramework(WorkspaceDesignSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.DotnetVersion))
            throw new InvalidOperationException("DesignSettings.json 缺少 DotnetVersion 配置。");

        return settings.DotnetVersion;
    }

    private static WorkspaceDesignSettings LoadRequiredDesignSettings(string domainProjectPath)
    {
        var projectDirectory = Path.GetDirectoryName(domainProjectPath);
        if (string.IsNullOrWhiteSpace(projectDirectory))
            throw new InvalidOperationException("无法定位 Domain 项目目录，无法读取 DesignSettings.json。");

        var settingsPath = Path.Combine(projectDirectory, "DesignSettings.json");
        if (!File.Exists(settingsPath))
            throw new InvalidOperationException($"未找到 DesignSettings.json：{settingsPath}");

        try
        {
            var json = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<WorkspaceDesignSettings>(json, JsonOptions);
            if (settings is null)
                throw new InvalidOperationException("DesignSettings.json 解析结果为空。");

            return settings;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"DesignSettings.json 解析失败：{ex.Message}", ex);
        }
    }

    private static IReadOnlyList<WorkspacePackageVersion> ResolveRequiredWorkspacePackages(WorkspaceDesignSettings settings)
    {
        var packages = settings.Packages
            .Where(p => !string.IsNullOrWhiteSpace(p.PackageName) && !string.IsNullOrWhiteSpace(p.Version))
            .GroupBy(p => p.PackageName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();

        if (packages.Count == 0)
            throw new InvalidOperationException("DesignSettings.json 缺少 Packages 配置。");

        if (packages.Any(p => string.IsNullOrWhiteSpace(p.PackageName) || string.IsNullOrWhiteSpace(p.Version)))
            throw new InvalidOperationException("DesignSettings.json 的 Packages 项必须同时提供 PackageName 和 Version。");

        if (packages.All(p => !string.Equals(p.PackageName, "Microsoft.EntityFrameworkCore.Design", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("DesignSettings.json 的 Packages 中必须包含 Microsoft.EntityFrameworkCore.Design 版本配置。");

        return packages;
    }

    private static string BuildWorkspaceFactoryCode(DbConnectionProfile profile)
    {
        var escapedContextName = profile.ContextName.Replace("\"", "\"\"");

        var providerCode = profile.DbType switch
        {
            DbType.SqlServer => "Microsoft.EntityFrameworkCore.SqlServerDbContextOptionsExtensions.UseSqlServer(builder, connectionString, o => { o.EnableRetryOnFailure(maxRetryCount: 3); o.CommandTimeout(30); });",
            DbType.PostgreSQL => "Npgsql.EntityFrameworkCore.PostgreSQLDbContextOptionsBuilderExtensions.UseNpgsql(builder, connectionString);",
            DbType.MySQL => "MySQL.EntityFrameworkCore.Extensions.MySQLDbContextOptionsExtensions.UseMySQL(builder, connectionString);",
            _ => throw new NotSupportedException($"Unsupported database type: {profile.DbType}")
        };

        var template = @"using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tranbok.Domain.Context;

namespace Tranbok.Migrations.WorkSpace;

public sealed class WorkspaceDesignTimeFactory : IDesignTimeDbContextFactory<__CONTEXT_NAME__>
{
    public __CONTEXT_NAME__ CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(""TRANBOK_DB_CONNECTION"");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(""No connection string configured for workspace design-time factory."");

        var contextType = typeof(BaseDbContext).Assembly
            .GetTypes()
            .FirstOrDefault(t => typeof(DbContext).IsAssignableFrom(t)
                && !t.IsAbstract
                && string.Equals(t.Name, ""__CONTEXT_NAME__"", StringComparison.Ordinal));

        if (contextType is null)
            throw new InvalidOperationException(""DbContext '__CONTEXT_NAME__' was not found in Tranbok.Domain assembly."");

        var optionsType = typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType);
        var builder = (DbContextOptionsBuilder)Activator.CreateInstance(optionsType)!;
        __PROVIDER_CODE__

        var ctor = contextType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length >= 1
                    && typeof(DbContextOptions).IsAssignableFrom(parameters[0].ParameterType)
                    && parameters.Skip(1).All(p => p.HasDefaultValue || Nullable.GetUnderlyingType(p.ParameterType) is not null || !p.ParameterType.IsValueType);
            });

        if (ctor is null)
            throw new InvalidOperationException($""No suitable constructor found for {contextType.FullName}."");

        var ctorArgs = ctor.GetParameters()
            .Select((p, i) => i == 0 ? builder.Options : p.HasDefaultValue ? p.DefaultValue : null)
            .ToArray();

        return (__CONTEXT_NAME__)ctor.Invoke(ctorArgs);
    }
}";

        return template
            .Replace("__CONTEXT_NAME__", escapedContextName)
            .Replace("__PROVIDER_CODE__", providerCode);
    }

    private static async Task<ProcessResult> RunWorkspaceEfAsync(
        WorkspaceInfo workspace,
        string arguments,
        CancellationToken ct,
        string? connectionString = null)
    {
        var restore = await RunDotnetAsync(workspace.WorkspaceDirectory, $"restore \"{workspace.WorkspaceProjectPath}\"", ct, connectionString);
        if (!restore.Success)
        {
            return new ProcessResult(
                false,
                restore.Output,
                $"Workspace restore failed.\n{restore.Error}");
        }

        return await RunDotnetAsync(workspace.WorkspaceDirectory, arguments, ct, connectionString);
    }

    private static async Task<ProcessResult> RunDotnetAsync(
        string workingDir,
        string arguments,
        CancellationToken ct,
        string? connectionString = null)
    {
        var psi = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(connectionString))
            psi.Environment["TRANBOK_DB_CONNECTION"] = connectionString;

        psi.Environment["DOTNET_CLI_UI_LANGUAGE"] = "zh-CN";

        using var process = new Process { StartInfo = psi };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(); } catch { }
            return new ProcessResult(false, stdout.ToString(), "Operation cancelled.");
        }

        return new ProcessResult(
            process.ExitCode == 0,
            stdout.ToString(),
            stderr.ToString());
    }

    private static DateTime? ParseTimestamp(string ts)
    {
        if (ts.Length != 14) return null;
        return DateTime.TryParseExact(ts, "yyyyMMddHHmmss",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt) ? dt : null;
    }
}
