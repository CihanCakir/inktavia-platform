using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// The owner opens a dispute on one of their own SRs (BE_MO5). The shared module open endpoint resolves the actor
/// from roles (→ Owner for an asserted participant) but does NOT owner-gate the SR, so the BFF gates ownership
/// (EnsureOwnedAsync) before proxying — a foreign/unknown SR yields a clean not-found. Returns the opened dispute as
/// a cost-free list row (enriched with the SR header) so the client can navigate straight to the case.
/// </summary>
public sealed class OpenMobileDisputeCommandHandler
    : AizenCommandHandler<OpenMobileDisputeCommand, MobileDisputeListItemDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;

    public OpenMobileDisputeCommandHandler(
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder holder,
        IServiceRequestRemoteCall sr)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
    }

    public override async Task<MobileDisputeListItemDto?> Handle(
        OpenMobileDisputeCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var description = request.Request?.Description?.Trim();
        if (string.IsNullOrWhiteSpace(description))
            throw new AizenBusinessException("A description is required to open a dispute.");

        var ownerUserId = _holder.UserId ?? 0;

        // The module open trusts the caller — gate ownership here (clean not-found on a foreign/unknown SR).
        var detail = await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, ownerUserId, cancellationToken);

        var reason = MobileServiceRequestMapper.ParseDisputeReason(request.Request?.ReasonCode);

        var opened = await _sr.OpenDispute(request.ServiceRequestId, new OpenServiceRequestDisputeRequest
        {
            Reason = reason,
            Description = description!,
        });

        var dispute = opened?.Body?.Dispute
            ?? throw new AizenBusinessException("Could not open the dispute.");

        return MobileServiceRequestMapper.MapOpenedDispute(dispute, detail, ownerUserId);
    }
}
