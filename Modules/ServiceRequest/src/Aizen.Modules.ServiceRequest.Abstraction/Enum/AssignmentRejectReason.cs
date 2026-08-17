
namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest assignment reject reason enum",
    "Structured reason the provider picks when rejecting an assignment (N-E). Maps to a provider-fault RefundReason. Free-text stays as the note.")]
public enum AssignmentRejectReason
{
    Unavailable = 1,
    OutOfServiceArea = 2,
    CapacityFull = 3,
    PriceNotViable = 4,
    ScheduleConflict = 5,
    Other = 99
}
