using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// The owner rejects a received offer with the N-E structured reason + optional note. The module reject does NOT
/// owner-check, so the BFF gates the SR against the resolved owner id (EnsureOwnedAsync) before proxying, then
/// re-reads the offer so the client shows the Rejected status. No money moves (accept → checkout is MO3).
/// </summary>
public sealed class RejectMobileServiceRequestOfferCommandHandler
    : AizenCommandHandler<RejectMobileServiceRequestOfferCommand, MobileServiceRequestOfferDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IProviderNameResolver _providerNames;

    public RejectMobileServiceRequestOfferCommandHandler(
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder holder,
        IServiceRequestRemoteCall sr,
        IProviderNameResolver providerNames)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
        _providerNames = providerNames;
    }

    public override async Task<MobileServiceRequestOfferDto?> Handle(
        RejectMobileServiceRequestOfferCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // The module reject trusts the caller — gate ownership here (clean not-found on a foreign/unknown SR).
        await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);

        await _sr.RejectOwnerOffer(request.ServiceRequestId, request.OfferId, new RejectServiceRequestOfferRequest
        {
            OfferId = request.OfferId,
            ReasonCode = MobileServiceRequestMapper.ParseOfferRejectReason(request.Request?.ReasonCode),
            Reason = string.IsNullOrWhiteSpace(request.Request?.Note) ? null : request.Request!.Note!.Trim(),
        });

        // Re-read so the client shows the Rejected status.
        var resp = await _sr.GetOwnerOffer(request.ServiceRequestId, request.OfferId);
        var offer = resp?.Body?.Offer
            ?? throw new AizenBusinessException("Offer not found.");

        var names = await _providerNames.ResolveAsync(new[] { offer.ProviderProfileId }, cancellationToken);
        return MobileServiceRequestMapper.MapOffer(offer, names.GetValueOrDefault(offer.ProviderProfileId));
    }
}
