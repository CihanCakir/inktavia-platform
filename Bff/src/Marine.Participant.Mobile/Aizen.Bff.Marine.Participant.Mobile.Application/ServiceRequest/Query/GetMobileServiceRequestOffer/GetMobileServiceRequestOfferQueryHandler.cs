using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// One received offer (full cost-free breakdown) for the caller's own SR. The module owner-scopes the read; a
/// foreign/unknown SR or offer yields a clean not-found.
/// </summary>
public sealed class GetMobileServiceRequestOfferQueryHandler
    : AizenQueryHandler<GetMobileServiceRequestOfferQuery, MobileServiceRequestOfferDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IProviderNameResolver _providerNames;

    public GetMobileServiceRequestOfferQueryHandler(
        IParticipantProfileResolver resolver, IServiceRequestRemoteCall sr, IProviderNameResolver providerNames)
    {
        _resolver = resolver;
        _sr = sr;
        _providerNames = providerNames;
    }

    public override async Task<MobileServiceRequestOfferDto?> Handle(
        GetMobileServiceRequestOfferQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var resp = await _sr.GetOwnerOffer(request.ServiceRequestId, request.OfferId);
        var offer = resp?.Body?.Offer
            ?? throw new AizenBusinessException("Offer not found.");

        // BE_MO2b — resolve the provider display name (one id, one batch call); name only, never the id.
        var names = await _providerNames.ResolveAsync(new[] { offer.ProviderProfileId }, cancellationToken);

        return MobileServiceRequestMapper.MapOffer(offer, names.GetValueOrDefault(offer.ProviderProfileId));
    }
}
