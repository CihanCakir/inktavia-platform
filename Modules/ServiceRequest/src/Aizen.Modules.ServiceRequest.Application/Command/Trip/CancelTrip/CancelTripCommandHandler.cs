using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Trip;
using Aizen.Modules.ServiceRequest.Application.Services.Trip;

namespace Aizen.Modules.ServiceRequest.Application.Command.Trip;

[DocumentationInfo("Cancel trip command handler", "Finalizes the trip as Cancelled: summary + trail purge + TripCancelled fan-out.")]
public sealed class CancelTripCommandHandler : AizenCommandHandler<CancelTripCommand, TripActionResponse>
{
    private readonly TripFinalizeService _finalize;
    public CancelTripCommandHandler(TripFinalizeService finalize) => _finalize = finalize;

    public override async Task<TripActionResponse?> Handle(CancelTripCommand request, CancellationToken ct)
        => await _finalize.FinalizeAsync(request.ServiceRequestId, arrived: false, ct);
}
