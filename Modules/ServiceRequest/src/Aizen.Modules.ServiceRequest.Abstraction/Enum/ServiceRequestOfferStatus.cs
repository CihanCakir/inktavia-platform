
namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest offer status enum", "Lifecycle status values for a provider offer on a service request.")]
public enum ServiceRequestOfferStatus
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    Accepted = 4,
    Rejected = 5,
    Withdrawn = 6,
    Expired = 7
}
