namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;

[DocumentationInfo("Service request status count response",
    "One (status → count) row for the admin dashboard SR-volume chart (C2). Status is the lowercase enum key the BFF/FE use (e.g. WaitingForOffer → waitingforoffer).")]
public sealed class ServiceRequestStatusCountDto
{
    public string Status { get; set; } = default!;
    public int Count { get; set; }
}
