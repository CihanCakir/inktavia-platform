
namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest offer item type enum", "Classification of a line item within a service offer.")]
public enum ServiceRequestOfferItemType
{
    Service = 1,
    Product = 2,
    Installation = 3,
    Delivery = 4,
    Labor = 5,
    Inspection = 6,
    EmergencyFee = 7,
    Discount = 8,
    Other = 99
}
