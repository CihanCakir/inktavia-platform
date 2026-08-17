using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Repository.Services;

[DocumentationInfo("File validation service", "Validates content type, extension, file size and checksum rules.")]
public sealed class FileValidationService : IFileValidationService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "video/mp4", "video/quicktime", "video/x-msvideo",
        "text/plain", "text/csv",
        "application/zip"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "jpg", "jpeg", "png", "gif", "webp", "svg",
        "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx",
        "mp4", "mov", "avi",
        "txt", "csv", "zip"
    };

    private static readonly Dictionary<FileCategory, long> SizeLimits = new()
    {
        [FileCategory.Image] = 10L * 1024 * 1024,
        [FileCategory.Document] = 50L * 1024 * 1024,
        [FileCategory.Video] = 500L * 1024 * 1024,
        [FileCategory.Invoice] = 20L * 1024 * 1024,
        [FileCategory.Certificate] = 10L * 1024 * 1024,
        [FileCategory.Report] = 50L * 1024 * 1024,
        [FileCategory.Other] = 100L * 1024 * 1024
    };

    public void ValidateContentType(string contentType)
    {
        if (!IsAllowedContentType(contentType))
            throw new InvalidOperationException($"Content type '{contentType}' is not allowed.");
    }

    public void ValidateExtension(string extension)
    {
        if (!IsAllowedExtension(extension))
            throw new InvalidOperationException($"File extension '{extension}' is not allowed.");
    }

    public void ValidateFileSize(long sizeInBytes, FileCategory category)
    {
        if (!SizeLimits.TryGetValue(category, out var limit))
            limit = SizeLimits[FileCategory.Other];

        if (sizeInBytes > limit)
            throw new InvalidOperationException($"File size {sizeInBytes} bytes exceeds the limit of {limit} bytes for category '{category}'.");
    }

    public bool ValidateChecksum(string expected, string actual)
        => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);

    public bool IsAllowedContentType(string contentType)
        => AllowedContentTypes.Contains(contentType);

    public bool IsAllowedExtension(string extension)
        => AllowedExtensions.Contains(extension.TrimStart('.'));
}
