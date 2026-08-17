using System.Text.Json;
using Aizen.Modules.ReferenceData.Repository.Options;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.ReferenceData.Repository.Seed.Readers;

/// <summary>File-system-based implementation of IReferenceDataJsonSeedReader using System.Text.Json.</summary>
[DocumentationInfo(
    "Reads JSON seed files from disk using the configured JsonRootPath.",
    "Combines AppContext.BaseDirectory with ReferenceDataSeedOptions.JsonRootPath to resolve file paths. Uses case-insensitive JSON deserialization.")]
public sealed class ReferenceDataJsonSeedReader : IReferenceDataJsonSeedReader
{
    private readonly string _rootPath;
    private readonly bool _failOnMissingRequired;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public ReferenceDataJsonSeedReader(IOptions<ReferenceDataSeedOptions> options)
    {
        var opts = options.Value;
        _failOnMissingRequired = opts.FailOnMissingRequiredFile;
        _rootPath = Path.IsPathRooted(opts.JsonRootPath)
            ? opts.JsonRootPath
            : Path.Combine(AppContext.BaseDirectory, opts.JsonRootPath);
    }

    public async Task<IReadOnlyList<T>> ReadListAsync<T>(string relativePath, bool optional = false, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(relativePath);

        if (!File.Exists(fullPath))
        {
            if (optional) return Array.Empty<T>();
            if (_failOnMissingRequired)
                throw new FileNotFoundException($"Required ReferenceData seed file not found: {fullPath}", fullPath);
            return Array.Empty<T>();
        }

        await using var stream = File.OpenRead(fullPath);
        var result = await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions, cancellationToken)
                     ?? throw new InvalidOperationException($"Seed file deserialized to null: {fullPath}");
        return result;
    }

    public async Task<T?> ReadSingleAsync<T>(string relativePath, bool optional = false, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(relativePath);

        if (!File.Exists(fullPath))
        {
            if (optional) return default;
            if (_failOnMissingRequired)
                throw new FileNotFoundException($"Required ReferenceData seed file not found: {fullPath}", fullPath);
            return default;
        }

        await using var stream = File.OpenRead(fullPath);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<T>> ReadAllInDirectoryAsync<T>(string relativeDirectory, CancellationToken cancellationToken = default)
    {
        var fullDir = ResolvePath(relativeDirectory);

        if (!Directory.Exists(fullDir))
            return Array.Empty<T>();

        var jsonFiles = Directory.GetFiles(fullDir, "*.json", SearchOption.AllDirectories);
        var all = new List<T>();

        foreach (var file in jsonFiles.OrderBy(f => f))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var stream = File.OpenRead(file);
            var items = await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions, cancellationToken);
            if (items is not null)
                all.AddRange(items);
        }

        return all;
    }

    private string ResolvePath(string relativePath)
        => Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
}
