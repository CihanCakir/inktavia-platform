using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest dispute reason enum", "Reason category for a dispute opened on a service request.")]
public enum ServiceRequestDisputeReason
{
    QualityIssue = 1,
    IncompleteWork = 2,
    PricingDispute = 3,
    TimelineIssue = 4,
    DamageOccurred = 5,
    NoShow = 6,
    Other = 99
}
