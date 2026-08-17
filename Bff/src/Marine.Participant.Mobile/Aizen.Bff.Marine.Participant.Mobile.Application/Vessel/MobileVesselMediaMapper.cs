using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.Vessel.Abstraction.Dto.Media;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// Projects module VesselMediaDto → the mobile media contract and resolves each item's presigned read URL itself
/// (M4f) — same approach as documents: the BFF reads the media list with includeAccessUrls=false (the page the
/// module's InvalidateMediaAsync evicts on add/remove/set-cover) and mints the read URL per-item here.
/// </summary>
internal static class MobileVesselMediaMapper
{
    private static readonly TimeSpan ReadUrlTtl = TimeSpan.FromMinutes(15);

    public static MobileVesselMediaDto Map(VesselMediaDto m) => new()
    {
        Id = m.Id,
        MediaType = m.MediaType.ToString(),
        SortOrder = m.SortOrder,
        IsCover = m.IsCover,
        Title = m.Title,
    };

    public static async Task<MobileVesselMediaDto> MapWithUrlAsync(
        VesselMediaDto m, IFileStorageRemoteCall fileStorage, ILogger logger, CancellationToken ct)
    {
        var dto = Map(m);
        if (m.FileId is { } fileId && fileId != Guid.Empty)
        {
            try
            {
                var url = await fileStorage.CreateReadUrl(fileId, new CreateReadUrlRequest { ExpiresIn = ReadUrlTtl });
                if (url?.Body is not null)
                {
                    dto.Url = url.Body.ReadUrl;
                    dto.UrlExpiresAt = url.Body.ExpiresAt;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not resolve read URL for vessel media {MediaId} (file {FileId}).", m.Id, fileId);
            }
        }
        return dto;
    }
}
