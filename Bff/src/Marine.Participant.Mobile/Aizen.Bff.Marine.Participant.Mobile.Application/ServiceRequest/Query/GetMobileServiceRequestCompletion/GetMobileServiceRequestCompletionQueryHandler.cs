using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// The owner reads the provider's completion for one of their own SRs (BE_MO4). Reuses the owner-gated SR detail
/// (EnsureOwnedAsync — the module detail carries the Completion, including the N3 AutoApproveAt), so a foreign/unknown
/// SR yields a clean not-found. The single evidence file's presigned read URL is resolved best-effort (a failure
/// leaves it null → the FE shows a placeholder). Cost-free: only status / notes / evidence / review fields cross.
/// </summary>
public sealed class GetMobileServiceRequestCompletionQueryHandler
    : AizenQueryHandler<GetMobileServiceRequestCompletionQuery, MobileServiceRequestCompletionDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<GetMobileServiceRequestCompletionQueryHandler> _logger;

    public GetMobileServiceRequestCompletionQueryHandler(
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder holder,
        IServiceRequestRemoteCall sr,
        IFileStorageRemoteCall fileStorage,
        ILogger<GetMobileServiceRequestCompletionQueryHandler> logger)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<MobileServiceRequestCompletionDto?> Handle(
        GetMobileServiceRequestCompletionQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var detail = await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);

        // No completion submitted yet → null (the FE hides the review section). Not an error.
        if (detail.Completion is null)
            return null;

        return await MobileServiceRequestMapper.MapCompletionWithEvidenceUrlAsync(
            detail.Completion, _fileStorage, _logger, cancellationToken);
    }
}
