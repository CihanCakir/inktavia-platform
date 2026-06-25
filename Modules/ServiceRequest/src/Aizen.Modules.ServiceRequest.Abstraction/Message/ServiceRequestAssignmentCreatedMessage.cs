using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request assignment created message", "Published when an assignment is created after offer acceptance.")]
public sealed class ServiceRequestAssignmentCreatedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long AssignmentId { get; set; }
    public long ProviderProfileId { get; set; }
    public long ProviderUserId { get; set; }
    public DateTime? ScheduledStartDate { get; set; }
}
