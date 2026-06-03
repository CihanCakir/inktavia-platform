using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Assignment;
using Aizen.Modules.ServiceRequest.Domain.Entities.Completion;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;

[DocumentationInfo("ServiceRequest entity", "Root aggregate representing a marine service request from a boat owner.")]
public sealed class ServiceRequestEntity : AizenEntityWithAudit
{
    public string RequestCode { get; private set; } = default!;
    public long OwnerUserId { get; private set; }
    public long VesselId { get; private set; }
    public string ServiceCategoryCode { get; private set; } = default!;
    public string? ServiceTypeCode { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public ServiceRequestStatus Status { get; private set; }
    public ServiceRequestPriority Priority { get; private set; }
    public DateTime? RequestedStartDate { get; private set; }
    public DateTime? RequestedEndDate { get; private set; }
    public string? LocationCountryCode { get; private set; }
    public string? LocationCityCode { get; private set; }
    public string? LocationMarinaName { get; private set; }
    public decimal? LocationLatitude { get; private set; }
    public decimal? LocationLongitude { get; private set; }
    public string? OwnerNotes { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancelReason { get; private set; }
    public long? CancelledByUserId { get; private set; }

    private readonly List<ServiceRequestItemEntity> _items = new();
    public IReadOnlyCollection<ServiceRequestItemEntity> Items => _items.AsReadOnly();

    private readonly List<ServiceRequestStatusHistoryEntity> _statusHistory = new();
    public IReadOnlyCollection<ServiceRequestStatusHistoryEntity> StatusHistory => _statusHistory.AsReadOnly();

    private readonly List<ServiceRequestAttachmentEntity> _attachments = new();
    public IReadOnlyCollection<ServiceRequestAttachmentEntity> Attachments => _attachments.AsReadOnly();

    private readonly List<ServiceRequestMessageEntity> _messages = new();
    public IReadOnlyCollection<ServiceRequestMessageEntity> Messages => _messages.AsReadOnly();

    private readonly List<ServiceRequestOfferEntity> _offers = new();
    public IReadOnlyCollection<ServiceRequestOfferEntity> Offers => _offers.AsReadOnly();

    public ServiceRequestAssignmentEntity? Assignment { get; private set; }
    public ServiceRequestCompletionEntity? Completion { get; private set; }
    public ServiceRequestDisputeEntity? Dispute { get; private set; }

    public ServiceRequestEntity() { }

    public static ServiceRequestEntity Create(
        string requestCode,
        long ownerUserId,
        long vesselId,
        string serviceCategoryCode,
        string? serviceTypeCode,
        string title,
        string? description,
        ServiceRequestPriority priority,
        DateTime? requestedStartDate,
        DateTime? requestedEndDate,
        string? locationCountryCode,
        string? locationCityCode,
        string? locationMarinaName,
        decimal? locationLatitude,
        decimal? locationLongitude,
        string? ownerNotes,
        DateTime? expiresAt)
    {
        return new ServiceRequestEntity
        {
            RequestCode = requestCode,
            OwnerUserId = ownerUserId,
            VesselId = vesselId,
            ServiceCategoryCode = serviceCategoryCode.ToUpperInvariant(),
            ServiceTypeCode = serviceTypeCode?.ToUpperInvariant(),
            Title = title.Trim(),
            Description = description,
            Status = ServiceRequestStatus.Draft,
            Priority = priority,
            RequestedStartDate = requestedStartDate,
            RequestedEndDate = requestedEndDate,
            LocationCountryCode = locationCountryCode?.ToUpperInvariant(),
            LocationCityCode = locationCityCode?.ToUpperInvariant(),
            LocationMarinaName = locationMarinaName,
            LocationLatitude = locationLatitude,
            LocationLongitude = locationLongitude,
            OwnerNotes = ownerNotes,
            ExpiresAt = expiresAt,
            IsActive = true
        };
    }

    public void UpdateProfile(
        string title,
        string? description,
        string serviceCategoryCode,
        string? serviceTypeCode,
        ServiceRequestPriority priority,
        DateTime? requestedStartDate,
        DateTime? requestedEndDate,
        string? locationCountryCode,
        string? locationCityCode,
        string? locationMarinaName,
        decimal? locationLatitude,
        decimal? locationLongitude,
        string? ownerNotes,
        DateTime? expiresAt)
    {
        Title = title.Trim();
        Description = description;
        ServiceCategoryCode = serviceCategoryCode.ToUpperInvariant();
        ServiceTypeCode = serviceTypeCode?.ToUpperInvariant();
        Priority = priority;
        RequestedStartDate = requestedStartDate;
        RequestedEndDate = requestedEndDate;
        LocationCountryCode = locationCountryCode?.ToUpperInvariant();
        LocationCityCode = locationCityCode?.ToUpperInvariant();
        LocationMarinaName = locationMarinaName;
        LocationLatitude = locationLatitude;
        LocationLongitude = locationLongitude;
        OwnerNotes = ownerNotes;
        ExpiresAt = expiresAt;
    }

    public void ChangeStatus(ServiceRequestStatus newStatus) => Status = newStatus;

    public void Publish() => Status = ServiceRequestStatus.Open;

    public void Cancel(long cancelledByUserId, string? reason)
    {
        Status = ServiceRequestStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancelReason = reason;
        CancelledByUserId = cancelledByUserId;
        IsActive = false;
    }

    public void AddItem(ServiceRequestItemEntity item) => _items.Add(item);
    public void AddStatusHistory(ServiceRequestStatusHistoryEntity entry) => _statusHistory.Add(entry);
    public void AddAttachment(ServiceRequestAttachmentEntity attachment) => _attachments.Add(attachment);
    public void AddMessage(ServiceRequestMessageEntity message) => _messages.Add(message);
    public void AddOffer(ServiceRequestOfferEntity offer) => _offers.Add(offer);
    public void SetAssignment(ServiceRequestAssignmentEntity assignment) => Assignment = assignment;
    public void SetCompletion(ServiceRequestCompletionEntity completion) => Completion = completion;
    public void SetDispute(ServiceRequestDisputeEntity dispute) => Dispute = dispute;
}
