using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Query;

[DocumentationInfo("Get admin vessel media BFF query handler", "Fetches vessel media with optional type filter and maps to UI-ready BFF DTOs.")]
public sealed class GetVesselMediaBffQueryHandler
    : AizenQueryHandler<GetVesselMediaBffQuery, AdminVesselMediaBffResponse>
{
    private readonly IVesselRemoteCall _vessel;

    public GetVesselMediaBffQueryHandler(
        IVesselRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<AdminVesselMediaBffResponse?> Handle(
        GetVesselMediaBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminVesselMediaBffResponse();

        try
        {

            // R5 — ask the Vessel module to presign each media file (it builds the read URL in-module via FileStorage).
            var result = await _vessel.GetVesselMedia(
                request.VesselId,
                request.PageIndex, request.PageSize,
                includeAccessUrls: true);

            var items = result?.Body?.Media?.Items;
            if (items != null)
            {
                var mapped = items.Select(m => new VesselMediaBffDto
                {
                    Id = m.Id,
                    MediaType = m.MediaType.ToString(),
                    Title = m.Title,
                    Description = m.Description,
                    OriginalFileName = m.OriginalFileNameSnapshot,
                    ContentType = m.ContentTypeSnapshot,
                    FileSizeBytes = m.SizeInBytesSnapshot,
                    // AccessUrl is the presigned GET; there is no separate thumbnail, so reuse it for both.
                    Url = m.AccessUrl,
                    ThumbnailUrl = m.AccessUrl ?? m.ThumbnailUrl,
                    UploadedAt = null, // not available from current entity
                    UploadedByUserId = m.UploadedByUserId,
                    IsPrimary = m.IsCover,
                    SortOrder = m.SortOrder,
                    IsActive = m.IsActive
                });

                response.Media = string.IsNullOrWhiteSpace(request.MediaType)
                    ? mapped.ToList()
                    : mapped.Where(m => string.Equals(m.MediaType, request.MediaType, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }
        catch (Exception)
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel.Media"));
        }

        return response;
    }
}
