using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Assignment;

[DocumentationInfo("Create assignment response", "Response after creating a service request assignment.")]
public sealed class CreateServiceRequestAssignmentResponse(ServiceRequestAssignmentDto assignment)
{
    public ServiceRequestAssignmentDto Assignment { get; } = assignment;
}
