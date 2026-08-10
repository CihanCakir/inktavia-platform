using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// The owner polls a change order's incremental escrow payment status (BE_MO6). Owner-gates the SR (EnsureOwnedAsync),
/// then reads the module CO payment-status (the MO3 transaction-status seam pointed at the CO's own transaction — the
/// SR payment-status reads the original acceptance escrow, not this one) and maps to the shared cost-free mobile
/// payment DTO. Returns None until the CO has an incremental transaction (Decrease / still-Proposed).
/// </summary>
public sealed class GetMobileChangeOrderPaymentStatusQueryHandler
    : AizenQueryHandler<GetMobileChangeOrderPaymentStatusQuery, MobilePaymentStatusDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;

    public GetMobileChangeOrderPaymentStatusQueryHandler(
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder holder,
        IServiceRequestRemoteCall sr)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
    }

    public override async Task<MobilePaymentStatusDto?> Handle(
        GetMobileChangeOrderPaymentStatusQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);

        var resp = await _sr.GetChangeOrderPaymentStatus(request.ServiceRequestId, request.ChangeOrderId);
        return MobileServiceRequestMapper.MapPaymentStatus(request.ServiceRequestId, resp?.Body);
    }
}
