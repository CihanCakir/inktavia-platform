using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Repository.Services;

[DocumentationInfo("No-op file scanner", "Stub IFileScanner implementation that always returns a clean result. Replace with a real AV scanner (e.g. ClamAV) when ready.")]
public sealed class NoOpFileScanner : IFileScanner
{
    public Task<FileScanResult> ScanAsync(string bucketName, string objectKey, CancellationToken ct)
        => Task.FromResult(new FileScanResult { IsClean = true });
}
