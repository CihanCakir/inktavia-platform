using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Domain.Entities.Assignment;
using Aizen.Modules.ServiceRequest.Domain.Entities.Completion;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Entities.WorkLog;

namespace Aizen.Modules.ServiceRequest.Repository.Mapping;

[DocumentationInfo("ServiceRequest mapping extensions", "Extension methods for mapping domain entities to DTOs.")]
public static class ServiceRequestMappingExtensions
{
    public static ServiceRequestDto ToDto(this ServiceRequestEntity entity) => new()
    {
        Id = entity.Id,
        RequestCode = entity.RequestCode,
        OwnerUserId = entity.OwnerUserId,
        VesselId = entity.VesselId,
        ServiceCategoryCode = entity.ServiceCategoryCode,
        ServiceTypeCode = entity.ServiceTypeCode,
        Title = entity.Title,
        Description = entity.Description,
        Status = entity.Status,
        Priority = entity.Priority,
        RequestedStartDate = entity.RequestedStartDate,
        RequestedEndDate = entity.RequestedEndDate,
        LocationCountryCode = entity.LocationCountryCode,
        LocationCityCode = entity.LocationCityCode,
        LocationMarinaName = entity.LocationMarinaName,
        LocationLatitude = entity.LocationLatitude,
        LocationLongitude = entity.LocationLongitude,
        ExpiresAt = entity.ExpiresAt,
        CreatedAt = entity.CreateDate ?? DateTime.UtcNow,
        UpdatedAt = entity.ModifyDate ?? entity.CreateDate ?? DateTime.UtcNow
    };

    public static ServiceRequestSummaryDto ToSummaryDto(this ServiceRequestEntity entity) => new()
    {
        Id = entity.Id,
        RequestCode = entity.RequestCode,
        Title = entity.Title,
        ServiceCategoryCode = entity.ServiceCategoryCode,
        ServiceTypeCode = entity.ServiceTypeCode,
        Status = entity.Status,
        Priority = entity.Priority,
        VesselId = entity.VesselId,
        OwnerUserId = entity.OwnerUserId,
        ProviderProfileId = entity.Assignment?.ProviderProfileId,
        LocationMarinaName = entity.LocationMarinaName,
        LocationCityCode = entity.LocationCityCode,
        LocationCountryCode = entity.LocationCountryCode,
        OwnerNotes = entity.OwnerNotes,
        RequestedStartDate = entity.RequestedStartDate,
        LastActivityAt = entity.ModifyDate ?? entity.CreateDate,
        OfferCount = entity.Offers.Count,
        HasActiveAssignment = entity.Assignment is not null,
        CreatedAt = entity.CreateDate ?? DateTime.UtcNow,
        UpdatedAt = entity.ModifyDate ?? entity.CreateDate ?? DateTime.UtcNow
    };

    public static ServiceRequestItemDto ToDto(this ServiceRequestItemEntity entity) => new()
    {
        Id = entity.Id,
        ItemType = entity.ItemType,
        Title = entity.Title,
        Description = entity.Description,
        Quantity = entity.Quantity,
        UnitCode = entity.UnitCode,
        EstimatedUnitPrice = entity.EstimatedUnitPrice,
        SortOrder = entity.SortOrder
    };

    public static ServiceRequestAttachmentDto ToDto(this ServiceRequestAttachmentEntity entity) => new()
    {
        Id = entity.Id,
        FileId = entity.FileId,
        AttachmentType = entity.AttachmentType,
        Title = entity.Title,
        Description = entity.Description,
        UploaderUserId = entity.UploaderUserId,
        UploaderActorType = entity.UploaderActorType,
        CreatedAt = entity.CreateDate ?? DateTime.UtcNow
    };

    public static ServiceRequestStatusHistoryDto ToDto(this ServiceRequestStatusHistoryEntity entity) => new()
    {
        Id = entity.Id,
        FromStatus = entity.FromStatus,
        ToStatus = entity.ToStatus,
        Reason = entity.Reason,
        ActorUserId = entity.ActorUserId,
        ActorType = entity.ActorType,
        OccurredAt = entity.OccurredAt
    };

    public static ServiceRequestOfferDto ToDto(this ServiceRequestOfferEntity entity) => new()
    {
        Id = entity.Id,
        ServiceRequestId = entity.ServiceRequestId,
        ProviderProfileId = entity.ProviderProfileId,
        ProviderUserId = entity.ProviderUserId,
        Status = entity.Status,
        TotalAmount = entity.TotalAmount,
        CurrencyCode = entity.CurrencyCode,
        Description = entity.Description,
        ProviderNotes = entity.ProviderNotes,
        EstimatedStartDate = entity.EstimatedStartDate,
        EstimatedEndDate = entity.EstimatedEndDate,
        EstimatedDurationMinutes = entity.EstimatedDurationMinutes,
        ExpiresAt = entity.ExpiresAt,
        Subtotal = entity.Subtotal,
        TaxTotal = entity.TaxTotal,
        DiscountTotal = entity.DiscountTotal,
        GrandTotal = entity.GrandTotal,
        DepositType = entity.DepositType,
        DepositValue = entity.DepositValue,
        PaymentTermsNote = entity.PaymentTermsNote,
        WarrantyNote = entity.WarrantyNote,
        SubmittedAt = entity.SubmittedAt,
        ViewedAt = entity.ViewedAt,
        Items = entity.Items.Select(i => i.ToDto()).ToList(),
        // BE-S3 — offer-level FX rate snapshots (empty for a TRY-only offer)
        FxSnapshots = entity.FxSnapshots.Select(f => new Abstraction.Dto.OfferFxSnapshotDto
        {
            SourceCurrencyCode     = f.SourceCurrencyCode,
            SettlementCurrencyCode = f.SettlementCurrencyCode,
            Rate                   = f.Rate,
            RateDate               = f.RateDate,
        }).ToList(),
        CreatedAt = entity.CreateDate ?? DateTime.UtcNow,
        UpdatedAt = entity.ModifyDate ?? entity.CreateDate ?? DateTime.UtcNow
    };

    public static ServiceRequestOfferItemDto ToDto(this ServiceRequestOfferItemEntity entity) => new()
    {
        Id = entity.Id,
        ItemType = entity.ItemType,
        Title = entity.Title,
        Description = entity.Description,
        Quantity = entity.Quantity,
        UnitPrice = entity.UnitPrice,
        CurrencyCode = entity.CurrencyCode,
        // BE-S3 — surface both figures: source (foreign) + converted TRY. Non-null SourceUnitPrice ⇒ the line was converted.
        SourceUnitPrice = entity.SourceUnitPrice,
        SourceCurrencyCode = entity.SourceUnitPrice.HasValue ? entity.CurrencyCode : null,
        SettlementCurrencyCode = Domain.Entities.Offer.OfferFxConstants.SettlementCurrency,
        SortOrder = entity.SortOrder,
        UnitCode = entity.UnitCode,
        TaxRate = entity.TaxRate,
        DiscountType = entity.DiscountType,
        DiscountValue = entity.DiscountValue,
        LineSubtotal = entity.LineSubtotal,
        TaxAmount = entity.TaxAmount,
        LineTotal = entity.LineTotal,
        DiscountAmount = entity.DiscountAmount
    };

    public static ServiceRequestAssignmentDto ToDto(this ServiceRequestAssignmentEntity entity) => new()
    {
        Id = entity.Id,
        ServiceRequestId = entity.ServiceRequestId,
        ServiceRequestOfferId = entity.ServiceRequestOfferId,
        ProviderProfileId = entity.ProviderProfileId,
        ProviderUserId = entity.ProviderUserId,
        AssignedTeamMemberId = entity.AssignedTeamMemberId,
        Status = entity.Status,
        ScheduledStartDate = entity.ScheduledStartDate,
        ScheduledEndDate = entity.ScheduledEndDate,
        ActualStartDate = entity.ActualStartDate,
        ActualEndDate = entity.ActualEndDate,
        ProviderNotes = entity.ProviderNotes,
        RejectionReason = entity.RejectionReason,
        CreatedAt = entity.CreateDate ?? DateTime.UtcNow
    };

    public static ServiceRequestWorkLogDto ToDto(this ServiceRequestWorkLogEntity entity) => new()
    {
        Id = entity.Id,
        ServiceRequestAssignmentId = entity.ServiceRequestAssignmentId,
        ProviderUserId = entity.ProviderUserId,
        LogType = entity.LogType,
        Title = entity.Title,
        Description = entity.Description,
        LocationLatitude = entity.LocationLatitude,
        LocationLongitude = entity.LocationLongitude,
        AttachmentFileId = entity.AttachmentFileId,
        LoggedAt = entity.LoggedAt
    };

    public static ServiceRequestCompletionDto ToDto(this ServiceRequestCompletionEntity entity) => new()
    {
        Id = entity.Id,
        ServiceRequestId = entity.ServiceRequestId,
        ServiceRequestAssignmentId = entity.ServiceRequestAssignmentId,
        ProviderUserId = entity.ProviderUserId,
        Status = entity.Status,
        CompletionNotes = entity.CompletionNotes,
        EvidenceFileId = entity.EvidenceFileId,
        SubmittedAt = entity.SubmittedAt,
        ReviewedAt = entity.ReviewedAt,
        ReviewedByUserId = entity.ReviewedByUserId,
        ReviewNotes = entity.ReviewNotes,
        ClientRating = entity.ClientRating
    };

    public static ServiceRequestDisputeDto ToDto(this ServiceRequestDisputeEntity entity) => new()
    {
        Id = entity.Id,
        ServiceRequestId = entity.ServiceRequestId,
        OpenedByUserId = entity.OpenedByUserId,
        OpenedByActorType = entity.OpenedByActorType,
        Status = entity.Status,
        Reason = entity.Reason,
        Description = entity.Description,
        ResolutionNotes = entity.ResolutionNotes,
        ResolvedByAdminUserId = entity.ResolvedByAdminUserId,
        ResolvedAt = entity.ResolvedAt,
        OpenedAt = entity.OpenedAt,
        ResolutionOutcome = entity.ResolutionOutcome,
        ResolutionRefundAmount = entity.ResolutionRefundAmount,
        PaymentOutcomeAppliedAt = entity.PaymentOutcomeAppliedAt
    };

    public static ServiceRequestDetailDto ToDetailDto(this ServiceRequestEntity entity) => new()
    {
        Request = entity.ToDto(),
        Items = entity.Items.Select(i => i.ToDto()).ToList(),
        Attachments = entity.Attachments.Select(a => a.ToDto()).ToList(),
        Offers = entity.Offers.Select(o => o.ToDto()).ToList(),
        Assignment = entity.Assignment?.ToDto(),
        Completion = entity.Completion?.ToDto(),
        Dispute = entity.Dispute?.ToDto(),
        StatusHistory = entity.StatusHistory.Select(s => s.ToDto()).ToList()
    };

    public static ServiceRequestMessageDto ToDto(this ServiceRequestMessageEntity entity) => new()
    {
        Id = entity.Id,
        SenderUserId = entity.SenderUserId,
        SenderType = entity.SenderType,
        MessageType = entity.MessageType,
        Content = entity.Content,
        IsRead = entity.IsRead,
        ReadAt = entity.ReadAt,
        AttachmentFileId = entity.AttachmentFileId,
        LocationLat = entity.LocationLat,
        LocationLng = entity.LocationLng,
        LocationLabel = entity.LocationLabel,
        CreatedAt = entity.CreateDate ?? DateTime.UtcNow
    };

    // --- Provider-specific mappings (privacy-filtered) ---

    /// <summary>
    /// Provider-safe projection: no OwnerUserId, coordinates snapped to ~500m grid.
    /// Same snapping algorithm as the discovery SQL projection.
    /// </summary>
    public static ProviderServiceRequestDto ToProviderDto(this ServiceRequestEntity entity) => new()
    {
        Id = entity.Id,
        RequestCode = entity.RequestCode,
        Title = entity.Title,
        Description = entity.Description,
        Status = entity.Status,
        Priority = entity.Priority,
        ServiceCategoryCode = entity.ServiceCategoryCode,
        ServiceTypeCode = entity.ServiceTypeCode,
        RequestedStartDate = entity.RequestedStartDate,
        RequestedEndDate = entity.RequestedEndDate,
        ExpiresAt = entity.ExpiresAt,
        LocationCountryCode = entity.LocationCountryCode,
        LocationCityCode = entity.LocationCityCode,
        LocationMarinaName = entity.LocationMarinaName,
        ApproxLatitude = SnapCoordinate(entity.LocationLatitude, entity.Id),
        ApproxLongitude = SnapCoordinate(entity.LocationLongitude, entity.Id),
        VesselId = entity.VesselId,
        VesselName = entity.VesselName,
        OwnerNotes = entity.OwnerNotes,
        PublishedAt = entity.PublishedAt,
        CreatedAt = entity.CreateDate ?? DateTime.UtcNow,
        UpdatedAt = entity.ModifyDate ?? entity.CreateDate ?? DateTime.UtcNow
    };

    public static WorkScopeItemDto ToWorkScopeDto(this ServiceRequestItemEntity entity) => new()
    {
        Id = entity.Id,
        ItemType = entity.ItemType,
        Title = entity.Title,
        Description = entity.Description,
        Quantity = entity.Quantity,
        UnitCode = entity.UnitCode,
        SortOrder = entity.SortOrder
    };

    public static ProviderAttachmentMetaDto ToProviderAttachmentMetaDto(this ServiceRequestAttachmentEntity entity) => new()
    {
        Id = entity.Id,
        FileId = entity.FileId,
        AttachmentType = entity.AttachmentType,
        Title = entity.Title,
        CreatedAt = entity.CreateDate ?? DateTime.UtcNow
    };

    /// <summary>
    /// Assembles the full provider detail aggregate. Filters offers to the caller's own only.
    /// </summary>
    public static ProviderServiceRequestDetailDto ToProviderDetailDto(this ServiceRequestEntity entity, long providerProfileId)
    {
        var myOffer = entity.Offers
            .FirstOrDefault(o => o.ProviderProfileId == providerProfileId && !o.IsDeleted);

        return new ProviderServiceRequestDetailDto
        {
            Request = entity.ToProviderDto(),
            WorkScope = entity.Items
                .Where(i => !i.IsDeleted)
                .OrderBy(i => i.SortOrder)
                .Select(i => i.ToWorkScopeDto())
                .ToList(),
            Attachments = entity.Attachments
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.CreateDate)
                .Select(a => a.ToProviderAttachmentMetaDto())
                .ToList(),
            MyOffer = myOffer?.ToDto(),
            Timeline = entity.StatusHistory
                .OrderByDescending(s => s.OccurredAt)
                .Select(s => s.ToDto())
                .ToList(),
            OfferCount = entity.Offers.Count(o => !o.IsDeleted),
            AttachmentCount = entity.Attachments.Count(a => !a.IsDeleted)
        };
    }

    /// <summary>
    /// Snaps a coordinate to a ~500m grid with deterministic per-row jitter.
    /// Same algorithm as the discovery SQL projection in ServiceRequestRepository.
    /// </summary>
    private static decimal? SnapCoordinate(decimal? raw, long entityId)
    {
        if (raw is null) return null;
        var jitter = (entityId % 7 - 3) * 0.001;
        return (decimal)(Math.Round(((double)raw.Value + jitter) / 0.005) * 0.005);
    }
}
