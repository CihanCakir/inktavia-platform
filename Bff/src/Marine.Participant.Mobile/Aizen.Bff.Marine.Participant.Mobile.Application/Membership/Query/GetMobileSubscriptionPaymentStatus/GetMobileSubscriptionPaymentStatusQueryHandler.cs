using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Membership;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Membership;

public sealed class GetMobileSubscriptionPaymentStatusQueryHandler
    : AizenQueryHandler<GetMobileSubscriptionPaymentStatusQuery, MobileMembershipPaymentStatusDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantMembershipRemoteCall _membership;

    public GetMobileSubscriptionPaymentStatusQueryHandler(
        IParticipantProfileResolver resolver, IParticipantMembershipRemoteCall membership)
    {
        _resolver = resolver;
        _membership = membership;
    }

    public override async Task<MobileMembershipPaymentStatusDto?> Handle(
        GetMobileSubscriptionPaymentStatusQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var resp = await _membership.GetPaymentStatus(request.TransactionId);
        return MobileMembershipMapper.MapPaymentStatus(resp?.Body);
    }
}
