using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;

[DocumentationInfo("ServiceRequest item entity", "Represents a line item or specific service requirement within a service request.")]
public sealed class ServiceRequestItemEntity : AizenEntityWithAudit
{
    public long ServiceRequestId { get; private set; }
    public ServiceRequestItemType ItemType { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public int Quantity { get; private set; }
    public string? UnitCode { get; private set; }
    public decimal? EstimatedUnitPrice { get; private set; }
    public int SortOrder { get; private set; }

    public ServiceRequestEntity ServiceRequest { get; private set; } = default!;

    public ServiceRequestItemEntity() { }

    public static ServiceRequestItemEntity Create(
        long serviceRequestId,
        ServiceRequestItemType itemType,
        string title,
        string? description,
        int quantity,
        string? unitCode,
        decimal? estimatedUnitPrice,
        int sortOrder)
    {
        return new ServiceRequestItemEntity
        {
            ServiceRequestId = serviceRequestId,
            ItemType = itemType,
            Title = title.Trim(),
            Description = description,
            Quantity = quantity > 0 ? quantity : 1,
            UnitCode = unitCode,
            EstimatedUnitPrice = estimatedUnitPrice,
            SortOrder = sortOrder,
            IsActive = true
        };
    }

    public void Update(string title, string? description, int quantity, string? unitCode, decimal? estimatedUnitPrice)
    {
        Title = title.Trim();
        Description = description;
        Quantity = quantity > 0 ? quantity : 1;
        UnitCode = unitCode;
        EstimatedUnitPrice = estimatedUnitPrice;
    }
}
