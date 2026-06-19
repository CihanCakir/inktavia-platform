using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel media BFF query handler", "Fetches vessel media with optional type filter and maps to UI-ready BFF DTOs.")]
public sealed class GetAdminVesselMediaBffQueryHandler
    : AizenQueryHandler<GetAdminVesselMediaBffQuery, AdminVesselMediaBffResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetAdminVesselMediaBffQueryHandler(
        IVesselAdminBffRemoteCall vessel,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _vessel = vessel;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminVesselMediaBffResponse?> Handle(
        GetAdminVesselMediaBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminVesselMediaBffResponse();

        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
            var authHeader = $"Bearer {serviceToken}";

            var result = await _vessel.GetVesselMedia(
                request.VesselId, authHeader, request.UserToken,
                request.PageIndex, request.PageSize);

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
                    ThumbnailUrl = m.ThumbnailUrl,
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
