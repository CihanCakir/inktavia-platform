namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Upload;

/// <summary>Request a client-side presigned upload session (M4f canonical pattern). The client then PUTs the raw
/// bytes directly to the returned presigned URL — the bytes NEVER pass through the BFF.</summary>
public sealed class CreateMobileUploadSessionRequest
{
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeInBytes { get; set; }
    /// <summary>Optional category (Image / Document / Video …). Inferred from ContentType when omitted.</summary>
    public string? Category { get; set; }
}

/// <summary>The presigned PUT session. The client must PUT with the EXACT <see cref="RequiredContentType"/> header
/// (the URL is signed against it) to a device-reachable host (PublicServiceUrl), then call complete.</summary>
public sealed class MobileUploadSessionResponse
{
    public Guid FileId { get; set; }
    public string UploadUrl { get; set; } = default!;
    public string UploadSessionCode { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public string RequiredContentType { get; set; } = default!;
}

/// <summary>Finalize the upload after the client's PUT (the module verifies the object exists in storage).</summary>
public sealed class CompleteMobileUploadRequest
{
    public string UploadSessionCode { get; set; } = default!;
}

public sealed class MobileUploadCompleteResponse
{
    public Guid FileId { get; set; }
}
