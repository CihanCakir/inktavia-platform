using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Trip;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Trip;

namespace Aizen.Modules.ServiceRequest.Application.Command.Trip;

[DocumentationInfo("Ping trip command", "Provider position ping while en route (server-side throttled).")]
public sealed class PingTripCommand : AizenCommand<TripActionResponse>
{
    public PingTripCommand(long serviceRequestId, TripLocationRequest request)
    {
        ServiceRequestId = serviceRequestId;
        Request = request;
    }

    public long ServiceRequestId { get; }
    public TripLocationRequest Request { get; }
}
