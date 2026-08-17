using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// The owner approves a change order (BE_MO6). Reuses the S11 `ApproveServiceChangeOrder` command verbatim — the
/// module runs the incremental economics (Increase → new snapshot + capture-at-approve; Decrease → P10 refund) and is
/// idempotent on the CO ref, so a re-approve never double-charges; a P5/S9 breach flips the CO to Rejected (no
/// capture). The module approve does NOT owner-check, so the BFF gates ownership (EnsureOwnedAsync) before proxying.
/// After apply, reads the CO's incremental payment status (the MO3 seam pointed at the CO transaction) so the client
/// shows the result without a poll on the manual gateway. Cost-free.
/// </summary>
public sealed class ApproveMobileChangeOrderCommandHandler
    : AizenCommandHandler<ApproveMobileChangeOrderCommand, MobileChangeOrderApproveResultDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;

    public ApproveMobileChangeOrderCommandHandler(
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder holder,
        IServiceRequestRemoteCall sr)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
    }

    public override async Task<MobileChangeOrderApproveResultDto?> Handle(
        ApproveMobileChangeOrderCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // The module approve trusts the caller — gate ownership here (clean not-found on a foreign/unknown SR).
        await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);

        // Reuse the S11 apply engine exactly (incremental snapshot+capture / P10 refund; idempotent; breach→Rejected).
        var approved = await _sr.ApproveChangeOrder(request.ServiceRequestId, request.ChangeOrderId);
        var co = approved?.Body
            ?? throw new AizenBusinessException("Could not approve the change order.");

        // Read the CO's incremental payment status (capture-at-approve → already Paid on the manual gateway).
        var statusResp = await _sr.GetChangeOrderPaymentStatus(request.ServiceRequestId, request.ChangeOrderId);

        return new MobileChangeOrderApproveResultDto
        {
            ChangeOrder = MobileServiceRequestMapper.MapChangeOrder(co),
            Payment = MobileServiceRequestMapper.MapPaymentStatus(request.ServiceRequestId, statusResp?.Body),
        };
    }
}
