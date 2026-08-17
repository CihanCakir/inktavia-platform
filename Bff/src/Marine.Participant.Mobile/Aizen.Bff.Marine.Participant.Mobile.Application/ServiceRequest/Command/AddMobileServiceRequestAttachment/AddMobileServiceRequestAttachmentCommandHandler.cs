using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// Attach a completed client-side upload (fileId) to one of the caller's own service requests. The bytes were PUT
/// straight to storage via /mobile/uploads — the BFF only gates ownership and attaches the fileId. Returns the
/// created attachment with a freshly-resolved presigned read URL.
/// </summary>
public sealed class AddMobileServiceRequestAttachmentCommandHandler
    : AizenCommandHandler<AddMobileServiceRequestAttachmentCommand, MobileServiceRequestAttachmentDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<AddMobileServiceRequestAttachmentCommandHandler> _logger;

    public AddMobileServiceRequestAttachmentCommandHandler(
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder holder,
        IServiceRequestRemoteCall sr,
        IFileStorageRemoteCall fileStorage,
        ILogger<AddMobileServiceRequestAttachmentCommandHandler> logger)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<MobileServiceRequestAttachmentDto?> Handle(
        AddMobileServiceRequestAttachmentCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request ?? throw new AizenBusinessException("An attachment payload is required.");
        if (r.FileId == Guid.Empty)
            throw new AizenBusinessException("A fileId is required.");

        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);

        var addResp = await _sr.AddAttachment(request.ServiceRequestId, new AddServiceRequestAttachmentRequest
        {
            FileId = r.FileId,
            AttachmentType = MobileServiceRequestMapper.ParseAttachmentType(r.AttachmentType),
            Title = string.IsNullOrWhiteSpace(r.Title) ? null : r.Title!.Trim(),
            Description = string.IsNullOrWhiteSpace(r.Description) ? null : r.Description!.Trim(),
        });

        var created = addResp?.Body?.Attachment
            ?? throw new AizenBusinessException("The file could not be attached to the service request.");

        return await MobileServiceRequestMapper.MapAttachmentWithUrlAsync(created, _fileStorage, _logger, cancellationToken);
    }
}
