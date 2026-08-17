
namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest cancel reason enum",
    "Structured reason the owner picks when cancelling a service request (N-E). Auto-maps to a Payment RefundReason for deterministic refund allocation. Free-text stays as the note.")]
public enum ServiceRequestCancelReason
{
    NoLongerNeeded = 1,
    FoundAnotherProvider = 2,
    PriceTooHigh = 3,
    ProviderUnresponsive = 4,
    ChangedMind = 5,
    Duplicate = 6,
    Other = 99
}
