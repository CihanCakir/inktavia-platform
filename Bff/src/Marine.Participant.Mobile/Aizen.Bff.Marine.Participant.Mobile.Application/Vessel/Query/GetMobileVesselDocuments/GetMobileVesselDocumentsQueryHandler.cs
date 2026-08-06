using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// List a vessel's documents (M4e). Owner-gated against the caller's default-page owned set (clean not-found for
/// a foreign/unknown id). Reads the module's documents DEFAULT page with includeAccessUrls=false — the exact key
/// InvalidateDocumentsAsync evicts on add/remove, so a just-uploaded/deleted doc shows immediately — then resolves
/// each doc's presigned read URL per-doc (avoiding the never-invalidated includeAccessUrls=true cache variant).
/// </summary>
public sealed class GetMobileVesselDocumentsQueryHandler
    : AizenQueryHandler<GetMobileVesselDocumentsQuery, List<MobileVesselDocumentDto>>
{
    private const int OwnedPageIndex = 0;
    private const int OwnedPageSize = 20;

    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<GetMobileVesselDocumentsQueryHandler> _logger;

    public GetMobileVesselDocumentsQueryHandler(
        IParticipantProfileResolver resolver,
        IVesselRemoteCall vessel,
        IFileStorageRemoteCall fileStorage,
        ILogger<GetMobileVesselDocumentsQueryHandler> logger)
    {
        _resolver = resolver;
        _vessel = vessel;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<List<MobileVesselDocumentDto>?> Handle(
        GetMobileVesselDocumentsQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        await EnsureOwnedAsync(request.VesselId);

        // Read the invalidated default page (includeAccessUrls=false) — the BFF resolves URLs itself.
        var resp = await _vessel.GetVesselDocuments(request.VesselId, 0, OwnedPageSize, includeAccessUrls: false);
        // Active only: the module's "remove" deactivates (IsActive=false) rather than hard-deleting, and its list
        // read returns inactive rows — so a deleted document must drop out here (mirrors the M4d archived filter).
        var items = (resp?.Body?.Documents?.Items
            ?? Enumerable.Empty<Aizen.Modules.Vessel.Abstraction.Dto.Document.VesselDocumentDto>())
            .Where(d => d.IsActive).ToList();

        var result = new List<MobileVesselDocumentDto>(items.Count);
        foreach (var d in items)
            result.Add(await MobileVesselDocumentMapper.MapWithUrlAsync(d, _fileStorage, _logger, cancellationToken));
        return result;
    }

    private async Task EnsureOwnedAsync(long vesselId)
    {
        var ownedResp = await _vessel.GetUserVessels(OwnedPageIndex, OwnedPageSize);
        var owns = ownedResp?.Body?.Vessels?.Items?.Any(v => v.Id == vesselId) ?? false;
        if (!owns)
            throw new AizenBusinessException("Vessel not found.");
    }
}
