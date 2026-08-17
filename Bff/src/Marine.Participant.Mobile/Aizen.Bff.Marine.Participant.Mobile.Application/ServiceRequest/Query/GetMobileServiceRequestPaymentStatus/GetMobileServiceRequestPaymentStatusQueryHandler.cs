using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// The owner polls the payment status of their accepted SR (BE_MO3). The module read is owner-gated (UserInfo.UserId),
/// and the BFF also gates ownership (EnsureOwnedAsync) before proxying — a foreign/unknown SR is a clean not-found.
/// No capture logic: the status reflects the existing capture + webhook path. Cost-free: customer total + status.
/// </summary>
public sealed class GetMobileServiceRequestPaymentStatusQueryHandler
    : AizenQueryHandler<GetMobileServiceRequestPaymentStatusQuery, MobilePaymentStatusDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;

    public GetMobileServiceRequestPaymentStatusQueryHandler(
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder holder,
        IServiceRequestRemoteCall sr)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
    }

    public override async Task<MobilePaymentStatusDto?> Handle(
        GetMobileServiceRequestPaymentStatusQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);

        var resp = await _sr.GetOwnerPaymentStatus(request.ServiceRequestId);
        return MobileServiceRequestMapper.MapPaymentStatus(request.ServiceRequestId, resp?.Body);
    }
}
