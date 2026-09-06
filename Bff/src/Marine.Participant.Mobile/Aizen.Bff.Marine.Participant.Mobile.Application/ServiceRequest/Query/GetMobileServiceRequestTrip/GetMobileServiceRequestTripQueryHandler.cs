using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// The owner's current live trip for one of their SRs, for the map screen's initial render before the socket
/// attaches. Resolve → BFF-side ownership gate (EnsureOwnedAsync, defense-in-depth on top of the module's owner
/// scoping) → read the module trip → project cost-free. Returns null when there is no trip (the FE renders "not
/// started" / 404-style empty).
/// </summary>
public sealed class GetMobileServiceRequestTripQueryHandler
    : AizenQueryHandler<GetMobileServiceRequestTripQuery, MobileServiceRequestTripDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;

    public GetMobileServiceRequestTripQueryHandler(
        IParticipantProfileResolver resolver, IParticipantIdentityHolder holder, IServiceRequestRemoteCall sr)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
    }

    public override async Task<MobileServiceRequestTripDto?> Handle(
        GetMobileServiceRequestTripQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // Ownership gate (defense-in-depth). Throws a clean not-found for a foreign/unknown id.
        await MobileServiceRequestMapper.EnsureOwnedAsync(_sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);

        var trip = (await _sr.GetOwnerTrip(request.ServiceRequestId))?.Body?.Trip;
        return trip is null ? null : MobileServiceRequestMapper.MapTrip(trip);
    }
}
