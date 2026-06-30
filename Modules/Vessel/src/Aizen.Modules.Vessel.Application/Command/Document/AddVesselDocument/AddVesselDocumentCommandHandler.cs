using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Vessel.Abstraction.Message;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Document;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Vessel.Application.Command.Document;

[DocumentationInfo("Add Vessel Document Command Handler", "Validates the file via FileStorage, creates a vessel document entity, links the file to the vessel, invalidates documents cache and publishes VesselDocumentAddedMessage.")]
public sealed class AddVesselDocumentCommandHandler : AizenCommandHandler<AddVesselDocumentCommand, AddVesselDocumentResponse>
{
    private static readonly IReadOnlyList<string> AllowedContentTypes =
        ["application/pdf", "image/jpeg", "image/png", "image/webp", "text/plain"];

    private readonly IVesselDocumentRepository _documentRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IVesselFileStorageService _fileStorage;
    private readonly IAizenMessagePublisher _publisher;
    private readonly IAizenInfoAccessor _info;
    private readonly ILogger<AddVesselDocumentCommandHandler> _logger;

    public AddVesselDocumentCommandHandler(
        IVesselDocumentRepository documentRepository,
        IVesselCacheInvalidationService invalidation,
        IVesselAccessService accessService,
        IVesselFileStorageService fileStorage,
        IAizenMessagePublisher publisher,
        IAizenInfoAccessor info,
        ILogger<AddVesselDocumentCommandHandler> logger)
    {
        _documentRepository = documentRepository;
        _invalidation = invalidation;
        _accessService = accessService;
        _fileStorage = fileStorage;
        _publisher = publisher;
        _info = info;
        _logger = logger;
    }

    public override async Task<AddVesselDocumentResponse?> Handle(AddVesselDocumentCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var isAdmin = _info.UserInfoAccessor.UserInfo.Roles.Contains("Admin");
        var accessToken = _info.UserInfoAccessor.UserInfo.AccessToken;

        if (!isAdmin)
            await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        var r = request.Request;
        string? originalFileNameSnapshot = null;
        string? contentTypeSnapshot = null;
        long? sizeInBytesSnapshot = null;

        if (r.FileId.HasValue)
        {
            await _fileStorage.EnsureFileCanBeAttachedToVesselAsync(r.FileId.Value, AllowedContentTypes, accessToken, cancellationToken);
            var meta = await _fileStorage.GetRequiredFileMetadataAsync(r.FileId.Value, accessToken, cancellationToken);
            originalFileNameSnapshot = meta.OriginalFileName;
            contentTypeSnapshot = meta.ContentType;
            sizeInBytesSnapshot = meta.SizeInBytes;
        }

        var document = VesselDocumentEntity.Create(
            request.VesselId,
            r.DocumentTypeCode,
            r.DocumentName,
            r.FileId,
            originalFileNameSnapshot,
            contentTypeSnapshot,
            sizeInBytesSnapshot,
            r.ExpiresAt,
            r.Notes);

        await _documentRepository.AddAsync(document, cancellationToken);

        if (r.FileId.HasValue)
        {
            try
            {
                await _fileStorage.LinkFileToVesselOwnerAsync(r.FileId.Value, document.Id, "VesselDocument", accessToken, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to link file {FileId} to document {DocumentId}. Continuing.", r.FileId, document.Id);
            }
        }

        await _invalidation.InvalidateDocumentsAsync(request.VesselId, cancellationToken);
        await _invalidation.InvalidateVesselAsync(request.VesselId, cancellationToken);

        await _publisher.PublishAsync(new VesselDocumentAddedMessage
        {
            VesselId = request.VesselId,
            DocumentId = document.Id,
            DocumentTypeCode = document.DocumentTypeCode,
            FileId = document.FileId,
            ActorUserId = currentUserId
        }, cancellationToken);

        return new AddVesselDocumentResponse(document.ToDto());
    }
}
