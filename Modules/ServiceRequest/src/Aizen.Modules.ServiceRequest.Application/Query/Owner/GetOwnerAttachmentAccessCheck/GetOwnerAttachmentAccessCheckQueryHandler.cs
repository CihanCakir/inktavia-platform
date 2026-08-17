using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Query.Owner.GetOwnerAttachmentAccessCheck;

/// <summary>
/// BE-MO10a/b — owner-scoped attachment access-check. The caller must OWN the SR (<c>sr.OwnerUserId == the asserted
/// UserInfo.UserId</c>) AND the fileId must actually belong to the SR (request attachment | message attachment |
/// work-log evidence | completion evidence) — the same fileId verification the provider handler uses. Because the
/// owner writes chat images through the SR module (MO10a, <c>SendServiceRequestMessage</c>, SenderType=Owner), the
/// fileId lands in <c>sr.Messages</c>, so this check finds it (task #81). Every failure is a vague not-found (no info
/// leak). The provider handler is untouched.
/// </summary>
public sealed class GetOwnerAttachmentAccessCheckQueryHandler
    : AizenQueryHandler<GetOwnerAttachmentAccessCheckQuery, GetAttachmentAccessCheckResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ILogger<GetOwnerAttachmentAccessCheckQueryHandler> _logger;

    public GetOwnerAttachmentAccessCheckQueryHandler(
        IServiceRequestRepository srRepository,
        IAizenInfoAccessor info,
        ILogger<GetOwnerAttachmentAccessCheckQueryHandler> logger)
    {
        _srRepository = srRepository;
        _info = info;
        _logger = logger;
    }

    public override async Task<GetAttachmentAccessCheckResponse?> Handle(
        GetOwnerAttachmentAccessCheckQuery request, CancellationToken ct)
    {
        var ownerUserId = _info.UserInfoAccessor?.UserInfo?.UserId ?? 0;

        var sr = await _srRepository.GetByIdWithDetailsAsync(request.ServiceRequestId, ct);

        return Evaluate(sr, ownerUserId, request.FileId, _logger);
    }

    /// <summary>
    /// Pure owner-gate + fileId-belongs-to-SR check (no I/O) — testable in isolation. Throws a vague not-found on an
    /// unresolved owner, a foreign SR, or a fileId not on the SR; else returns access-OK.
    /// </summary>
    public static GetAttachmentAccessCheckResponse Evaluate(
        ServiceRequestEntity? sr, long ownerUserId, Guid fileId, ILogger? logger = null)
    {
        if (sr is null || ownerUserId <= 0 || sr.OwnerUserId != ownerUserId)
        {
            logger?.LogWarning(
                "Owner {OwnerUserId} tried to access an attachment on SR {ServiceRequestId} they do not own.",
                ownerUserId, sr?.Id);
            throw new AizenBusinessException("Service request not found.");
        }

        // fileId must actually belong to this SR — IDENTICAL to the provider check.
        var isRequestAttachment  = sr.Attachments.Any(a => a.FileId == fileId && !a.IsDeleted);
        var isMessageAttachment  = sr.Messages.Any(m => m.AttachmentFileId == fileId && !m.IsDeleted);
        var isWorkLogEvidence    = sr.Assignment?.WorkLogs.Any(w => w.AttachmentFileId == fileId) ?? false;
        var isCompletionEvidence = sr.Completion?.EvidenceFileId == fileId;

        if (!isRequestAttachment && !isMessageAttachment && !isWorkLogEvidence && !isCompletionEvidence)
        {
            logger?.LogWarning(
                "Owner {OwnerUserId} requested read-url for fileId {FileId} which is not an attachment/message/evidence on SR {ServiceRequestId}.",
                ownerUserId, fileId, sr.Id);
            throw new AizenBusinessException("Service request not found.");
        }

        return new GetAttachmentAccessCheckResponse(fileId, true);
    }
}
