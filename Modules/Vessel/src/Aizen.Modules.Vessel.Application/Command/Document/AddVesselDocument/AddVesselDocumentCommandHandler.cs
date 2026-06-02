using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Document;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Document;

[DocumentationInfo("Add Vessel Document Command Handler", "Creates a vessel document entity and invalidates documents and vessel detail caches.")]
public sealed class AddVesselDocumentCommandHandler : AizenCommandHandler<AddVesselDocumentCommand, VesselDocumentDto>
{
    private readonly IVesselDocumentRepository _documentRepository;
    private readonly IVesselCacheInvalidationService _invalidation;

    public AddVesselDocumentCommandHandler(IVesselDocumentRepository documentRepository, IVesselCacheInvalidationService invalidation)
    {
        _documentRepository = documentRepository;
        _invalidation = invalidation;
    }

    public override async Task<VesselDocumentDto?> Handle(AddVesselDocumentCommand request, CancellationToken cancellationToken)
    {
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

        return document.ToDto();
    }
}
