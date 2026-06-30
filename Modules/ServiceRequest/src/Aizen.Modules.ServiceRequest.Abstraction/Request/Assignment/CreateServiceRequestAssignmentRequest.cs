
namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Assignment;

[DocumentationInfo("Create assignment request", "Input for creating a service request assignment after offer acceptance.")]
public sealed class CreateServiceRequestAssignmentRequest
{
    public long OfferId { get; set; }
    public long? AssignedTeamMemberId { get; set; }
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
}
