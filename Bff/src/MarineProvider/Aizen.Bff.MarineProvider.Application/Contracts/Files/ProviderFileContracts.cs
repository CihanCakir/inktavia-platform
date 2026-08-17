using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;

namespace Aizen.Bff.MarineProvider.Application.Contracts.Files;

public sealed class CreateUploadSessionBffRequest
{
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long Size { get; set; }
    public string Category { get; set; } = "Document";
}

/// <summary>Narrowed response — never expose BucketName or ObjectKey to the frontend.</summary>
public sealed class CreateUploadSessionBffResponse
{
    public Guid FileId { get; set; }
    public string UploadSessionCode { get; set; } = default!;
    public string UploadUrl { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public Dictionary<string, string> RequiredHeaders { get; set; } = new();
}

public sealed class CompleteUploadBffRequest
{
    public string UploadSessionCode { get; set; } = default!;
}

public sealed class CompleteUploadBffResponse
{
    public Guid FileId { get; set; }
    public string Status { get; set; } = default!;
    public string Message { get; set; } = string.Empty;
}

public sealed class AttachDocumentBffRequest
{
    public Guid FileId { get; set; }
    public string DocumentType { get; set; } = default!;
    public string? Issuer { get; set; }
}

public sealed class AttachDocumentBffResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ProviderDocumentDto? Document { get; set; }
}

public sealed class DocumentAccessUrlBffResponse
{
    public string Url { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}

public sealed class DeleteDocumentBffResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
