using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// Delete a vessel document (M4e). Owner-gated against the caller's default-page owned set (clean not-found for a
/// foreign/unknown vessel id; the module returns not-found for a foreign doc id). The module invalidates the
/// documents default page, so the mobile list drops the doc immediately.
/// </summary>
public sealed class DeleteMobileVesselDocumentCommandHandler
    : AizenCommandHandler<DeleteMobileVesselDocumentCommand, MobileVesselDocumentDeletedDto>
{
    private const int OwnedPageIndex = 0;
    private const int OwnedPageSize = 20;

    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;

    public DeleteMobileVesselDocumentCommandHandler(IParticipantProfileResolver resolver, IVesselRemoteCall vessel)
    {
        _resolver = resolver;
        _vessel = vessel;
    }

    public override async Task<MobileVesselDocumentDeletedDto?> Handle(
        DeleteMobileVesselDocumentCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var ownedResp = await _vessel.GetUserVessels(OwnedPageIndex, OwnedPageSize);
        var owns = ownedResp?.Body?.Vessels?.Items?.Any(v => v.Id == request.VesselId) ?? false;
        if (!owns)
            throw new AizenBusinessException("Vessel not found.");

        // Confirm the document is one of this vessel's active documents before deleting — an unknown/foreign doc
        // id yields a clean not-found instead of the module's raw 500 (its remove throws for a missing id).
        var docsResp = await _vessel.GetVesselDocuments(request.VesselId, 0, OwnedPageSize, includeAccessUrls: false);
        var docExists = docsResp?.Body?.Documents?.Items?.Any(d => d.Id == request.DocumentId && d.IsActive) ?? false;
        if (!docExists)
            throw new AizenBusinessException("Document not found.");

        var resp = await _vessel.RemoveVesselDocument(request.VesselId, request.DocumentId);
        if (resp?.Body is null)
            throw new AizenBusinessException("Document not found.");

        return new MobileVesselDocumentDeletedDto { VesselId = resp.Body.VesselId, DocumentId = resp.Body.DocumentId };
    }
}
