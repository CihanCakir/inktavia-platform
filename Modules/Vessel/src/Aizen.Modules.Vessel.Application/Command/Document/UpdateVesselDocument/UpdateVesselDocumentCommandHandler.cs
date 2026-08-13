using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Vessel.Abstraction.Message;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Document;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Vessel.Application.Command.Document;

[DocumentationInfo("Update Vessel Document Command Handler", "Loads document, validates the file if changed, applies metadata update, links the new file, invalidates documents cache and publishes VesselDocumentUpdatedMessage.")]
public sealed class UpdateVesselDocumentCommandHandler : AizenCommandHandler<UpdateVesselDocumentCommand, UpdateVesselDocumentResponse>
{
    private static readonly IReadOnlyList<string> AllowedContentTypes =
        ["application/pdf", "image/jpeg", "image/png", "image/webp", "text/plain"];

    private readonly IVesselDocumentRepository _documentRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IVesselFileStorageService _fileStorage;
    private readonly IAizenMessagePublisher _publisher;
    private readonly IAizenInfoAccessor _info;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UpdateVesselDocumentCommandHandler> _logger;

    public UpdateVesselDocumentCommandHandler(
        IVesselDocumentRepository documentRepository,
        IVesselCacheInvalidationService invalidation,
        IVesselAccessService accessService,
        IVesselFileStorageService fileStorage,
        IAizenMessagePublisher publisher,
        IAizenInfoAccessor info,
        IHttpContextAccessor httpContextAccessor,
        ILogger<UpdateVesselDocumentCommandHandler> logger)
    {
        _documentRepository = documentRepository;
        _invalidation = invalidation;
        _accessService = accessService;
        _fileStorage = fileStorage;
        _publisher = publisher;
        _info = info;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public override async Task<UpdateVesselDocumentResponse?> Handle(UpdateVesselDocumentCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        // Roles is empty on a BFF assertion S2S call; fall back to the framework principal (service token carries Admin).
        var isAdmin = _info.UserInfoAccessor.UserInfo.Roles?.Contains("Admin") == true
            || _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;
        var accessToken = _info.UserInfoAccessor.UserInfo.AccessToken;

        var document = await _documentRepository.GetByIdWithVesselAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document {request.DocumentId} not found.");

        if (!isAdmin)
            await _accessService.EnsureCanEditAsync(document.VesselId, currentUserId, cancellationToken);

        var r = request.Request;
        string? originalFileNameSnapshot = document.OriginalFileNameSnapshot;
        string? contentTypeSnapshot = document.ContentTypeSnapshot;
        long? sizeInBytesSnapshot = document.SizeInBytesSnapshot;
        bool fileChanged = r.FileId.HasValue && r.FileId != document.FileId;

        if (fileChanged)
        {
            await _fileStorage.EnsureFileCanBeAttachedToVesselAsync(r.FileId!.Value, AllowedContentTypes, accessToken, cancellationToken);
            var meta = await _fileStorage.GetRequiredFileMetadataAsync(r.FileId.Value, accessToken, cancellationToken);
            originalFileNameSnapshot = meta.OriginalFileName;
            contentTypeSnapshot = meta.ContentType;
            sizeInBytesSnapshot = meta.SizeInBytes;
        }

        document.Update(
            r.DocumentName,
            r.DocumentTypeCode,
            r.FileId,
            originalFileNameSnapshot,
            contentTypeSnapshot,
            sizeInBytesSnapshot,
            r.ExpiresAt,
            r.Notes);
        _documentRepository.Update(document);

        if (fileChanged)
        {
            try
            {
                await _fileStorage.LinkFileToVesselOwnerAsync(r.FileId!.Value, document.Id, "VesselDocument", accessToken, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to link file {FileId} to document {DocumentId}. Continuing.", r.FileId, document.Id);
            }
        }

        await _invalidation.InvalidateDocumentsAsync(document.VesselId, cancellationToken);

        await _publisher.PublishAsync(new VesselDocumentUpdatedMessage
        {
            VesselId = document.VesselId,
            DocumentId = document.Id,
            ActorUserId = currentUserId
        }, cancellationToken);

        return new UpdateVesselDocumentResponse(document.ToDto());
    }
}
