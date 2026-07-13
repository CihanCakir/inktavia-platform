namespace Aizen.Modules.FileStorage.Domain.Interface.Service;

[DocumentationInfo("File scanner interface", "Abstracts virus/malware scanning of objects in storage. Implementations may delegate to ClamAV, a cloud-based AV API, or return a no-op clean result.")]
public interface IFileScanner
{
    Task<FileScanResult> ScanAsync(string bucketName, string objectKey, CancellationToken ct);
}

[DocumentationInfo("File scan result", "Result of a virus/malware scan on a stored object.")]
public sealed class FileScanResult
{
    public bool IsClean { get; set; }
    public string? ThreatName { get; set; }
}
