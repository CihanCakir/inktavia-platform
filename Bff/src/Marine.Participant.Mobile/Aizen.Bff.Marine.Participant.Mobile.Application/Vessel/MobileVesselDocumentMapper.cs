using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.Vessel.Abstraction.Dto.Document;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// Projects module VesselDocumentDto → the mobile document contract and resolves each doc's presigned read URL
/// itself (M4e). The BFF reads the documents list with includeAccessUrls=false — the exact page the module's
/// InvalidateDocumentsAsync evicts on add/remove, so the list stays fresh without a flush — and mints the read
/// URL per-doc here (FileStorage has no read cache, so this is always a fresh, valid presigned URL).
/// </summary>
internal static class MobileVesselDocumentMapper
{
    private static readonly TimeSpan ReadUrlTtl = TimeSpan.FromMinutes(15);

    public static MobileVesselDocumentDto MapDoc(VesselDocumentDto d) => new()
    {
        Id = d.Id,
        DocumentTypeCode = d.DocumentTypeCode,
        DocumentName = d.DocumentName,
        Status = d.Status.ToString(),
        OriginalFileName = d.OriginalFileNameSnapshot,
        ContentType = d.ContentTypeSnapshot,
        SizeInBytes = d.SizeInBytesSnapshot,
        ExpiresAt = d.ExpiresAt,
        Notes = d.Notes,
    };

    /// <summary>Map one doc and attach a freshly-resolved presigned read URL (best-effort — null on failure).</summary>
    public static async Task<MobileVesselDocumentDto> MapWithUrlAsync(
        VesselDocumentDto d, IFileStorageRemoteCall fileStorage, ILogger logger, CancellationToken ct)
    {
        var dto = MapDoc(d);
        if (d.FileId is { } fileId && fileId != Guid.Empty)
        {
            try
            {
                var url = await fileStorage.CreateReadUrl(fileId, new CreateReadUrlRequest { ExpiresIn = ReadUrlTtl });
                if (url?.Body is not null)
                {
                    dto.DownloadUrl = url.Body.ReadUrl;
                    dto.DownloadUrlExpiresAt = url.Body.ExpiresAt;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not resolve read URL for vessel document {DocumentId} (file {FileId}).", d.Id, fileId);
            }
        }
        return dto;
    }
}
