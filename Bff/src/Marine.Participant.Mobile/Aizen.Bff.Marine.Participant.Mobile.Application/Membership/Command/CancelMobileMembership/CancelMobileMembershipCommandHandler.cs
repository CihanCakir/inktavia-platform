using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Membership;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Membership;

public sealed class CancelMobileMembershipCommandHandler
    : AizenCommandHandler<CancelMobileMembershipCommand, MobileCancelSubscriptionDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantMembershipRemoteCall _membership;

    public CancelMobileMembershipCommandHandler(
        IParticipantProfileResolver resolver, IParticipantMembershipRemoteCall membership)
    {
        _resolver = resolver;
        _membership = membership;
    }

    public override async Task<MobileCancelSubscriptionDto?> Handle(
        CancelMobileMembershipCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason!.Trim();
        var resp = await _membership.Cancel(reason);
        var result = resp?.Body
            ?? throw new AizenBusinessException("Could not cancel the subscription.");

        return MobileMembershipMapper.MapCancel(result);
    }
}
