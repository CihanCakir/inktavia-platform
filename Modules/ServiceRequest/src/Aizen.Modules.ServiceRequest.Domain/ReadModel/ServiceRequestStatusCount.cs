using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.ReadModel;

[DocumentationInfo("Service request status count read-model",
    "One (status → count) row from the admin status-breakdown aggregation (C2). Enum-typed; the handler maps it to the lowercase key the BFF/FE use.")]
public sealed class ServiceRequestStatusCount
{
    public ServiceRequestStatus Status { get; set; }
    public int Count { get; set; }
}
