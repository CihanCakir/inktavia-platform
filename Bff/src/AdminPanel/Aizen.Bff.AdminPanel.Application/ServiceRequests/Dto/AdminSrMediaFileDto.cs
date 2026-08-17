namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;

/// <summary>
/// A single presigned file surfaced on the admin service-request detail so the admin panel can view every uploaded
/// image/file across an SR (request attachments, provider completion evidence, work-log evidence photos). Built
/// best-effort: <see cref="Url"/> is null when FileStorage could not presign the file, and <see cref="MimeType"/> is
/// null when metadata was unavailable — the frontend guards on both (<c>url ? &lt;img/&gt; : icon</c>).
/// </summary>
[DocumentationInfo("Admin SR media file", "A presigned file (request attachment / completion evidence / work-log photo) for admin viewing.")]
public sealed class AdminSrMediaFileDto
{
    /// <summary>The underlying FileStorage file id.</summary>
    public Guid FileId { get; set; }

    /// <summary>
    /// Which SR surface this file belongs to. Emitted as a string (not an enum) so the admin BFF's numeric enum
    /// serialization does not force the frontend to map integers: <see cref="AdminSrMediaSurfaces"/>.
    /// </summary>
    public string Surface { get; set; } = default!;

    /// <summary>Presigned read URL (≈60 min). Null when FileStorage could not presign this file.</summary>
    public string? Url { get; set; }

    /// <summary>Content type resolved from FileStorage metadata (e.g. <c>image/jpeg</c>). Null when unavailable.</summary>
    public string? MimeType { get; set; }

    /// <summary>True when <see cref="MimeType"/> is an <c>image/*</c> type — the frontend renders these as thumbnails.</summary>
    public bool IsImage { get; set; }

    /// <summary>Original file name from FileStorage metadata (drives the download affordance for non-images).</summary>
    public string? FileName { get; set; }

    /// <summary>Human label for the file (attachment title / work-log title). Null when the source has none.</summary>
    public string? Title { get; set; }

    /// <summary>Id of the owning row (attachment id / completion id / work-log id) — lets the frontend key/group items.</summary>
    public long? SourceId { get; set; }

    /// <summary>When the file was attached/logged (UTC), for ordering.</summary>
    public DateTime? CreatedAt { get; set; }
}

/// <summary>Canonical string values for <see cref="AdminSrMediaFileDto.Surface"/>.</summary>
public static class AdminSrMediaSurfaces
{
    public const string RequestAttachment = "RequestAttachment";
    public const string CompletionEvidence = "CompletionEvidence";
    public const string WorkLogPhoto = "WorkLogPhoto";
}
