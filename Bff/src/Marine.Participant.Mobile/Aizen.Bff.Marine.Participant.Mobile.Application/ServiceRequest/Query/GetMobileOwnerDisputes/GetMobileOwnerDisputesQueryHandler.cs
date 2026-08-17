using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// The owner reads their own disputes (BE_MO5). Resolves the participant so the assertion carries the owner id, then
/// passes through the module owner-disputes query (scoped module-side by <c>sr.OwnerUserId == UserInfo.UserId</c>) and
/// maps each row to the cost-free mobile contract. The global open count crosses unchanged. No per-item gate is
/// needed — the module already scopes to the caller.
/// </summary>
public sealed class GetMobileOwnerDisputesQueryHandler
    : AizenQueryHandler<GetMobileOwnerDisputesQuery, MobileDisputeListDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IServiceRequestRemoteCall _sr;

    public GetMobileOwnerDisputesQueryHandler(
        IParticipantProfileResolver resolver,
        IServiceRequestRemoteCall sr)
    {
        _resolver = resolver;
        _sr = sr;
    }

    public override async Task<MobileDisputeListDto?> Handle(
        GetMobileOwnerDisputesQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var status = MobileServiceRequestMapper.ParseDisputeStatus(request.Status);

        var resp = await _sr.GetOwnerDisputes(request.PageIndex, request.PageSize, status);
        var body = resp?.Body;

        return new MobileDisputeListDto
        {
            PageIndex = body?.PageIndex ?? request.PageIndex,
            PageSize = body?.PageSize ?? request.PageSize,
            Total = body?.Total ?? 0,
            OpenCount = body?.OpenCount ?? 0,
            Items = body?.Items.Select(MobileServiceRequestMapper.MapDisputeListItem).ToList()
                    ?? new List<MobileDisputeListItemDto>(),
        };
    }
}
