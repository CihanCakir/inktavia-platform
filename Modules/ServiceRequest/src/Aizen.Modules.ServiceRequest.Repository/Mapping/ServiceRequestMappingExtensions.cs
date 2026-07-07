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
        Items = entity.Items.Select(i => i.ToDto()).ToList(),
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
        SortOrder = entity.SortOrder,
        IsDiscount = entity.IsDiscount
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
        OpenedAt = entity.OpenedAt
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
        CreatedAt = entity.CreateDate ?? DateTime.UtcNow
    };
}
