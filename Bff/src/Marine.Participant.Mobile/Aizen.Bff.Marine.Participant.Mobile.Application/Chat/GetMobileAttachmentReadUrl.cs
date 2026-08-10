using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Chat;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Microsoft.Extensions.Logging;
using Refit;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Chat;

/// <summary>GET /api/v1/mobile/service-requests/{id}/attachments/{fileId}/read-url — a short-lived signed read-url for
/// a chat image on the owner's own SR (BE_MO10b). Mirrors the provider read-url: the SR module owner-gates the fileId
/// (owns the SR + fileId is on the SR), then the BFF mints the presigned GET via FileStorage. A failed access-check is
/// a clean not-found; a resolved-but-missing object degrades to an empty url (FE placeholder — not a security event,
/// the access-check already proved ownership).</summary>
public sealed class GetMobileAttachmentReadUrlQuery : AizenQuery<MobileAttachmentReadUrlDto>
{
    public GetMobileAttachmentReadUrlQuery(long serviceRequestId, Guid fileId)
    {
        ServiceRequestId = serviceRequestId;
        FileId = fileId;
    }

    public long ServiceRequestId { get; }
    public Guid FileId { get; }
}

public sealed class GetMobileAttachmentReadUrlQueryHandler
    : AizenQueryHandler<GetMobileAttachmentReadUrlQuery, MobileAttachmentReadUrlDto>
{
    private static readonly TimeSpan ReadUrlTtl = TimeSpan.FromMinutes(5);

    private readonly IParticipantProfileResolver _resolver;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<GetMobileAttachmentReadUrlQueryHandler> _logger;

    public GetMobileAttachmentReadUrlQueryHandler(
        IParticipantProfileResolver resolver,
        IServiceRequestRemoteCall sr,
        IFileStorageRemoteCall fileStorage,
        ILogger<GetMobileAttachmentReadUrlQueryHandler> logger)
    {
        _resolver = resolver;
        _sr = sr;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<MobileAttachmentReadUrlDto?> Handle(
        GetMobileAttachmentReadUrlQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // Owner gate + fileId-on-SR, module-side. A vague not-found on any mismatch (no info leak).
        try
        {
            var check = await _sr.CheckAttachmentAccess(request.ServiceRequestId, request.FileId);
            if (check?.Body is not { Authorized: true })
                throw new AizenBusinessException("Attachment not found.");
        }
        catch (ApiException)
        {
            throw new AizenBusinessException("Attachment not found.");
        }

        // Authorized → mint the presigned GET. A resolved-but-missing object → empty url (FE placeholder).
        try
        {
            var url = await _fileStorage.CreateReadUrl(request.FileId, new CreateReadUrlRequest { ExpiresIn = ReadUrlTtl });
            if (url?.Body is null)
                return new MobileAttachmentReadUrlDto { Url = string.Empty };

            return new MobileAttachmentReadUrlDto { Url = url.Body.ReadUrl, ExpiresAt = url.Body.ExpiresAt };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not mint a read-url for chat attachment {FileId} on SR {SrId}.",
                request.FileId, request.ServiceRequestId);
            return new MobileAttachmentReadUrlDto { Url = string.Empty };
        }
    }
}
