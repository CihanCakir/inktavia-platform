using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// The owner reads the cost-free dispute case for one of their own SRs (BE_MO5). Owner-gates the SR via
/// EnsureOwnedAsync (primary gate — a foreign/unknown SR yields a clean not-found), then reads the module case (the
/// owner module endpoint also gates by SR ownership; defense-in-depth) and verifies the returned case belongs to the
/// gated SR before mapping. The projection drops all cost / commission / provider-net figures and every user id, and
/// resolves evidence file ids → presigned read URLs (best-effort). The owner is read-only — no resolve/status path.
/// </summary>
public sealed class GetMobileDisputeCaseQueryHandler
    : AizenQueryHandler<GetMobileDisputeCaseQuery, MobileDisputeCaseDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<GetMobileDisputeCaseQueryHandler> _logger;

    public GetMobileDisputeCaseQueryHandler(
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder holder,
        IServiceRequestRemoteCall sr,
        IFileStorageRemoteCall fileStorage,
        ILogger<GetMobileDisputeCaseQueryHandler> logger)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<MobileDisputeCaseDto?> Handle(
        GetMobileDisputeCaseQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var ownerUserId = _holder.UserId ?? 0;

        // Primary owner gate on the SR (clean not-found on a foreign/unknown SR).
        await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, ownerUserId, cancellationToken);

        var resp = await _sr.GetOwnerDisputeCase(request.DisputeId);
        var caseResp = resp?.Body;

        // Defense-in-depth: the dispute must belong to the SR the caller was gated on. Any mismatch → not-found.
        if (caseResp?.ServiceRequest is null
            || caseResp.ServiceRequest.Id != request.ServiceRequestId
            || caseResp.ServiceRequest.OwnerUserId != ownerUserId)
            throw new AizenBusinessException("Dispute not found.");

        var dto = await MobileServiceRequestMapper.MapDisputeCaseAsync(
            caseResp, _fileStorage, _logger, cancellationToken);
        dto.OpenedByMe = caseResp.Dispute.OpenedByUserId == ownerUserId;
        return dto;
    }
}
