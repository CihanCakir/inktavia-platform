using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Vessel.Abstraction.Message;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Modules.Vessel.Application.Command.Document;

[DocumentationInfo("Remove Vessel Document Command Handler", "Deactivates a vessel document, invalidates documents cache and publishes VesselDocumentRemovedMessage.")]
public sealed class RemoveVesselDocumentCommandHandler : AizenCommandHandler<RemoveVesselDocumentCommand, RemoveVesselDocumentResponse>
{
    private readonly IVesselDocumentRepository _documentRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenMessagePublisher _publisher;
    private readonly IAizenInfoAccessor _info;

    public RemoveVesselDocumentCommandHandler(
        IVesselDocumentRepository documentRepository,
        IVesselCacheInvalidationService invalidation,
        IVesselAccessService accessService,
        IAizenMessagePublisher publisher,
        IAizenInfoAccessor info)
    {
        _documentRepository = documentRepository;
        _invalidation = invalidation;
        _accessService = accessService;
        _publisher = publisher;
        _info = info;
    }

    public override async Task<RemoveVesselDocumentResponse?> Handle(RemoveVesselDocumentCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var isAdmin = _info.UserInfoAccessor.UserInfo.Roles.Contains("Admin");

        var document = await _documentRepository.GetByIdWithVesselAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document {request.DocumentId} not found.");

        if (!isAdmin)
            await _accessService.EnsureCanEditAsync(document.VesselId, currentUserId, cancellationToken);

        document.Deactivate();
        _documentRepository.Update(document);

        await _invalidation.InvalidateDocumentsAsync(document.VesselId, cancellationToken);

        await _publisher.PublishAsync(new VesselDocumentRemovedMessage
        {
            VesselId = document.VesselId,
            DocumentId = document.Id,
            ActorUserId = currentUserId
        }, cancellationToken);

        return new RemoveVesselDocumentResponse(document.VesselId, document.Id);
    }
}
