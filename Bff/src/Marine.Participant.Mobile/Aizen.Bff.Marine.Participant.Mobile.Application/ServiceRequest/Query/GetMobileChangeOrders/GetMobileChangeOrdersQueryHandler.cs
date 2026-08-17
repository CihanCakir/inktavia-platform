using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// The owner reads the change orders on one of their own SRs (BE_MO6). The S11 list endpoint is NOT owner-scoped, so
/// the BFF owner-gates via EnsureOwnedAsync (a foreign/unknown SR → clean not-found) before proxying, then maps to the
/// cost-free mobile list (customer amounts + line inputs only — no provider net / snapshot ids / eligibility flags).
/// </summary>
public sealed class GetMobileChangeOrdersQueryHandler
    : AizenQueryHandler<GetMobileChangeOrdersQuery, MobileChangeOrderListDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;

    public GetMobileChangeOrdersQueryHandler(
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder holder,
        IServiceRequestRemoteCall sr)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
    }

    public override async Task<MobileChangeOrderListDto?> Handle(
        GetMobileChangeOrdersQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);

        var resp = await _sr.GetChangeOrders(request.ServiceRequestId);
        var list = resp?.Body;
        if (list is null)
            return new MobileChangeOrderListDto { ServiceRequestId = request.ServiceRequestId };

        return MobileServiceRequestMapper.MapChangeOrderList(list);
    }
}
