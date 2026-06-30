using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Offer;

[DocumentationInfo("ServiceRequest offer entity", "An offer from a service provider responding to a service request.")]
public sealed class ServiceRequestOfferEntity : AizenEntityWithAudit
{
    public long ServiceRequestId { get; private set; }
    public long ProviderProfileId { get; private set; }
    public long ProviderUserId { get; private set; }
    public ServiceRequestOfferStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public string? Description { get; private set; }
    public string? ProviderNotes { get; private set; }
    public DateTime? EstimatedStartDate { get; private set; }
    public DateTime? EstimatedEndDate { get; private set; }
    public int? EstimatedDurationMinutes { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? WithdrawnAt { get; private set; }
    public string? WithdrawalReason { get; private set; }

    private readonly List<ServiceRequestOfferItemEntity> _items = new();
    public IReadOnlyCollection<ServiceRequestOfferItemEntity> Items => _items.AsReadOnly();

    public ServiceRequestOfferEntity() { }

    public static ServiceRequestOfferEntity Create(
        long serviceRequestId,
        long providerProfileId,
        long providerUserId,
        decimal totalAmount,
        string currencyCode,
        string? description,
        string? providerNotes,
        DateTime? estimatedStartDate,
        DateTime? estimatedEndDate,
        int? estimatedDurationMinutes,
        DateTime? expiresAt)
    {
        return new ServiceRequestOfferEntity
        {
            ServiceRequestId = serviceRequestId,
            ProviderProfileId = providerProfileId,
            ProviderUserId = providerUserId,
            Status = ServiceRequestOfferStatus.Draft,
            TotalAmount = totalAmount,
            CurrencyCode = currencyCode.ToUpperInvariant(),
            Description = description,
            ProviderNotes = providerNotes,
            EstimatedStartDate = estimatedStartDate,
            EstimatedEndDate = estimatedEndDate,
            EstimatedDurationMinutes = estimatedDurationMinutes,
            ExpiresAt = expiresAt,
            IsActive = true
        };
    }

    public void Submit() => Status = ServiceRequestOfferStatus.Submitted;

    public void Accept()
    {
        Status = ServiceRequestOfferStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
    }

    public void Reject(string? reason)
    {
        Status = ServiceRequestOfferStatus.Rejected;
        RejectedAt = DateTime.UtcNow;
        RejectionReason = reason;
    }

    public void Withdraw(string? reason)
    {
        Status = ServiceRequestOfferStatus.Withdrawn;
        WithdrawnAt = DateTime.UtcNow;
        WithdrawalReason = reason;
    }

    public void Update(
        decimal totalAmount,
        string currencyCode,
        string? description,
        string? providerNotes,
        DateTime? estimatedStartDate,
        DateTime? estimatedEndDate,
        int? estimatedDurationMinutes,
        DateTime? expiresAt)
    {
        TotalAmount = totalAmount;
        CurrencyCode = currencyCode.ToUpperInvariant();
        Description = description;
        ProviderNotes = providerNotes;
        EstimatedStartDate = estimatedStartDate;
        EstimatedEndDate = estimatedEndDate;
        EstimatedDurationMinutes = estimatedDurationMinutes;
        ExpiresAt = expiresAt;
    }

    public void AddItem(ServiceRequestOfferItemEntity item) => _items.Add(item);
    public void RecalculateTotal() => TotalAmount = _items.Sum(i => i.Quantity * i.UnitPrice);
}
