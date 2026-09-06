using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Trip;
using Aizen.Modules.ServiceRequest.Application.Services.Trip;

namespace Aizen.Modules.ServiceRequest.Application.Command.Trip;

[DocumentationInfo("Arrive trip command handler", "Finalizes the trip as Arrived: summary + trail purge + TripArrived fan-out.")]
public sealed class ArriveTripCommandHandler : AizenCommandHandler<ArriveTripCommand, TripActionResponse>
{
    private readonly TripFinalizeService _finalize;
    public ArriveTripCommandHandler(TripFinalizeService finalize) => _finalize = finalize;

    public override async Task<TripActionResponse?> Handle(ArriveTripCommand request, CancellationToken ct)
        => await _finalize.FinalizeAsync(request.ServiceRequestId, arrived: true, ct);
}
