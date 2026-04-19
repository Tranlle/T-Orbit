using System.IO.Compression;
using System.Security.Cryptography;
using TOrbit.Plugin.RuntimeHost.Models;

namespace TOrbit.Plugin.RuntimeHost.Services;

public sealed class RuntimePackageService
{
    private readonly RuntimeConfigurationStore _store;

    public RuntimePackageService(RuntimeConfigurationStore store)
    {
        _store = store;
    }

    public RuntimeDeploymentResult DeployPackage(HostedAppProfile profile, string packageFilePath)
    {
        if (string.IsNullOrWhiteSpace(packageFilePath) || !File.Exists(packageFilePath))
            throw new FileNotFoundException("Release package was not found.", packageFilePath);

        var appRoot = _store.GetAppRoot(profile.Id);
        var packageDirectory = _store.GetPackageDirectory(profile.Id);
        var currentDirectory = _store.GetCurrentDeploymentDirectory(profile.Id);
        var extractDirectory = Path.Combine(appRoot, "extracting");

        Directory.CreateDirectory(appRoot);
        Directory.CreateDirectory(packageDirectory);

        if (Directory.Exists(extractDirectory))
            Directory.Delete(extractDirectory, true);

        Directory.CreateDirectory(extractDirectory);

        using (var archive = ZipFile.OpenRead(packageFilePath))
        {
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.FullName))
                    continue;

                var destinationPath = Path.GetFullPath(Path.Combine(extractDirectory, entry.FullName));
                if (!destinationPath.StartsWith(Path.GetFullPath(extractDirectory), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The archive contains an invalid entry path.");

                if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
                {
                    Directory.CreateDirectory(destinationPath);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                entry.ExtractToFile(destinationPath, overwrite: true);
            }
        }

        var detection = DetectEntry(extractDirectory);
        var packageHash = ComputeSha256(packageFilePath);
        var packageDestination = Path.Combine(packageDirectory, Path.GetFileName(packageFilePath));
        File.Copy(packageFilePath, packageDestination, true);

        if (Directory.Exists(currentDirectory))
            Directory.Delete(currentDirectory, true);

        Directory.Move(extractDirectory, currentDirectory);

        var manifest = new RuntimeDeploymentManifest
        {
            PackageFileName = Path.GetFileName(packageDestination),
            PackageHash = packageHash,
            DeployedAt = DateTimeOffset.Now,
            EntryRelativePath = detection.EntryRelativePath,
            RunWithDotnet = detection.RunWithDotnet
        };

        _store.SaveDeploymentManifest(profile.Id, manifest);

        return new RuntimeDeploymentResult
        {
            PackagePath = packageDestination,
            PackageHash = packageHash,
            DeployedAt = manifest.DeployedAt,
            EntryRelativePath = detection.EntryRelativePath,
            RunWithDotnet = detection.RunWithDotnet
        };
    }

    public RuntimeDeploymentManifest? GetDeploymentManifest(string profileId)
        => _store.LoadDeploymentManifest(profileId);

    private static (string EntryRelativePath, bool RunWithDotnet) DetectEntry(string rootDirectory)
    {
        var executable = Directory.GetFiles(rootDirectory, "*.exe", SearchOption.AllDirectories)
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(executable))
            return (Path.GetRelativePath(rootDirectory, executable), false);

        var dll = Directory.GetFiles(rootDirectory, "*.dll", SearchOption.AllDirectories)
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(dll))
            return (Path.GetRelativePath(rootDirectory, dll), true);

        throw new InvalidOperationException("No executable entry was found in the uploaded release package.");
    }

    private static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }
}
