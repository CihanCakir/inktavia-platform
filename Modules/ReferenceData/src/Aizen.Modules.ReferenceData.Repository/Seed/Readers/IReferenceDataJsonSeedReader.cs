
namespace Aizen.Modules.ReferenceData.Repository.Seed.Readers;

/// <summary>Reads and deserializes JSON seed files from the configured seed root path.</summary>
[DocumentationInfo("Contract for reading typed JSON seed data from the file system.", "Implementations must use System.Text.Json with case-insensitive property matching.")]
public interface IReferenceDataJsonSeedReader
{
    /// <summary>
    /// Reads and deserializes a JSON file that contains a list of objects.
    /// </summary>
    /// <param name="relativePath">Path relative to the configured JsonRootPath (e.g. "Currency/currencies.json").</param>
    /// <param name="optional">When true, returns empty list if the file does not exist. When false, throws if missing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<T>> ReadListAsync<T>(string relativePath, bool optional = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads and deserializes a JSON file that contains a single object.
    /// </summary>
    /// <param name="relativePath">Path relative to the configured JsonRootPath.</param>
    /// <param name="optional">When true, returns null if the file does not exist. When false, throws if missing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<T?> ReadSingleAsync<T>(string relativePath, bool optional = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers all JSON files recursively under the given subdirectory and deserializes each as a list.
    /// </summary>
    /// <param name="relativeDirectory">Directory path relative to the configured JsonRootPath.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<T>> ReadAllInDirectoryAsync<T>(string relativeDirectory, CancellationToken cancellationToken = default);
}
