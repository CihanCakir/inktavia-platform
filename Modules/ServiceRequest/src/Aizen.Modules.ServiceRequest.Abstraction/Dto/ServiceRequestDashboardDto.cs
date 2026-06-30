
namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest dashboard DTO", "Aggregated stats for the service request dashboard view.")]
public sealed class ServiceRequestDashboardDto
{
    public int TotalRequests { get; set; }
    public int OpenRequests { get; set; }
    public int InProgressRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int DisputedRequests { get; set; }
    public int CancelledRequests { get; set; }
    public int RequestsWithPendingOffers { get; set; }
    public int RequestsAwaitingApproval { get; set; }
}
