
namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

[DocumentationInfo("ServiceRequest completion reject reason enum",
    "Structured reason the owner picks when rejecting a submitted completion (N-E). Maps to a service-not-delivered RefundReason. Free-text stays as the note.")]
public enum CompletionRejectReason
{
    WorkIncomplete = 1,
    QualityIssue = 2,
    NotAsAgreed = 3,
    Other = 99
}
