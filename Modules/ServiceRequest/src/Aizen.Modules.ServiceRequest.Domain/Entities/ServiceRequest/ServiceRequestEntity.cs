using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Assignment;
using Aizen.Modules.ServiceRequest.Domain.Entities.Completion;
using Aizen.Modules.ServiceRequest.Domain.Entities.Conversation;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Entities.WorkPhase;

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

    public string? Category { get; private set; }
    public string? VesselName { get; private set; }
    public string? RequestedByEmail { get; private set; }
    public string? AssignedProviderName { get; private set; }
    public DateTimeOffset? DisputedAt { get; private set; }
    public string? DisputeReason { get; private set; }

    /// <summary>
    /// Payment module transaction ID created when offer is accepted (escrow held).
    /// Populated by AcceptServiceRequestOfferCommandHandler via IPaymentModuleRemoteCall.
    /// Required by ReleasePaymentCommandHandler to release escrow.
    /// </summary>
    public long? PaymentTransactionId { get; private set; }

    /// <summary>UTC timestamp when the request transitioned from Draft to Open. Idempotent: a republish does not overwrite.</summary>
    public DateTime? PublishedAt { get; private set; }

    /// <summary>UTC timestamp of the last owner content edit (UpdateProfile). Not set by publish, status change, or offer flows.</summary>
    public DateTime? ContentUpdatedAt { get; private set; }

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

    private readonly List<WorkPhaseEntity> _workPhases = new();
    public IReadOnlyCollection<WorkPhaseEntity> WorkPhases => _workPhases.AsReadOnly();

    private readonly List<ServiceRequestConversationEntity> _conversations = new();
    public IReadOnlyCollection<ServiceRequestConversationEntity> Conversations => _conversations.AsReadOnly();

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
        ContentUpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(ServiceRequestStatus newStatus) => Status = newStatus;

    public void Publish()
    {
        Status = ServiceRequestStatus.Open;
        PublishedAt ??= DateTime.UtcNow;
    }

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

    public void MarkCompleted(DateTimeOffset completedAt)
    {
        Status = ServiceRequestStatus.Completed;
    }

    public void MarkDisputed(string reason, DateTimeOffset disputedAt)
    {
        DisputedAt = disputedAt;
        DisputeReason = reason;
    }

    public void Assign(long providerId, string providerName)
    {
        AssignedProviderName = providerName;
        Status = ServiceRequestStatus.Assigned;
    }

    /// <summary>
    /// Stores the Payment module transaction ID after escrow is created.
    /// Called immediately after IPaymentModuleRemoteCall.CreateEscrowAsync succeeds.
    /// </summary>
    public void SetPaymentTransaction(long transactionId)
    {
        PaymentTransactionId = transactionId;
    }

    public void ReleasePayment()
    {
        Status = ServiceRequestStatus.Closed;
    }

    public void UpdateCategory(string? category) => Category = category;
    public void UpdateDenormalized(string? vesselName, string? requestedByEmail) { VesselName = vesselName; RequestedByEmail = requestedByEmail; }
    public void AddWorkPhase(WorkPhaseEntity phase) => _workPhases.Add(phase);
    public void AddConversation(ServiceRequestConversationEntity conversation) => _conversations.Add(conversation);
}
