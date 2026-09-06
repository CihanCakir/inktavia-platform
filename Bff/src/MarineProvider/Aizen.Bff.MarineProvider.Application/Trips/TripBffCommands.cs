using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Trip;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Trip;

namespace Aizen.Bff.MarineProvider.Application.Trips;

/// <summary>Provider marks "en route" — opens live tracking for the accepted job.</summary>
public sealed class StartTripBffCommand : AizenCommand<TripActionResponse>
{
    public long ServiceRequestId { get; init; }
}

/// <summary>Provider position ping while en route (server-side throttled in the module).</summary>
public sealed class PingTripBffCommand : AizenCommand<TripActionResponse>
{
    public long ServiceRequestId { get; init; }
    public TripLocationRequest Body { get; init; } = default!;
}

/// <summary>Provider marks "arrived" — closes tracking + purges the trail.</summary>
public sealed class ArriveTripBffCommand : AizenCommand<TripActionResponse>
{
    public long ServiceRequestId { get; init; }
}

/// <summary>Provider cancels the trip — closes tracking + purges the trail.</summary>
public sealed class CancelTripBffCommand : AizenCommand<TripActionResponse>
{
    public long ServiceRequestId { get; init; }
}
