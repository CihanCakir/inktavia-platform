
namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest offer reject reason enum",
    "Structured reason the owner picks when rejecting a provider's offer (N-E). Free-text stays as the note.")]
public enum OfferRejectReason
{
    PriceTooHigh = 1,
    ChoseAnotherOffer = 2,
    ScopeMismatch = 3,
    Timing = 4,
    Other = 99
}
