using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;

public sealed class GetProviderJobDetailResponse
{
    // Assignment
    public long AssignmentId { get; init; }
    public long ServiceRequestId { get; init; }
    public long ServiceRequestOfferId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? AssignmentStatus { get; init; }
    public DateTime? ScheduledStartDate { get; init; }
    public DateTime? ScheduledEndDate { get; init; }
    public DateTime? ActualStartDate { get; init; }
    public DateTime? ActualEndDate { get; init; }
    public string? ProviderNotes { get; init; }

    // Service Request (provider-safe)
    public ProviderServiceRequestDto Request { get; set; } = default!;
    public List<WorkScopeItemDto> WorkScope { get; init; } = new();
    public List<ProviderAttachmentMetaDto> Attachments { get; init; } = new();
    public List<ServiceRequestStatusHistoryDto> Timeline { get; init; } = new();

    // Accepted offer (read-only)
    public ServiceRequestOfferDto? AcceptedOffer { get; init; }

    // Completion evidence (after photo)
    public Guid? CompletionEvidenceFileId { get; init; }
    public DateTime? CompletedAtUtc { get; init; }

    /// <summary>
    /// N3 — UTC deadline after which a still-pending completion is auto-approved
    /// (= SubmittedAt + AutoApproveWindowDays). Null until a completion is submitted (or once
    /// the completion is approved/rejected/disputed). Read-only; the provider app shows an
    /// informational countdown while the owner review is pending.
    /// </summary>
    public DateTime? AutoApproveAt { get; init; }
}
