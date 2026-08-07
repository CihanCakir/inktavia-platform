using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// The offers received on the caller's own SR. The module owner-scopes the query (verifies OwnerUserId from the
/// asserted identity) and excludes Drafts, so a foreign/unknown SR yields a clean not-found. The BFF just projects
/// each offer to the cost-free mobile offer DTO.
/// </summary>
public sealed class GetMobileServiceRequestOffersQueryHandler
    : AizenQueryHandler<GetMobileServiceRequestOffersQuery, List<MobileServiceRequestOfferDto>>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IProviderNameResolver _providerNames;

    public GetMobileServiceRequestOffersQueryHandler(
        IParticipantProfileResolver resolver, IServiceRequestRemoteCall sr, IProviderNameResolver providerNames)
    {
        _resolver = resolver;
        _sr = sr;
        _providerNames = providerNames;
    }

    public override async Task<List<MobileServiceRequestOfferDto>?> Handle(
        GetMobileServiceRequestOffersQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var resp = await _sr.GetOwnerOffers(request.ServiceRequestId);
        var offers = resp?.Body?.Offers ?? new();

        // BE_MO2b — resolve every offer's provider display name in ONE batch call (no N+1). The provider profile
        // id is used only here to look up the name; it is never placed on the cost-free DTO.
        var names = await _providerNames.ResolveAsync(
            offers.Select(o => o.ProviderProfileId), cancellationToken);

        return offers
            .Select(o => MobileServiceRequestMapper.MapOffer(o, names.GetValueOrDefault(o.ProviderProfileId)))
            .ToList();
    }
}
