using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// The owner approves the provider's completion (BE_MO4). Reuses the module approve command verbatim — the module
/// transitions the SR to Completed, publishes CompletionApproved, and the escrow release to the provider runs on the
/// existing decoupled path (ServiceRequestCompletedConsumer / PaymentAutoReleaseEligibilityJob) — NONE of which this
/// BFF touches. The module approve does NOT owner-check, so the BFF gates ownership (EnsureOwnedAsync) before
/// proxying, then re-reads the completion so the client shows the ApprovedByOwner state. Cost-free.
/// </summary>
public sealed class ApproveMobileServiceRequestCompletionCommandHandler
    : AizenCommandHandler<ApproveMobileServiceRequestCompletionCommand, MobileServiceRequestCompletionDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<ApproveMobileServiceRequestCompletionCommandHandler> _logger;

    public ApproveMobileServiceRequestCompletionCommandHandler(
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder holder,
        IServiceRequestRemoteCall sr,
        IFileStorageRemoteCall fileStorage,
        ILogger<ApproveMobileServiceRequestCompletionCommandHandler> logger)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<MobileServiceRequestCompletionDto?> Handle(
        ApproveMobileServiceRequestCompletionCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var rating = request.Request?.Rating;
        if (rating is int r && r is < 1 or > 5)
            throw new AizenBusinessException("Rating must be between 1 and 5.");

        // The module approve trusts the caller — gate ownership here (clean not-found on a foreign/unknown SR).
        await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);

        // Reuse the module approve exactly — SR → Completed, CompletionApproved, decoupled escrow release. Untouched.
        await _sr.ApproveOwnerCompletion(request.ServiceRequestId, new ApproveServiceRequestCompletionRequest
        {
            ReviewNotes = string.IsNullOrWhiteSpace(request.Request?.Note) ? null : request.Request!.Note!.Trim(),
            ClientRating = rating,
        });

        // Re-read so the client shows the ApprovedByOwner completion (+ any rating) and the released/paid state.
        var detail = await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);
        if (detail.Completion is null)
            throw new AizenBusinessException("Completion not found.");

        return await MobileServiceRequestMapper.MapCompletionWithEvidenceUrlAsync(
            detail.Completion, _fileStorage, _logger, cancellationToken);
    }
}
