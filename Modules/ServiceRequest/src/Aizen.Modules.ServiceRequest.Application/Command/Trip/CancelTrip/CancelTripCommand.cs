using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Trip;

namespace Aizen.Modules.ServiceRequest.Application.Command.Trip;

[DocumentationInfo("Cancel trip command", "Provider cancels the trip — closes tracking, stores the summary, purges the raw trail.")]
public sealed class CancelTripCommand : AizenCommand<TripActionResponse>
{
    public CancelTripCommand(long serviceRequestId) => ServiceRequestId = serviceRequestId;
    public long ServiceRequestId { get; }
}
