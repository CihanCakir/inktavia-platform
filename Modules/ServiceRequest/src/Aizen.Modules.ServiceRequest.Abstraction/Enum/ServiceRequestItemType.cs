
namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest item type enum", "Classification of a line item within a service request.")]
public enum ServiceRequestItemType
{
    Service = 1,
    Product = 2,
    Installation = 3,
    Inspection = 4,
    Repair = 5,
    Emergency = 6,
    Other = 99
}
