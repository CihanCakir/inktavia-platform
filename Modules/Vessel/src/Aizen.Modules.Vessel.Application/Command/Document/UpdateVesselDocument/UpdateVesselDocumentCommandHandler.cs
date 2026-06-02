using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Document;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Document;

[DocumentationInfo("Update Vessel Document Command Handler", "Loads document, applies metadata update and invalidates documents cache.")]
public sealed class UpdateVesselDocumentCommandHandler : AizenCommandHandler<UpdateVesselDocumentCommand, VesselDocumentDto>
{
    private readonly IVesselDocumentRepository _documentRepository;
    private readonly IVesselCacheInvalidationService _invalidation;

    public UpdateVesselDocumentCommandHandler(IVesselDocumentRepository documentRepository, IVesselCacheInvalidationService invalidation)
    {
        _documentRepository = documentRepository;
        _invalidation = invalidation;
    }

    public override async Task<VesselDocumentDto?> Handle(UpdateVesselDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document {request.DocumentId} not found.");

        document.Update(request.Request.DocumentName, request.Request.ExpiresAt, request.Request.Notes);
        _documentRepository.Update(document);

        await _invalidation.InvalidateDocumentsAsync(request.VesselId, cancellationToken);

        return document.ToDto();
    }
}
