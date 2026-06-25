using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Offer;

[DocumentationInfo("ServiceRequest offer item entity", "Line item within a service offer with type, quantity, and pricing.")]
public sealed class ServiceRequestOfferItemEntity : AizenEntityWithAudit
{
    public long ServiceRequestOfferId { get; private set; }
    public ServiceRequestOfferItemType ItemType { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public int SortOrder { get; private set; }
    public bool IsDiscount { get; private set; }

    public ServiceRequestOfferEntity Offer { get; private set; } = default!;

    public ServiceRequestOfferItemEntity() { }

    public static ServiceRequestOfferItemEntity Create(
        long serviceRequestOfferId,
        ServiceRequestOfferItemType itemType,
        string title,
        string? description,
        int quantity,
        decimal unitPrice,
        string currencyCode,
        int sortOrder)
    {
        return new ServiceRequestOfferItemEntity
        {
            ServiceRequestOfferId = serviceRequestOfferId,
            ItemType = itemType,
            Title = title.Trim(),
            Description = description,
            Quantity = quantity > 0 ? quantity : 1,
            UnitPrice = unitPrice,
            CurrencyCode = currencyCode.ToUpperInvariant(),
            SortOrder = sortOrder,
            IsDiscount = itemType == ServiceRequestOfferItemType.Discount,
            IsActive = true
        };
    }
}
