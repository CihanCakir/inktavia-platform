using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Modules.Vessel.Application.Command.Document;

[DocumentationInfo("Update Vessel Document Status Command Handler", "Changes document status and invalidates documents cache.")]
public sealed class UpdateVesselDocumentStatusCommandHandler : AizenCommandHandler<UpdateVesselDocumentStatusCommand, UpdateVesselDocumentStatusResponse>
{
    private readonly IVesselDocumentRepository _documentRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public UpdateVesselDocumentStatusCommandHandler(
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

    public override async Task<UpdateVesselDocumentStatusResponse?> Handle(UpdateVesselDocumentStatusCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document {request.DocumentId} not found.");

        document.ChangeStatus(request.Status);
        _documentRepository.Update(document);

        await _invalidation.InvalidateDocumentsAsync(request.VesselId, cancellationToken);

        return new UpdateVesselDocumentStatusResponse(request.VesselId, request.DocumentId, request.Status);
    }
}
