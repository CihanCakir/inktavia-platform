using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Membership;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Membership;

/// <summary>GET /api/v1/mobile/membership/subscription — the caller's current active subscription, or null (no plan).</summary>
public sealed class GetMobileCurrentSubscriptionQuery : AizenQuery<MobileCurrentSubscriptionDto> { }

public sealed class GetMobileCurrentSubscriptionQueryHandler
    : AizenQueryHandler<GetMobileCurrentSubscriptionQuery, MobileCurrentSubscriptionDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantMembershipRemoteCall _membership;

    public GetMobileCurrentSubscriptionQueryHandler(
        IParticipantProfileResolver resolver, IParticipantMembershipRemoteCall membership)
    {
        _resolver = resolver;
        _membership = membership;
    }

    public override async Task<MobileCurrentSubscriptionDto?> Handle(
        GetMobileCurrentSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var resp = await _membership.GetSubscription();
        var sub = resp?.Body;
        return sub is null ? null : MobileMembershipMapper.MapSubscription(sub);
    }
}
