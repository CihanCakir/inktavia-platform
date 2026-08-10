using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Membership;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Membership;

/// <summary>GET /api/v1/mobile/membership/plans — the active participant plans to compare/subscribe (cost-free).</summary>
public sealed class GetMobileMembershipPlansQuery : AizenQuery<List<MobileMembershipPlanDto>> { }

public sealed class GetMobileMembershipPlansQueryHandler
    : AizenQueryHandler<GetMobileMembershipPlansQuery, List<MobileMembershipPlanDto>>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantMembershipRemoteCall _membership;

    public GetMobileMembershipPlansQueryHandler(
        IParticipantProfileResolver resolver, IParticipantMembershipRemoteCall membership)
    {
        _resolver = resolver;
        _membership = membership;
    }

    public override async Task<List<MobileMembershipPlanDto>?> Handle(
        GetMobileMembershipPlansQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var resp = await _membership.GetPlans();
        return resp?.Body ?? new List<MobileMembershipPlanDto>();
    }
}
