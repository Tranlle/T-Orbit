using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tranbok.Tools.Plugin.Migration.Models;

namespace Tranbok.Tools.Plugin.Migration.Services;

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

    private static string PluginRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Plugins", "Migration"));
    private static string ConfigFilePath => Path.Combine(PluginRoot, ConfigFileName);
    private static string SettingsFilePath => Path.Combine(PluginRoot, SettingsFileName);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static string GetProjectDir(string projectPath)
        => File.Exists(projectPath)
            ? Path.GetDirectoryName(projectPath) ?? projectPath
            : projectPath;

    public MigrationToolConfig LoadConfig(string? projectPath = null)
    {
        if (File.Exists(ConfigFilePath))
        {
            try
            {
                var json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<MigrationToolConfig>(json, JsonOptions);
                if (config is not null)
                {
                    if (string.IsNullOrWhiteSpace(config.ProjectPath) && !string.IsNullOrWhiteSpace(projectPath))
                        config.ProjectPath = projectPath;
                    return config;
                }
            }
            catch
            {
            }
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
                catch
                {
                }
            }
        }

        return new MigrationToolConfig { ProjectPath = projectPath ?? string.Empty };
    }

    public void SaveConfig(MigrationToolConfig config)
    {
        Directory.CreateDirectory(PluginRoot);
        File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(config, JsonOptions));
    }

    public MigrationSettings LoadSettings(string? projectPath = null)
    {
        if (File.Exists(SettingsFilePath))
        {
            try
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<MigrationSettings>(json, JsonOptions);
                if (settings is not null)
                {
                    if (string.IsNullOrWhiteSpace(settings.ProjectPath) && !string.IsNullOrWhiteSpace(projectPath))
                        settings.ProjectPath = projectPath;
                    return settings;
                }
            }
            catch
            {
            }
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
                catch
                {
                }
            }
        }

        return new MigrationSettings { ProjectPath = projectPath ?? string.Empty };
    }

    public void SaveSettings(MigrationSettings settings)
    {
        Directory.CreateDirectory(PluginRoot);
        File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public async Task<(List<MigrationEntry> Migrations, ProcessResult Raw)> ListMigrationsAsync(string projectPath, string startupProjectPath, DbConnectionProfile profile, CancellationToken ct = default)
    {
        var workspace = GetWorkspaceInfo(projectPath, startupProjectPath, profile);
        var args = BuildEfArgs("migrations list", null, workspace.WorkspaceProjectPath, workspace.StartupProjectPath, profile.ContextName, "--no-color");
        var result = await RunWorkspaceEfAsync(workspace, args, ct, profile.ConnectionString);

        var migrations = ParseMigrationsList(result.Output);
        EnrichWithFilePaths(migrations, workspace.WorkspaceDirectory, projectPath, profile);

        if (migrations.Count == 0)
        {
            var fallback = ScanMigrationFiles(workspace.WorkspaceDirectory, projectPath, profile);
            return (fallback, result);
        }

        return (migrations, result);
    }

    public string ReadMigrationFile(string filePath)
        => File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;

    public void SaveMigrationFile(string filePath, string content)
        => File.WriteAllText(filePath, content, Encoding.UTF8);

    public async Task<ProcessResult> AddMigrationAsync(string projectPath, string startupProjectPath, string migrationName, DbConnectionProfile profile, CancellationToken ct = default)
    {
        var workspace = EnsureWorkspace(projectPath, startupProjectPath, profile);
        var args = BuildEfArgs("migrations add", migrationName, workspace.WorkspaceProjectPath, workspace.StartupProjectPath, profile.ContextName, null);
        var result = await RunWorkspaceEfAsync(workspace, args, ct, profile.ConnectionString);
        if (result.Success)
            CopyWorkspaceMigrationsToOutput(workspace.WorkspaceDirectory, GetOutputDirectory(projectPath, profile));

        var commandLog = string.Join(Environment.NewLine, new[]
        {
            $"[EfCommand] dotnet {args}",
            $"[WorkspaceDirectory] {workspace.WorkspaceDirectory}",
            $"[WorkspaceMigrationDirectory] {GetWorkspaceMigrationDirectory(workspace.WorkspaceDirectory)}",
            $"[CopyTargetDirectory] {GetOutputDirectory(projectPath, profile)}"
        });

        return result with
        {
            Output = string.IsNullOrWhiteSpace(result.Output) ? commandLog : commandLog + Environment.NewLine + result.Output
        };
    }

    public Task<ProcessResult> UpdateDatabaseAsync(string projectPath, string startupProjectPath, DbConnectionProfile profile, string? targetMigration = null, CancellationToken ct = default)
    {
        var workspace = GetWorkspaceInfo(projectPath, startupProjectPath, profile);
        var target = string.IsNullOrWhiteSpace(targetMigration) ? string.Empty : $" {targetMigration}";
        var args = BuildEfArgs($"database update{target}", null, workspace.WorkspaceProjectPath, workspace.StartupProjectPath, profile.ContextName, null);
        return RunWorkspaceEfAsync(workspace, args, ct, profile.ConnectionString);
    }

    public async Task<ProcessResult> RollbackLastMigrationAsync(string projectPath, string startupProjectPath, DbConnectionProfile profile, MigrationEntry migration, IReadOnlyList<MigrationEntry> allMigrations, CancellationToken ct = default)
    {
        if (migration.Status == MigrationStatus.Applied)
        {
            var ordered = allMigrations.OrderBy(m => m.TimestampId).ToList();
            var idx = ordered.FindIndex(m => m.FullName == migration.FullName);
            var target = idx > 0 ? ordered[idx - 1].FullName : "0";
            var dbResult = await UpdateDatabaseAsync(projectPath, startupProjectPath, profile, target, ct);
            if (!dbResult.Success)
                return dbResult with { Error = $"[DB rollback failed — {migration.FullName} ← {target}]\n{dbResult.Error}" };
        }

        var workspace = GetWorkspaceInfo(projectPath, startupProjectPath, profile);
        var removeArgs = BuildEfArgs("migrations remove", null, workspace.WorkspaceProjectPath, workspace.StartupProjectPath, profile.ContextName, "--force");
        var removeResult = await RunWorkspaceEfAsync(workspace, removeArgs, ct, profile.ConnectionString);

        if (removeResult.Success)
        {
            return removeResult with
            {
                Output = removeResult.Output + $"\n✓ Removed migration {migration.FullName} (files + ModelSnapshot updated by EF Core)"
            };
        }

        return removeResult;
    }

    public static string GetOutputDirectory(string projectPath, DbConnectionProfile profile)
    {
        var domainProjectPath = Path.GetFullPath(projectPath);
        var projectName = Path.GetFileNameWithoutExtension(domainProjectPath);
        var contextName = string.IsNullOrWhiteSpace(profile.ContextName) ? "DefaultDbContext" : profile.ContextName;
        return Path.Combine(PluginRoot, "Output", projectName, contextName, DbTypeFolder(profile.DbType));
    }

    private static List<MigrationEntry> ParseMigrationsList(string output)
    {
        var linePattern = new Regex(@"^\s*(?<ts>\d{14})_(?<name>[A-Za-z0-9_]+)\s*(?:\((?<status>Applied|Pending)\))?\s*$", RegexOptions.Compiled);
        var result = new List<MigrationEntry>();
        foreach (var raw in output.Split('\n'))
        {
            var match = linePattern.Match(raw.Trim());
            if (!match.Success)
                continue;

            var ts = match.Groups["ts"].Value;
            var name = match.Groups["name"].Value;
            var status = match.Groups["status"].Value;

            result.Add(new MigrationEntry
            {
                TimestampId = ts,
                MigrationName = name,
                Status = status switch
                {
                    "Applied" => MigrationStatus.Applied,
                    "Pending" => MigrationStatus.Pending,
                    _ => MigrationStatus.Unknown
                },
                CreatedAt = ParseTimestamp(ts) ?? default
            });
        }

        return result;
    }

    private static void EnrichWithFilePaths(List<MigrationEntry> entries, string workspaceDirectory, string projectPath, DbConnectionProfile profile)
    {
        var dir = GetWorkspaceMigrationDirectory(workspaceDirectory);
        if (!Directory.Exists(dir))
            dir = GetOutputDirectory(projectPath, profile);
        if (!Directory.Exists(dir))
            return;

        var fileMap = Directory.GetFiles(dir, "*.cs")
            .Where(file => !file.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                           && !file.EndsWith("Snapshot.cs", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(file => Path.GetFileNameWithoutExtension(file), file => file, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (fileMap.TryGetValue(entry.FullName, out var path))
                entry.FilePath = path;
        }
    }

    private static List<MigrationEntry> ScanMigrationFiles(string workspaceDirectory, string projectPath, DbConnectionProfile profile)
    {
        var dir = GetWorkspaceMigrationDirectory(workspaceDirectory);
        if (!Directory.Exists(dir))
            dir = GetOutputDirectory(projectPath, profile);
        if (!Directory.Exists(dir))
            return [];

        var pattern = new Regex(@"^(\d{14})_(.+)\.cs$", RegexOptions.Compiled);

        return Directory.GetFiles(dir, "*.cs")
            .Select(file => (file, name: Path.GetFileName(file)))
            .Where(x => !x.name.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                     && !x.name.EndsWith("Snapshot.cs", StringComparison.OrdinalIgnoreCase))
            .Select(x =>
            {
                var match = pattern.Match(x.name);
                if (!match.Success)
                    return null;

                var ts = match.Groups[1].Value;
                var name = match.Groups[2].Value;
                return new MigrationEntry
                {
                    TimestampId = ts,
                    MigrationName = name,
                    FilePath = x.file,
                    Status = MigrationStatus.Unknown,
                    CreatedAt = ParseTimestamp(ts) ?? new FileInfo(x.file).CreationTime
                };
            })
            .OfType<MigrationEntry>()
            .OrderBy(x => x.TimestampId)
            .ToList();
    }

    private static string DbTypeFolder(DbType dbType) => dbType switch
    {
        DbType.SqlServer => "SqlServer",
        DbType.PostgreSQL => "Pgsql",
        DbType.MySQL => "Mysql",
        _ => "SqlServer"
    };

    private static string GetWorkspaceMigrationDirectory(string workspaceDirectory)
    {
        var candidates = new[]
        {
            workspaceDirectory,
            Path.Combine(workspaceDirectory, "Migrations"),
            Path.Combine(workspaceDirectory, "Migration"),
            Path.Combine(workspaceDirectory, "Migrations", "Output"),
            Path.Combine(workspaceDirectory, "Migration", "Output")
        };

        foreach (var candidate in candidates)
        {
            if (!Directory.Exists(candidate))
                continue;

            var hasMigrationFile = Directory.GetFiles(candidate, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Any(name => Regex.IsMatch(name!, @"^\d{14}_.+\.cs$", RegexOptions.Compiled)
                             && !name!.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                             && !name.EndsWith("Snapshot.cs", StringComparison.OrdinalIgnoreCase));

            if (hasMigrationFile)
                return candidate;
        }

        return workspaceDirectory;
    }

    private static void CopyWorkspaceMigrationsToOutput(string workspaceDirectory, string outputDirectory)
    {
        var sourceDirectory = GetWorkspaceMigrationDirectory(workspaceDirectory);
        if (!Directory.Exists(sourceDirectory))
            return;

        Directory.CreateDirectory(outputDirectory);
        var migrationFilePattern = new Regex(@"^\d{14}_.+\.cs$", RegexOptions.Compiled);

        foreach (var file in Directory.GetFiles(sourceDirectory, "*.cs"))
        {
            var fileName = Path.GetFileName(file);
            if (!migrationFilePattern.IsMatch(fileName))
                continue;
            if (fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            File.Copy(file, Path.Combine(outputDirectory, fileName), true);
        }
    }

    private static string BuildEfArgs(string command, string? positional, string projectPath, string startupProjectPath, string? contextName, string? extra = null)
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

    private static WorkspaceInfo GetWorkspaceInfo(string projectPath, string startupProjectPath, DbConnectionProfile profile)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new InvalidOperationException("Domain project path is required.");
        if (string.IsNullOrWhiteSpace(startupProjectPath))
            throw new InvalidOperationException("Provider project path is required.");
        if (string.IsNullOrWhiteSpace(profile.ContextName))
            throw new InvalidOperationException("DbContext name is required.");

        var domainProjectPath = Path.GetFullPath(projectPath);
        var domainProjectName = Path.GetFileNameWithoutExtension(domainProjectPath);
        var dbFolderName = DbTypeFolder(profile.DbType);
        var workspaceDirectory = Path.Combine(PluginRoot, WorkspaceDirectoryName, domainProjectName, profile.ContextName, dbFolderName);
        var contextProjectName = $"{profile.ContextName}Project";
        var workspaceProjectName = $"{domainProjectName}.{contextProjectName}.{dbFolderName}.Workspace";
        var workspaceProjectPath = Path.Combine(workspaceDirectory, workspaceProjectName + ".csproj");
        var outputDirectory = GetOutputDirectory(domainProjectPath, profile);
        var relativeOutputDirectory = Path.GetRelativePath(workspaceDirectory, outputDirectory);

        return new WorkspaceInfo
        {
            WorkspaceDirectory = workspaceDirectory,
            WorkspaceProjectPath = workspaceProjectPath,
            StartupProjectPath = workspaceProjectPath,
            RelativeOutputDirectory = relativeOutputDirectory,
            DomainProjectPath = domainProjectPath
        };
    }

    private static WorkspaceInfo EnsureWorkspace(string projectPath, string startupProjectPath, DbConnectionProfile profile)
    {
        var workspace = GetWorkspaceInfo(projectPath, startupProjectPath, profile);
        Directory.CreateDirectory(workspace.WorkspaceDirectory);
        Directory.CreateDirectory(GetOutputDirectory(workspace.DomainProjectPath, profile));

        foreach (var existingWorkspaceProject in Directory.GetFiles(workspace.WorkspaceDirectory, "*.csproj", SearchOption.TopDirectoryOnly))
        {
            if (!string.Equals(existingWorkspaceProject, workspace.WorkspaceProjectPath, StringComparison.OrdinalIgnoreCase))
                File.Delete(existingWorkspaceProject);
        }

        var workspaceProjectXml = BuildWorkspaceProjectXml(workspace.WorkspaceDirectory, workspace.DomainProjectPath, profile.DbType);
        File.WriteAllText(workspace.WorkspaceProjectPath, workspaceProjectXml);
        var contextNamespace = ResolveDbContextNamespace(workspace.DomainProjectPath, profile.ContextName);
        File.WriteAllText(Path.Combine(workspace.WorkspaceDirectory, "DesignTimeFactory.cs"), BuildWorkspaceFactoryCode(profile, Path.GetFileNameWithoutExtension(workspace.WorkspaceProjectPath), contextNamespace));

        return workspace;
    }

    private static string BuildWorkspaceProjectXml(string workspaceDirectory, string domainProjectPath, DbType dbType)
    {
        static string Normalize(string value) => value.Replace('\\', '/');

        var settingsPath = Path.Combine(Path.GetDirectoryName(domainProjectPath)!, "DesignSettings.json");
        var settings = LoadRequiredDesignSettings(domainProjectPath);
        var targetFramework = ResolveTargetFramework(settings, settingsPath);

        var project = new XElement("Project",
            new XAttribute("Sdk", "Microsoft.NET.Sdk"),
            new XElement("PropertyGroup",
                new XElement("TargetFramework", targetFramework),
                new XElement("ImplicitUsings", "enable"),
                new XElement("Nullable", "enable"),
                new XElement("TranbokWorkspaceMarker", "design-settings-v2")));

        var references = new XElement("ItemGroup",
            new XElement("ProjectReference", new XAttribute("Include", Normalize(Path.GetRelativePath(workspaceDirectory, domainProjectPath)))));

        var packages = new XElement("ItemGroup");
        foreach (var package in ResolveRequiredWorkspacePackages(settings, settingsPath, dbType))
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

    private static string ResolveTargetFramework(WorkspaceDesignSettings settings, string settingsPath)
    {
        if (string.IsNullOrWhiteSpace(settings.DotnetVersion))
            throw new InvalidOperationException($"[WorkspaceConfigException] DesignSettings.json 缺少 DotnetVersion 配置：{settingsPath}");
        return settings.DotnetVersion;
    }

    private static WorkspaceDesignSettings LoadRequiredDesignSettings(string domainProjectPath)
    {
        var projectDirectory = Path.GetDirectoryName(domainProjectPath);
        if (string.IsNullOrWhiteSpace(projectDirectory))
            throw new InvalidOperationException($"[WorkspaceConfigException] 无法定位 Domain 项目目录，无法读取 DesignSettings.json。Domain 项目：{domainProjectPath}");

        var settingsPath = Path.Combine(projectDirectory, "DesignSettings.json");
        if (!File.Exists(settingsPath))
            throw new InvalidOperationException($"[WorkspaceConfigException] 未找到 DesignSettings.json：{settingsPath}");

        try
        {
            var json = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<WorkspaceDesignSettings>(json, JsonOptions);
            if (settings is null)
                throw new InvalidOperationException($"[WorkspaceConfigException] DesignSettings.json 解析结果为空：{settingsPath}");
            return settings;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"[WorkspaceConfigException] DesignSettings.json 解析失败：{settingsPath}\n{ex.Message}", ex);
        }
    }

    private static IReadOnlyList<WorkspacePackageVersion> ResolveRequiredWorkspacePackages(WorkspaceDesignSettings settings, string settingsPath, DbType dbType)
    {
        var packages = settings.Packages
            .Where(p => !string.IsNullOrWhiteSpace(p.PackageName) && !string.IsNullOrWhiteSpace(p.Version))
            .GroupBy(p => p.PackageName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();

        if (packages.Count == 0)
            throw new InvalidOperationException($"[WorkspaceConfigException] DesignSettings.json 缺少 Packages 配置：{settingsPath}");
        if (packages.Any(p => string.IsNullOrWhiteSpace(p.PackageName) || string.IsNullOrWhiteSpace(p.Version)))
            throw new InvalidOperationException($"[WorkspaceConfigException] DesignSettings.json 的 Packages 项必须同时提供 PackageName 和 Version：{settingsPath}");

        var designPackage = packages.LastOrDefault(p => string.Equals(p.PackageName, "Microsoft.EntityFrameworkCore.Design", StringComparison.OrdinalIgnoreCase));
        if (designPackage is null)
            throw new InvalidOperationException($"[WorkspaceConfigException] DesignSettings.json 的 Packages 中必须包含 Microsoft.EntityFrameworkCore.Design 版本配置：{settingsPath}");

        var requiredProviderPackage = dbType switch
        {
            DbType.SqlServer => "Microsoft.EntityFrameworkCore.SqlServer",
            DbType.PostgreSQL => "Npgsql.EntityFrameworkCore.PostgreSQL",
            DbType.MySQL => "MySQL.EntityFrameworkCore",
            _ => throw new NotSupportedException($"Unsupported database type: {dbType}")
        };

        if (packages.All(p => !string.Equals(p.PackageName, requiredProviderPackage, StringComparison.OrdinalIgnoreCase)))
        {
            packages.Add(new WorkspacePackageVersion
            {
                PackageName = requiredProviderPackage,
                Version = designPackage.Version
            });
        }

        return packages;
    }

    private static string BuildWorkspaceFactoryCode(DbConnectionProfile profile, string workspaceProjectName, string contextNamespace)
    {
        var escapedContextName = profile.ContextName.Replace("\"", "\"\"");
        var escapedWorkspaceProjectName = workspaceProjectName.Replace("\"", "\"\"");
        var providerUsing = profile.DbType switch
        {
            DbType.SqlServer => "using Microsoft.EntityFrameworkCore.SqlServer;",
            DbType.PostgreSQL => "using Npgsql.EntityFrameworkCore.PostgreSQL;",
            DbType.MySQL => "using MySQL.EntityFrameworkCore.Extensions;",
            _ => string.Empty
        };

        var providerCode = profile.DbType switch
        {
            DbType.SqlServer => $"options.UseSqlServer(connectionString, o => {{ o.MigrationsAssembly(\"{escapedWorkspaceProjectName}\"); o.EnableRetryOnFailure(maxRetryCount: 3); o.CommandTimeout(30); }});",
            DbType.PostgreSQL => $"options.UseNpgsql(connectionString, o => o.MigrationsAssembly(\"{escapedWorkspaceProjectName}\"));",
            DbType.MySQL => $"options.UseMySQL(connectionString, o => o.MigrationsAssembly(\"{escapedWorkspaceProjectName}\"));",
            _ => throw new NotSupportedException($"Unsupported database type: {profile.DbType}")
        };

        var template = @"using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
__PROVIDER_USING__
using __CONTEXT_NAMESPACE__;

namespace Tranbok.Migrations.WorkSpace;

public sealed class WorkspaceDesignTimeFactory : IDesignTimeDbContextFactory<__CONTEXT_NAME__>
{
    public __CONTEXT_NAME__ CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(""TRANBOK_DB_CONNECTION"")
            ?? throw new InvalidOperationException(""No connection string configured."");

        var options = new DbContextOptionsBuilder<__CONTEXT_NAME__>();
        __PROVIDER_CODE__
        return new __CONTEXT_NAME__(options.Options);
    }
}";

        return template
            .Replace("__CONTEXT_NAME__", escapedContextName)
            .Replace("__CONTEXT_NAMESPACE__", contextNamespace)
            .Replace("__PROVIDER_USING__", providerUsing)
            .Replace("__PROVIDER_CODE__", providerCode);
    }

    private static string ResolveDbContextNamespace(string domainProjectPath, string contextName)
    {
        var projectDirectory = Path.GetDirectoryName(domainProjectPath);
        if (string.IsNullOrWhiteSpace(projectDirectory))
            throw new InvalidOperationException($"[WorkspaceConfigException] 无法定位 Domain 项目目录：{domainProjectPath}");

        var classPattern = new Regex($@"\b(class|record)\s+{Regex.Escape(contextName)}\s*(?:\(|:|where|\{{)?", RegexOptions.Compiled);
        var fileScopedNamespacePattern = new Regex(@"^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*;", RegexOptions.Compiled | RegexOptions.Multiline);
        var blockNamespacePattern = new Regex(@"^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*\{", RegexOptions.Compiled | RegexOptions.Multiline);

        foreach (var file in Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            if (!classPattern.IsMatch(content))
                continue;

            var fileScopedNamespaceMatch = fileScopedNamespacePattern.Match(content);
            if (fileScopedNamespaceMatch.Success)
                return fileScopedNamespaceMatch.Groups[1].Value;

            var blockNamespaceMatch = blockNamespacePattern.Match(content);
            if (blockNamespaceMatch.Success)
                return blockNamespaceMatch.Groups[1].Value;
        }

        throw new InvalidOperationException($"[WorkspaceConfigException] 未能在 Domain 项目中找到 DbContext '{contextName}' 的命名空间：{domainProjectPath}");
    }

    private static async Task<ProcessResult> RunWorkspaceEfAsync(WorkspaceInfo workspace, string arguments, CancellationToken ct, string? connectionString = null)
    {
        var restore = await RunDotnetAsync(workspace.WorkspaceDirectory, $"restore \"{workspace.WorkspaceProjectPath}\"", ct, connectionString);
        if (!restore.Success)
            return new ProcessResult(false, restore.Output, $"[WorkspaceRestoreException] {restore.Error}");

        var build = await RunDotnetAsync(workspace.WorkspaceDirectory, $"build \"{workspace.WorkspaceProjectPath}\" --no-restore -v minimal", ct, connectionString);
        if (!build.Success)
        {
            var combinedOutput = string.Join(Environment.NewLine, new[] { restore.Output.TrimEnd(), build.Output.TrimEnd() }.Where(x => !string.IsNullOrWhiteSpace(x)));
            return new ProcessResult(false, combinedOutput, $"[WorkspaceBuildException] {build.Error}");
        }

        return await RunDotnetAsync(workspace.WorkspaceDirectory, arguments, ct, connectionString);
    }

    private static async Task<ProcessResult> RunDotnetAsync(string workingDir, string arguments, CancellationToken ct, string? connectionString = null)
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
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill();
            }
            catch
            {
            }

            return new ProcessResult(false, stdout.ToString(), "[WorkspaceOperationCanceledException] Operation cancelled.");
        }

        return new ProcessResult(process.ExitCode == 0, stdout.ToString(), stderr.ToString());
    }

    private static DateTime? ParseTimestamp(string ts)
    {
        if (ts.Length != 14)
            return null;

        return DateTime.TryParseExact(ts, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt)
            ? dt
            : null;
    }
}
