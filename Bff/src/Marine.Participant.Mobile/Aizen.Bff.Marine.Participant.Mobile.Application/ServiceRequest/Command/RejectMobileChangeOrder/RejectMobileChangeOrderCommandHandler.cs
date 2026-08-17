using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ChangeOrder;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// The owner rejects a proposed change order (BE_MO6). Reuses the S11 reject verbatim — terminal, no economics. The
/// module reject does NOT owner-check, so the BFF gates ownership (EnsureOwnedAsync) before proxying. Cost-free.
/// </summary>
public sealed class RejectMobileChangeOrderCommandHandler
    : AizenCommandHandler<RejectMobileChangeOrderCommand, MobileChangeOrderDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;

    public RejectMobileChangeOrderCommandHandler(
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder holder,
        IServiceRequestRemoteCall sr)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
    }

    public override async Task<MobileChangeOrderDto?> Handle(
        RejectMobileChangeOrderCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);

        var reason = string.IsNullOrWhiteSpace(request.Request?.Reason) ? null : request.Request!.Reason!.Trim();

        var rejected = await _sr.RejectChangeOrder(
            request.ServiceRequestId, request.ChangeOrderId, new RejectServiceChangeOrderRequest { Reason = reason });
        var co = rejected?.Body
            ?? throw new AizenBusinessException("Could not reject the change order.");

        return MobileServiceRequestMapper.MapChangeOrder(co);
    }
}
