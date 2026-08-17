using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Modules.Vessel.Application.Command.Document;

[DocumentationInfo("Approve Vessel Document Command Handler",
    "Sets ApprovedAt/ApprovedByUserId on a document via the domain Approve() method (idempotent — an already-approved document is returned unchanged) and invalidates the documents + vessel-detail caches. The endpoint is admin-gated at the controller, so no per-owner access check is applied here.")]
public sealed class ApproveVesselDocumentCommandHandler : AizenCommandHandler<ApproveVesselDocumentCommand, ApproveVesselDocumentResponse>
{
    private readonly IVesselDocumentRepository _documentRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IAizenInfoAccessor _info;

    public ApproveVesselDocumentCommandHandler(
        IVesselDocumentRepository documentRepository,
        IVesselCacheInvalidationService invalidation,
        IAizenInfoAccessor info)
    {
        _documentRepository = documentRepository;
        _invalidation = invalidation;
        _info = info;
    }

    public override async Task<ApproveVesselDocumentResponse?> Handle(ApproveVesselDocumentCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;

        var document = await _documentRepository.GetByIdWithVesselAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document {request.DocumentId} not found.");

        // Idempotent: an already-approved document is returned unchanged (no re-write, no cache churn).
        if (document.ApprovedAt.HasValue)
            return new ApproveVesselDocumentResponse(document.ToDto());

        document.Approve(currentUserId);
        _documentRepository.Update(document);

        await _invalidation.InvalidateDocumentsAsync(document.VesselId, cancellationToken);
        await _invalidation.InvalidateVesselAsync(document.VesselId, cancellationToken);

        return new ApproveVesselDocumentResponse(document.ToDto());
    }
}
