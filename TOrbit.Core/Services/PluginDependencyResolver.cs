using TOrbit.Plugin.Core;
using TOrbit.Plugin.Core.Abstractions;

namespace TOrbit.Core.Services;

public sealed class PluginDependencyResolver : IPluginDependencyResolver
{
    public Task<PluginDependencyGraph> ResolveAsync(
        IEnumerable<PluginManifest> manifests,
        CancellationToken cancellationToken = default)
    {
        var manifestById = manifests.ToDictionary(manifest => manifest.Id, StringComparer.OrdinalIgnoreCase);
        var dependencies = manifestById.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyCollection<PluginDependency>)(pair.Value.Dependencies?.ToArray() ?? []),
            StringComparer.OrdinalIgnoreCase);

        var order = new List<string>();
        var visitState = new Dictionary<string, VisitState>(StringComparer.OrdinalIgnoreCase);

        foreach (var manifest in manifestById.Values)
            Visit(manifest, manifestById, visitState, order, cancellationToken);

        return Task.FromResult(new PluginDependencyGraph(dependencies, order));
    }

    private static void Visit(
        PluginManifest manifest,
        IReadOnlyDictionary<string, PluginManifest> manifestById,
        IDictionary<string, VisitState> visitState,
        ICollection<string> order,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (visitState.TryGetValue(manifest.Id, out var state))
        {
            if (state == VisitState.Visited)
                return;

            if (state == VisitState.Visiting)
                throw new InvalidOperationException($"Circular plugin dependency detected at '{manifest.Id}'.");
        }

        visitState[manifest.Id] = VisitState.Visiting;

        foreach (var dependency in manifest.Dependencies)
        {
            if (!manifestById.TryGetValue(dependency.PluginId, out var dependencyManifest))
            {
                if (dependency.IsOptional)
                    continue;

                throw new InvalidOperationException(
                    $"Plugin '{manifest.Id}' requires missing dependency '{dependency.PluginId}'.");
            }

            if (!IsVersionCompatible(dependencyManifest.Version, dependency.VersionRange))
            {
                if (dependency.IsOptional)
                    continue;

                throw new InvalidOperationException(
                    $"Plugin '{manifest.Id}' requires '{dependency.PluginId}' version '{dependency.VersionRange}', " +
                    $"but found '{dependencyManifest.Version}'.");
            }

            Visit(dependencyManifest, manifestById, visitState, order, cancellationToken);
        }

        visitState[manifest.Id] = VisitState.Visited;
        if (!order.Contains(manifest.Id, StringComparer.OrdinalIgnoreCase))
            order.Add(manifest.Id);
    }

    private static bool IsVersionCompatible(string actualVersion, string versionRange)
    {
        if (string.IsNullOrWhiteSpace(versionRange) || versionRange == "*")
            return true;

        return string.Equals(actualVersion, versionRange, StringComparison.OrdinalIgnoreCase);
    }

    private enum VisitState
    {
        Visiting,
        Visited
    }
}
