using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// Full detail for one of the caller's own service requests. Resolves the participant, then fetches the module
/// detail and gates it against the resolved owner id (the module does not owner-check detail), so a foreign/unknown
/// id yields a clean not-found. Attachment presigned read URLs are resolved per-file.
/// </summary>
public sealed class GetMobileServiceRequestDetailQueryHandler
    : AizenQueryHandler<GetMobileServiceRequestDetailQuery, MobileServiceRequestDetailDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<GetMobileServiceRequestDetailQueryHandler> _logger;

    public GetMobileServiceRequestDetailQueryHandler(
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder holder,
        IServiceRequestRemoteCall sr,
        IFileStorageRemoteCall fileStorage,
        ILogger<GetMobileServiceRequestDetailQueryHandler> logger)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<MobileServiceRequestDetailDto?> Handle(
        GetMobileServiceRequestDetailQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var detail = await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);

        return await MobileServiceRequestMapper.MapDetailWithAttachmentUrlsAsync(
            detail, _fileStorage, _logger, cancellationToken);
    }
}
