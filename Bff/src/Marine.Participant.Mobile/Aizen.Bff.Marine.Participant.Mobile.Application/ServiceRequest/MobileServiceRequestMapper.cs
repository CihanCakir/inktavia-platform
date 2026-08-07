using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// Projects ServiceRequest module DTOs → the mobile owner contracts (BE_MO1). Cost-free: only the request +
/// status timeline + attachments cross; offers/economics are intentionally dropped (MO2). Enums map to their
/// string names. Attachment read URLs are minted per-file via FileStorage (no read cache → always fresh), the
/// same pattern the mobile Vessel documents use.
/// </summary>
internal static class MobileServiceRequestMapper
{
    private static readonly TimeSpan ReadUrlTtl = TimeSpan.FromMinutes(15);

    // A request that has reached one of these can no longer be edited/cancelled by the owner.
    private static readonly HashSet<ServiceRequestStatus> TerminalStatuses = new()
    {
        ServiceRequestStatus.Completed, ServiceRequestStatus.Cancelled,
        ServiceRequestStatus.Expired, ServiceRequestStatus.Closed,
        ServiceRequestStatus.DisputeResolved,
    };

    public static MobileServiceRequestListItemDto MapListItem(ServiceRequestSummaryDto s) => new()
    {
        Id = s.Id,
        RequestCode = s.RequestCode,
        Title = s.Title,
        ServiceCategoryCode = s.ServiceCategoryCode,
        ServiceTypeCode = s.ServiceTypeCode,
        Status = s.Status.ToString(),
        Priority = s.Priority.ToString(),
        VesselId = s.VesselId,
        LocationMarinaName = s.LocationMarinaName,
        LocationCityCode = s.LocationCityCode,
        RequestedStartDate = s.RequestedStartDate,
        OfferCount = s.OfferCount,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt,
        LastActivityAt = s.LastActivityAt,
    };

    /// <summary>Detail projection (no URLs). Timeline is chronological; offers/economics are intentionally omitted.</summary>
    public static MobileServiceRequestDetailDto MapDetail(ServiceRequestDetailDto d)
    {
        var r = d.Request;
        return new MobileServiceRequestDetailDto
        {
            Id = r.Id,
            RequestCode = r.RequestCode,
            VesselId = r.VesselId,
            ServiceCategoryCode = r.ServiceCategoryCode,
            ServiceTypeCode = r.ServiceTypeCode,
            Title = r.Title,
            Description = r.Description,
            Status = r.Status.ToString(),
            Priority = r.Priority.ToString(),
            RequestedStartDate = r.RequestedStartDate,
            RequestedEndDate = r.RequestedEndDate,
            LocationCountryCode = r.LocationCountryCode,
            LocationCityCode = r.LocationCityCode,
            LocationMarinaName = r.LocationMarinaName,
            LocationLatitude = r.LocationLatitude,
            LocationLongitude = r.LocationLongitude,
            OwnerNotes = null, // OwnerNotes lives on the summary/entity; detail carries it via the request DTO only when set
            ExpiresAt = r.ExpiresAt,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            CanEdit = r.Status == ServiceRequestStatus.Draft,
            CanCancel = !TerminalStatuses.Contains(r.Status),
            Timeline = d.StatusHistory
                .OrderBy(h => h.OccurredAt)
                .Select(h => new MobileServiceRequestTimelineEventDto
                {
                    FromStatus = h.FromStatus.ToString(),
                    ToStatus = h.ToStatus.ToString(),
                    Reason = h.Reason,
                    ActorType = h.ActorType.ToString(),
                    OccurredAt = h.OccurredAt,
                })
                .ToList(),
            Attachments = d.Attachments.Select(MapAttachment).ToList(),
        };
    }

    /// <summary>Detail + per-attachment presigned read URLs resolved (best-effort — a failure leaves the URL null).</summary>
    public static async Task<MobileServiceRequestDetailDto> MapDetailWithAttachmentUrlsAsync(
        ServiceRequestDetailDto d, IFileStorageRemoteCall fileStorage, ILogger logger, CancellationToken ct)
    {
        var dto = MapDetail(d);
        foreach (var att in dto.Attachments)
            await ResolveReadUrlAsync(att, fileStorage, logger, ct);
        return dto;
    }

    public static MobileServiceRequestAttachmentDto MapAttachment(ServiceRequestAttachmentDto a) => new()
    {
        Id = a.Id,
        FileId = a.FileId,
        AttachmentType = a.AttachmentType.ToString(),
        Title = a.Title,
        Description = a.Description,
        CreatedAt = a.CreatedAt,
    };

    public static async Task<MobileServiceRequestAttachmentDto> MapAttachmentWithUrlAsync(
        ServiceRequestAttachmentDto a, IFileStorageRemoteCall fileStorage, ILogger logger, CancellationToken ct)
    {
        var dto = MapAttachment(a);
        await ResolveReadUrlAsync(dto, fileStorage, logger, ct);
        return dto;
    }

    private static async Task ResolveReadUrlAsync(
        MobileServiceRequestAttachmentDto dto, IFileStorageRemoteCall fileStorage, ILogger logger, CancellationToken ct)
    {
        if (dto.FileId == Guid.Empty) return;
        try
        {
            var url = await fileStorage.CreateReadUrl(dto.FileId, new CreateReadUrlRequest { ExpiresIn = ReadUrlTtl });
            if (url?.Body is not null)
            {
                dto.DownloadUrl = url.Body.ReadUrl;
                dto.DownloadUrlExpiresAt = url.Body.ExpiresAt;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not resolve read URL for SR attachment {AttachmentId} (file {FileId}).", dto.Id, dto.FileId);
        }
    }

    // ── enum parsing (mobile string name → module enum, with safe defaults) ──────────────────────────

    public static ServiceRequestPriority ParsePriority(string? name) =>
        Enum.TryParse<ServiceRequestPriority>(name, ignoreCase: true, out var p) ? p : ServiceRequestPriority.Normal;

    public static ServiceRequestAttachmentType ParseAttachmentType(string? name) =>
        Enum.TryParse<ServiceRequestAttachmentType>(name, ignoreCase: true, out var t) ? t : ServiceRequestAttachmentType.Document;

    public static ServiceRequestCancelReason? ParseCancelReason(string? name) =>
        Enum.TryParse<ServiceRequestCancelReason>(name, ignoreCase: true, out var r) ? r : ServiceRequestCancelReason.Other;

    // ── ownership gate ───────────────────────────────────────────────────────────────────────────────
    // The module owner endpoints (detail/update/cancel/attachment) do NOT check that the request belongs to
    // the caller — they fetch by id and act. So the BFF MUST gate every by-id owner action against the resolved
    // owner id (from the asserted identity), or a caller could read/modify another owner's request. A mismatch
    // (or unknown id) is surfaced as a clean not-found, never leaking that the request exists.
    public static async Task<ServiceRequestDetailDto> EnsureOwnedAsync(
        IServiceRequestRemoteCall sr, long serviceRequestId, long ownerUserId, CancellationToken ct)
    {
        if (ownerUserId <= 0)
            throw new AizenBusinessException("Service request not found.");

        var resp = await sr.GetDetail(serviceRequestId);
        var detail = resp?.Body?.Detail;
        if (detail is null || detail.Request.OwnerUserId != ownerUserId)
            throw new AizenBusinessException("Service request not found.");

        return detail;
    }
}
