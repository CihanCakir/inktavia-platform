using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Trip;

namespace Aizen.Modules.ServiceRequest.Application.Command.Trip;

[DocumentationInfo("Arrive trip command", "Provider marks 'arrived' — closes tracking, stores the summary, purges the raw trail.")]
public sealed class ArriveTripCommand : AizenCommand<TripActionResponse>
{
    public ArriveTripCommand(long serviceRequestId) => ServiceRequestId = serviceRequestId;
    public long ServiceRequestId { get; }
}
