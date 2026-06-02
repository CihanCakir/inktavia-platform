using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Document;

[DocumentationInfo("Remove Vessel Document Command Handler", "Deactivates a vessel document and invalidates documents cache.")]
public sealed class RemoveVesselDocumentCommandHandler : AizenCommandHandler<RemoveVesselDocumentCommand, bool>
{
    private readonly IVesselDocumentRepository _documentRepository;
    private readonly IVesselCacheInvalidationService _invalidation;

    public RemoveVesselDocumentCommandHandler(IVesselDocumentRepository documentRepository, IVesselCacheInvalidationService invalidation)
    {
        _documentRepository = documentRepository;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(RemoveVesselDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document {request.DocumentId} not found.");

        document.Deactivate();
        _documentRepository.Update(document);

        await _invalidation.InvalidateDocumentsAsync(request.VesselId, cancellationToken);

        return true;
    }
}
