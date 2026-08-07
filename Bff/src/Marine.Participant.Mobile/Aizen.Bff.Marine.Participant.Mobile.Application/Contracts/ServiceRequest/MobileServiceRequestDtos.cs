namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;

// BE_MO1 — owner service-request create / list / detail. COST-FREE: no economics fields (offers/pricing/checkout
// are MO2/MO3). Enums cross as their string names (mobile-friendly), mirroring the mobile Vessel contracts.

/// <summary>Compact service request for the owner's list screen.</summary>
public sealed class MobileServiceRequestListItemDto
{
    public long Id { get; set; }
    public string RequestCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string ServiceCategoryCode { get; set; } = default!;
    public string? ServiceTypeCode { get; set; }
    public string Status { get; set; } = default!;
    public string Priority { get; set; } = default!;
    public long VesselId { get; set; }
    public string? LocationMarinaName { get; set; }
    public string? LocationCityCode { get; set; }
    public DateTime? RequestedStartDate { get; set; }
    /// <summary>Count only (cost-free) — the offers inbox arrives in MO2.</summary>
    public int OfferCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
}

/// <summary>Paged owner list → the client's PagedResponse&lt;T&gt;.</summary>
public sealed class MobileServiceRequestListDto
{
    public List<MobileServiceRequestListItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}

/// <summary>One status-timeline entry for the detail screen.</summary>
public sealed class MobileServiceRequestTimelineEventDto
{
    public string FromStatus { get; set; } = default!;
    public string ToStatus { get; set; } = default!;
    public string? Reason { get; set; }
    public string ActorType { get; set; } = default!;
    public DateTime OccurredAt { get; set; }
}

/// <summary>One attachment, with a freshly-resolved presigned read URL (best-effort).</summary>
public sealed class MobileServiceRequestAttachmentDto
{
    public long Id { get; set; }
    public Guid FileId { get; set; }
    public string AttachmentType { get; set; } = default!;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? DownloadUrl { get; set; }
    public DateTime? DownloadUrlExpiresAt { get; set; }
}

/// <summary>Full detail for the owner detail screen (cost-free: request + timeline + attachments).</summary>
public sealed class MobileServiceRequestDetailDto
{
    public long Id { get; set; }
    public string RequestCode { get; set; } = default!;
    public long VesselId { get; set; }
    public string ServiceCategoryCode { get; set; } = default!;
    public string? ServiceTypeCode { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string Status { get; set; } = default!;
    public string Priority { get; set; } = default!;
    public DateTime? RequestedStartDate { get; set; }
    public DateTime? RequestedEndDate { get; set; }
    public string? LocationCountryCode { get; set; }
    public string? LocationCityCode { get; set; }
    public string? LocationMarinaName { get; set; }
    public decimal? LocationLatitude { get; set; }
    public decimal? LocationLongitude { get; set; }
    public string? OwnerNotes { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<MobileServiceRequestTimelineEventDto> Timeline { get; set; } = new();
    public List<MobileServiceRequestAttachmentDto> Attachments { get; set; } = new();

    /// <summary>FE convenience — Draft is editable; a non-terminal request is cancellable. The module is authoritative.</summary>
    public bool CanEdit { get; set; }
    public bool CanCancel { get; set; }
}

// ── Request payloads ─────────────────────────────────────────────────────────────────────────────

/// <summary>Create payload the mobile wizard submits (basic + details + attachments). Publish=true (default)
/// submits it to providers (Draft → Open); false keeps it as a Draft.</summary>
public sealed class CreateMobileServiceRequestRequest
{
    public long VesselId { get; set; }
    public string ServiceCategoryCode { get; set; } = default!;
    public string? ServiceTypeCode { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    /// <summary>Priority enum name (Low/Normal/High/Urgent/Emergency). Blank → Normal.</summary>
    public string? Priority { get; set; }
    public DateTime? RequestedStartDate { get; set; }
    public DateTime? RequestedEndDate { get; set; }
    public string? LocationCountryCode { get; set; }
    public string? LocationCityCode { get; set; }
    public string? LocationMarinaName { get; set; }
    public decimal? LocationLatitude { get; set; }
    public decimal? LocationLongitude { get; set; }
    public string? OwnerNotes { get; set; }
    /// <summary>Already-uploaded files (client-side presigned via /mobile/uploads) to attach to the new request.</summary>
    public List<MobileServiceRequestAttachmentInput>? Attachments { get; set; }
    public bool Publish { get; set; } = true;
}

public sealed class MobileServiceRequestAttachmentInput
{
    public Guid FileId { get; set; }
    /// <summary>Attachment type enum name (Photo/Video/Document/Invoice/Certificate/Other). Blank → Document.</summary>
    public string? AttachmentType { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
}

/// <summary>Edit payload (Draft). Full replace of the editable fields — the FE sends the whole form.</summary>
public sealed class UpdateMobileServiceRequestRequest
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string ServiceCategoryCode { get; set; } = default!;
    public string? ServiceTypeCode { get; set; }
    public string? Priority { get; set; }
    public DateTime? RequestedStartDate { get; set; }
    public DateTime? RequestedEndDate { get; set; }
    public string? LocationCountryCode { get; set; }
    public string? LocationCityCode { get; set; }
    public string? LocationMarinaName { get; set; }
    public decimal? LocationLatitude { get; set; }
    public decimal? LocationLongitude { get; set; }
    public string? OwnerNotes { get; set; }
}

/// <summary>Cancel payload — the N-E structured reason (enum name) + an optional free-text note.</summary>
public sealed class CancelMobileServiceRequestRequest
{
    /// <summary>ServiceRequestCancelReason name (NoLongerNeeded/FoundAnotherProvider/PriceTooHigh/
    /// ProviderUnresponsive/ChangedMind/Duplicate/Other). Blank → Other.</summary>
    public string? ReasonCode { get; set; }
    public string? Note { get; set; }
}

/// <summary>Attach a completed client-side upload to the request as a typed attachment.</summary>
public sealed class AddMobileServiceRequestAttachmentRequest
{
    public Guid FileId { get; set; }
    public string? AttachmentType { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
}
