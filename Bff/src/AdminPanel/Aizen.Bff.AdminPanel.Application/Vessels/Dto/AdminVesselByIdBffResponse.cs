using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Modules.Vessel.Abstraction.Dto.Engine;
using Aizen.Modules.Vessel.Abstraction.Dto.Location;
using Aizen.Modules.Vessel.Abstraction.Dto.Specification;
using Aizen.Modules.Vessel.Abstraction.Dto.Status;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Dto;

/// <summary>
/// Enriched vessel-detail response for the admin `GET /vessels/{id}` (R5). Mirrors the module's <c>VesselDetailDto</c>
/// container shape (so the admin-web reads <c>body.vessel.{vessel,owners,documents,media,...}</c> unchanged) but with
/// owner display names resolved via Identity and media/document files presigned via FileStorage. Best-effort — a
/// downstream failure leaves the affected field empty and appends a warning; never a 500.
/// </summary>
[DocumentationInfo("Admin vessel-by-id BFF response", "Vessel detail with resolved owner names + presigned media/document URLs.")]
public sealed class AdminVesselByIdBffResponse
{
    public VesselDetailEnrichedBffDto? Vessel { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}

[DocumentationInfo("Vessel detail enriched BFF DTO", "Module vessel-detail container with enriched owners + presigned media/documents.")]
public sealed class VesselDetailEnrichedBffDto
{
    public VesselDto Vessel { get; set; } = default!;
    public List<VesselOwnerEnrichedBffDto> Owners { get; set; } = new();
    public VesselSpecificationDto? Specification { get; set; }
    public IReadOnlyList<VesselEngineDto> Engines { get; set; } = [];
    public List<VesselDocumentBffDto> Documents { get; set; } = new();
    public List<VesselMediaBffDto> Media { get; set; } = new();
    public VesselLocationSnapshotDto? CurrentLocation { get; set; }
    public IReadOnlyList<VesselStatusHistoryDto> StatusHistory { get; set; } = [];
}

[DocumentationInfo("Vessel owner enriched BFF DTO", "Vessel owner with display name/email/avatar resolved from Identity.")]
public sealed class VesselOwnerEnrichedBffDto
{
    public long Id { get; set; }
    public long VesselId { get; set; }
    public long UserId { get; set; }
    public long? UserProfileId { get; set; }
    public VesselOwnershipRole Role { get; set; }
    public VesselOwnershipStatus Status { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime? InvitedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RemovedAt { get; set; }

    // Resolved via Identity (null when unresolved — the FE falls back to #userId).
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
}
