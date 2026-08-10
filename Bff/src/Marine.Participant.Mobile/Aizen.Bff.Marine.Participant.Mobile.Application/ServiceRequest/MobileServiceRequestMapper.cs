using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.DisputeCase;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;
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

    public static OfferRejectReason? ParseOfferRejectReason(string? name) =>
        Enum.TryParse<OfferRejectReason>(name, ignoreCase: true, out var r) ? r : OfferRejectReason.Other;

    public static CompletionRejectReason? ParseCompletionRejectReason(string? name) =>
        Enum.TryParse<CompletionRejectReason>(name, ignoreCase: true, out var r) ? r : CompletionRejectReason.Other;

    public static ServiceRequestDisputeReason ParseDisputeReason(string? name) =>
        Enum.TryParse<ServiceRequestDisputeReason>(name, ignoreCase: true, out var r) ? r : ServiceRequestDisputeReason.Other;

    public static ServiceRequestDisputeStatus? ParseDisputeStatus(string? name) =>
        Enum.TryParse<ServiceRequestDisputeStatus>(name, ignoreCase: true, out var s) ? s : null;

    // ── BE_MO5 owner disputes (cost-free) ────────────────────────────────────────────────────────────
    // The owner sees only their own dispute + its SR header (list) and a read-only case that never carries
    // supplier cost / dealer margin / commission / provider-net, nor any provider/owner/reviewer/sender id.

    /// <summary>Projects an owner dispute-list row → the mobile contract (a straight cost-free copy).</summary>
    public static MobileDisputeListItemDto MapDisputeListItem(OwnerDisputeItemDto d) => new()
    {
        DisputeId = d.DisputeId,
        ServiceRequestId = d.ServiceRequestId,
        ServiceRequestCode = d.ServiceRequestCode,
        ServiceRequestTitle = d.ServiceRequestTitle,
        ServiceCategoryCode = d.ServiceCategoryCode,
        Status = d.Status,
        Reason = d.Reason,
        Description = d.Description,
        IsOpen = d.IsOpen,
        OpenedByMe = d.OpenedByMe,
        OpenedAt = d.OpenedAt,
        ResolvedAt = d.ResolvedAt,
    };

    /// <summary>Builds a list row from the just-opened dispute (module DTO) + the owner's SR header (from the
    /// owner-gated detail) so the FE can render/navigate immediately after opening. <paramref name="ownerUserId"/>
    /// decides <c>OpenedByMe</c>.</summary>
    public static MobileDisputeListItemDto MapOpenedDispute(
        ServiceRequestDisputeDto dispute, ServiceRequestDetailDto detail, long ownerUserId)
    {
        var r = detail.Request;
        return new MobileDisputeListItemDto
        {
            DisputeId = dispute.Id,
            ServiceRequestId = dispute.ServiceRequestId,
            ServiceRequestCode = r.RequestCode,
            ServiceRequestTitle = r.Title,
            ServiceCategoryCode = r.ServiceCategoryCode,
            Status = dispute.Status.ToString(),
            Reason = dispute.Reason.ToString(),
            Description = dispute.Description,
            IsOpen = dispute.Status != ServiceRequestDisputeStatus.Resolved
                     && dispute.Status != ServiceRequestDisputeStatus.Closed,
            OpenedByMe = dispute.OpenedByUserId == ownerUserId,
            OpenedAt = dispute.OpenedAt,
            ResolvedAt = dispute.ResolvedAt,
        };
    }

    /// <summary>Projects the module dispute-case (BE-S13a) → the owner's read-only cost-free case. Drops all cost /
    /// commission / provider-net figures and every user id; resolves evidence file ids → presigned read URLs
    /// (best-effort — a failure leaves the URL null and the FE shows a placeholder). Resolution outcome surfaces
    /// only once the admin resolves (the owner never resolves).</summary>
    public static async Task<MobileDisputeCaseDto> MapDisputeCaseAsync(
        GetDisputeCaseDetailResponse c, IFileStorageRemoteCall fileStorage, ILogger logger, CancellationToken ct)
    {
        var d = c.Dispute;
        var sr = c.ServiceRequest;

        var dto = new MobileDisputeCaseDto
        {
            DisputeId = d.Id,
            ServiceRequestId = d.ServiceRequestId,
            Status = d.Status.ToString(),
            Reason = d.Reason.ToString(),
            Description = d.Description,
            OpenedByMe = false, // set by the caller (needs the owner id); default false is safe
            OpenedAt = d.OpenedAt,

            IsResolved = d.Status == ServiceRequestDisputeStatus.Resolved,
            ResolvedAt = d.ResolvedAt,
            ResolutionOutcome = d.ResolutionOutcome?.ToString(),
            ResolutionNotes = d.ResolutionNotes,
            ResolutionRefundAmount = d.ResolutionRefundAmount,

            ServiceRequest = new MobileDisputeCaseServiceRequestDto
            {
                Id = sr.Id,
                RequestCode = sr.RequestCode,
                Title = sr.Title,
                Status = sr.Status.ToString(),
                ServiceCategoryCode = sr.ServiceCategoryCode,
                AssignedProviderName = sr.AssignedProviderName,
                CreatedAt = sr.CreatedAt,
                CancelledAt = sr.CancelledAt,
            },
            ReasonDetail = new MobileDisputeCaseReasonDto
            {
                DisputeReason = c.Reason.DisputeReason.ToString(),
                DisputeDescription = c.Reason.DisputeDescription,
                CancelReasonCode = c.Reason.CancelReasonCode?.ToString(),
                CompletionRejectReasonCode = c.Reason.CompletionRejectReasonCode?.ToString(),
            },
            StatusTimeline = c.StatusTimeline
                .OrderBy(h => h.OccurredAt)
                .Select(h => new MobileDisputeCaseTimelineEventDto
                {
                    FromStatus = h.FromStatus.ToString(),
                    ToStatus = h.ToStatus.ToString(),
                    Reason = h.Reason,
                    ActorType = h.ActorType.ToString(),
                    OccurredAt = h.OccurredAt,
                })
                .ToList(),
        };

        if (c.Economics is DisputeCaseEconomicsDto e)
        {
            dto.Economics = new MobileDisputeCaseEconomicsDto
            {
                CurrencyCode = e.CurrencyCode,
                ServiceAmount = e.ServiceAmount,
                CustomerTotalAmount = e.CustomerTotalAmount,
                Lines = e.Lines.Select(l => new MobileDisputeCaseEconomicsLineDto
                {
                    LineRef = l.LineRef,
                    ItemType = l.ItemType,
                    GrossBeforeDiscount = l.GrossBeforeDiscount,
                    CustomerDiscountAmount = l.CustomerDiscountAmount,
                    LineVatAmount = l.LineVatAmount,
                    LineTotalAmount = l.LineTotalAmount,
                }).ToList(),
            };
        }

        if (c.Completion is DisputeCaseCompletionDto comp)
        {
            dto.Completion = new MobileDisputeCaseCompletionDto
            {
                Status = comp.Status.ToString(),
                CompletionNotes = comp.CompletionNotes,
                EvidenceUrl = await MintReadUrlAsync(comp.EvidenceFileId, fileStorage, logger, ct),
                SubmittedAt = comp.SubmittedAt,
                ReviewedAt = comp.ReviewedAt,
                RejectReasonCode = comp.RejectReasonCode?.ToString(),
                ClientRating = comp.ClientRating,
            };
        }

        foreach (var w in c.WorkLogs)
        {
            dto.WorkLogs.Add(new MobileDisputeCaseWorkLogDto
            {
                LogType = w.LogType.ToString(),
                Title = w.Title,
                Description = w.Description,
                AttachmentUrl = await MintReadUrlAsync(w.AttachmentFileId, fileStorage, logger, ct),
                LoggedAt = w.LoggedAt,
            });
        }

        foreach (var m in c.Messages)
        {
            dto.Messages.Add(new MobileDisputeCaseMessageDto
            {
                SenderType = m.SenderType.ToString(),
                MessageType = m.MessageType.ToString(),
                Content = m.Content,
                AttachmentUrl = await MintReadUrlAsync(m.AttachmentFileId, fileStorage, logger, ct),
                CreatedAt = m.CreatedAt,
            });
        }

        if (c.PaymentState is DisputeCasePaymentStateDto p)
        {
            dto.PaymentState = new MobileDisputeCasePaymentStateDto
            {
                HasTransaction = p.HasTransaction,
                Status = p.Status,
                CurrencyCode = p.CurrencyCode,
                GrossAmount = p.GrossAmount,
                TotalRefundedAmount = p.TotalRefundedAmount,
                RefundableAmount = p.RefundableAmount,
                EscrowReleased = p.EscrowReleased,
                CapturedAt = p.CapturedAt,
                ReleasedAt = p.ReleasedAt,
                Refunds = p.RefundAllocations.Select(a => new MobileDisputeCaseRefundDto
                {
                    RefundReason = a.RefundReason,
                    RefundCause = a.RefundCause,
                    Amount = a.Amount,
                    RefundStatus = a.RefundStatus,
                    ProcessedAt = a.ProcessedAt,
                }).ToList(),
            };
        }

        return dto;
    }

    /// <summary>Mints a presigned read URL for a (nullable) file id, best-effort. Null id → null; a failure logs and
    /// returns null (the FE shows a placeholder). Shares the attachment read-url path/fix.</summary>
    private static async Task<string?> MintReadUrlAsync(
        Guid? fileId, IFileStorageRemoteCall fileStorage, ILogger logger, CancellationToken ct)
    {
        if (fileId is not Guid id || id == Guid.Empty) return null;
        try
        {
            var url = await fileStorage.CreateReadUrl(id, new CreateReadUrlRequest { ExpiresIn = ReadUrlTtl });
            return url?.Body?.ReadUrl;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not resolve read URL for dispute-case file {FileId}.", id);
            return null;
        }
    }

    // ── BE_MO2 offer projection (cost-free) ────────────────────────────────────────────────────────────
    // Only the customer-facing totals + line items + S3 FX cross. The module offer DTO already excludes cost /
    // commission / funding / provider-net; we additionally drop the provider profile/user ids the owner has no
    // use for. Totals are settlement-denominated (TRY), taken from the line SettlementCurrencyCode (never USD).

    public static MobileServiceRequestOfferDto MapOffer(ServiceRequestOfferDto o, string? providerName = null) => new()
    {
        OfferId = o.Id,
        ServiceRequestId = o.ServiceRequestId,
        Status = o.Status.ToString(),
        // BE_MO2b — the resolved provider display name (from the shared IProviderNameResolver), or null → the FE
        // shows its localized fallback. The provider profile/user id is NEVER exposed (name only).
        ProviderName = providerName,
        CurrencyCode = SettlementCurrencyOf(o),
        Subtotal = o.Subtotal,
        TaxTotal = o.TaxTotal,
        DiscountTotal = o.DiscountTotal,
        GrandTotal = o.GrandTotal,
        EstimatedStartDate = o.EstimatedStartDate,
        EstimatedEndDate = o.EstimatedEndDate,
        EstimatedDurationMinutes = o.EstimatedDurationMinutes,
        ExpiresAt = o.ExpiresAt,
        DepositType = o.DepositType.ToString(),
        DepositValue = o.DepositValue,
        PaymentTermsNote = o.PaymentTermsNote,
        WarrantyNote = o.WarrantyNote,
        Description = o.Description,
        SubmittedAt = o.SubmittedAt,
        CreatedAt = o.CreatedAt,
        IsActionable = o.Status is ServiceRequestOfferStatus.Submitted or ServiceRequestOfferStatus.UnderReview,
        Items = o.Items.OrderBy(i => i.SortOrder).Select(MapOfferItem).ToList(),
    };

    public static MobileServiceRequestOfferItemDto MapOfferItem(ServiceRequestOfferItemDto i) => new()
    {
        Id = i.Id,
        Title = i.Title,
        Description = i.Description,
        ItemType = i.ItemType.ToString(),
        Quantity = i.Quantity,
        UnitCode = i.UnitCode,
        UnitPrice = i.UnitPrice,
        TaxRate = i.TaxRate,
        LineSubtotal = i.LineSubtotal,
        TaxAmount = i.TaxAmount,
        DiscountAmount = i.DiscountAmount,
        LineTotal = i.LineTotal,
        SettlementCurrencyCode = i.SettlementCurrencyCode,
        SourceUnitPrice = i.SourceUnitPrice,
        SourceCurrencyCode = i.SourceCurrencyCode,
    };

    // ── BE_MO3 owner payment status (cost-free) ───────────────────────────────────────────────────────
    /// <summary>Projects the module payment-status response → the mobile owner contract. Cost-free: customer total
    /// + lifecycle status only. Null response (defensive) → a "None" status so the FE keeps its accept CTA.</summary>
    public static MobilePaymentStatusDto MapPaymentStatus(
        long serviceRequestId, Aizen.Modules.ServiceRequest.Abstraction.Response.Owner.GetServiceRequestPaymentStatusForOwnerResponse? s) => new()
    {
        ServiceRequestId = s?.ServiceRequestId ?? serviceRequestId,
        HasPayment       = s?.HasPayment ?? false,
        TransactionId    = s?.TransactionId,
        Status           = s?.Status ?? "None",
        Amount           = s?.Amount,
        CurrencyCode     = s?.CurrencyCode,
        PaidAt           = s?.PaidAt,
    };

    // ── BE_MO4 owner completion review (cost-free) ──────────────────────────────────────────────────────
    /// <summary>Projects the module completion DTO → the mobile owner review contract. Cost-free: status / notes /
    /// evidence / review fields only — the provider + reviewer user ids are dropped. The evidence read URL is
    /// resolved separately (best-effort) via <see cref="MapCompletionWithEvidenceUrlAsync"/>.</summary>
    public static MobileServiceRequestCompletionDto MapCompletion(ServiceRequestCompletionDto c) => new()
    {
        CompletionId    = c.Id,
        ServiceRequestId = c.ServiceRequestId,
        Status          = c.Status.ToString(),
        CompletionNotes = c.CompletionNotes,
        EvidenceFileId  = c.EvidenceFileId,
        SubmittedAt     = c.SubmittedAt,
        AutoApproveAt   = c.AutoApproveAt,
        ReviewedAt      = c.ReviewedAt,
        ReviewNotes     = c.ReviewNotes,
        RejectReasonCode = c.RejectReasonCode?.ToString(),
        ClientRating    = c.ClientRating,
        IsPendingReview = c.Status == ServiceRequestCompletionStatus.Submitted,
    };

    /// <summary>Completion projection + the evidence file's presigned read URL resolved (best-effort — a failure
    /// leaves the URL null and the FE shows a placeholder; shares the attachment read-url path/fix).</summary>
    public static async Task<MobileServiceRequestCompletionDto> MapCompletionWithEvidenceUrlAsync(
        ServiceRequestCompletionDto c, IFileStorageRemoteCall fileStorage, ILogger logger, CancellationToken ct)
    {
        var dto = MapCompletion(c);
        if (dto.EvidenceFileId is Guid fileId && fileId != Guid.Empty)
        {
            try
            {
                var url = await fileStorage.CreateReadUrl(fileId, new CreateReadUrlRequest { ExpiresIn = ReadUrlTtl });
                if (url?.Body is not null)
                {
                    dto.EvidenceUrl = url.Body.ReadUrl;
                    dto.EvidenceUrlExpiresAt = url.Body.ExpiresAt;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not resolve read URL for completion {CompletionId} evidence {FileId}.", dto.CompletionId, fileId);
            }
        }
        return dto;
    }

    /// <summary>The settlement currency the offer's totals are in (TRY) — from the line SettlementCurrencyCode,
    /// never the offer DTO's stale USD default.</summary>
    private static string SettlementCurrencyOf(ServiceRequestOfferDto o) =>
        o.Items.Select(i => i.SettlementCurrencyCode).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? "TRY";

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
