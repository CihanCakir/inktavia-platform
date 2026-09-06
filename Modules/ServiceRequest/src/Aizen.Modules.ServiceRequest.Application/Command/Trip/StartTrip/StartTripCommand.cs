using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Trip;

namespace Aizen.Modules.ServiceRequest.Application.Command.Trip;

[DocumentationInfo("Start trip command", "Provider marks 'en route' for an accepted job — opens live tracking.")]
public sealed class StartTripCommand : AizenCommand<TripActionResponse>
{
    public StartTripCommand(long serviceRequestId) => ServiceRequestId = serviceRequestId;
    public long ServiceRequestId { get; }
}
