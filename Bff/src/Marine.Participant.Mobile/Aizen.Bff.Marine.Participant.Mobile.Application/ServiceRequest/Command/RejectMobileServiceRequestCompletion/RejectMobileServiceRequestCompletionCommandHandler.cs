using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// The owner rejects the provider's completion with the N-E structured reason + optional note (BE_MO4). Reuses the
/// module reject command verbatim (SR → InProgress; no money moves). The module reject does NOT owner-check, so the
/// BFF gates ownership (EnsureOwnedAsync) before proxying, then re-reads the completion so the client shows the
/// RejectedByOwner state + the surfaced reason. Cost-free.
/// </summary>
public sealed class RejectMobileServiceRequestCompletionCommandHandler
    : AizenCommandHandler<RejectMobileServiceRequestCompletionCommand, MobileServiceRequestCompletionDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<RejectMobileServiceRequestCompletionCommandHandler> _logger;

    public RejectMobileServiceRequestCompletionCommandHandler(
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder holder,
        IServiceRequestRemoteCall sr,
        IFileStorageRemoteCall fileStorage,
        ILogger<RejectMobileServiceRequestCompletionCommandHandler> logger)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<MobileServiceRequestCompletionDto?> Handle(
        RejectMobileServiceRequestCompletionCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // The module reject trusts the caller — gate ownership here (clean not-found on a foreign/unknown SR).
        await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);

        await _sr.RejectOwnerCompletion(request.ServiceRequestId, new RejectServiceRequestCompletionRequest
        {
            ReasonCode = MobileServiceRequestMapper.ParseCompletionRejectReason(request.Request?.ReasonCode),
            ReviewNotes = string.IsNullOrWhiteSpace(request.Request?.Note) ? null : request.Request!.Note!.Trim(),
        });

        // Re-read so the client shows the RejectedByOwner completion + the surfaced reason.
        var detail = await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);
        if (detail.Completion is null)
            throw new AizenBusinessException("Completion not found.");

        return await MobileServiceRequestMapper.MapCompletionWithEvidenceUrlAsync(
            detail.Completion, _fileStorage, _logger, cancellationToken);
    }
}
