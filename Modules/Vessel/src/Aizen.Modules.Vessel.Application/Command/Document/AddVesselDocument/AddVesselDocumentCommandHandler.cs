using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Modules.Vessel.Application.Command.Document;

[DocumentationInfo("Add Vessel Document Command Handler", "Creates a vessel document entity and invalidates documents and vessel detail caches.")]
public sealed class AddVesselDocumentCommandHandler : AizenCommandHandler<AddVesselDocumentCommand, AddVesselDocumentResponse>
{
    private readonly IVesselDocumentRepository _documentRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public AddVesselDocumentCommandHandler(
        IVesselDocumentRepository documentRepository,
        IVesselCacheInvalidationService invalidation,
        IVesselAccessService accessService,
        IAizenInfoAccessor info)
    {
        _documentRepository = documentRepository;
        _invalidation = invalidation;
        _accessService = accessService;
        _info = info;
    }

    public override async Task<AddVesselDocumentResponse?> Handle(AddVesselDocumentCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        var r = request.Request;
        var document = VesselDocumentEntity.Create(
            request.VesselId,
            r.DocumentTypeCode,
            r.DocumentName,
            r.FileId,
            r.FileName,
            r.FileUrl,
            r.MimeType,
            r.ExpiresAt,
            r.Notes);

        await _documentRepository.AddAsync(document, cancellationToken);

        await _invalidation.InvalidateDocumentsAsync(request.VesselId, cancellationToken);
        await _invalidation.InvalidateVesselAsync(request.VesselId, cancellationToken);

        return new AddVesselDocumentResponse(document.ToDto());
    }
}
