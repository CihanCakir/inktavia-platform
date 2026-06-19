using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel list BFF query handler", "Fetches paged vessel list with UI-specific filters and maps to flat BFF DTOs.")]
public sealed class GetAdminVesselListBffQueryHandler
    : AizenQueryHandler<GetAdminVesselListBffQuery, AdminVesselListBffResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public GetAdminVesselListBffQueryHandler(
        IVesselAdminBffRemoteCall vessel,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _vessel = vessel;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<AdminVesselListBffResponse?> Handle(
        GetAdminVesselListBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminVesselListBffResponse();

        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
            var authHeader = $"Bearer {serviceToken}";

            var result = await _vessel.GetAdminVesselList(
                authHeader,
                request.UserToken,
                request.PageIndex,
                request.PageSize,
                request.SearchTerm,
                request.IsArchived,
                request.AssetTypes,
                request.OwnershipStatuses,
                request.OperationalStatuses);

            var page = result?.Body?.Vessels;
            if (page != null)
            {
                response.Vessels = new VesselPageBffDto
                {
                    From = page.From,
                    Index = page.Index,
                    Size = page.Size,
                    Count = page.Count,
                    Pages = page.Pages,
                    HasPrevious = page.HasPrevious,
                    HasNext = page.HasNext,
                    Items = page.Items?.Select(v => new VesselListItemBffDto
                    {
                        Id = v.Id,
                        VesselCode = v.VesselCode,
                        Name = v.Name,
                        Slug = v.Slug,
                        VesselTypeCode = v.VesselTypeCode,
                        FlagCountryCode = v.FlagCountryCode,
                        ThumbnailUrl = v.CoverMediaUrl,
                        OwnerName = v.OwnerName,
                        LengthMeters = v.LengthMeters,
                        GrossTonnage = v.GrossTonnage,
                        Latitude = v.Latitude,
                        Longitude = v.Longitude,
                        LastPositionDate = v.LastPositionDate,
                        OperationalStatus = v.OperationalStatus,
                        AssetType = v.AssetType,
                        OwnershipStatus = v.OwnershipStatus,
                        Status = (int)v.Status,
                        IsArchived = v.IsArchived,
                        CreateDate = v.CreateDate
                    }).ToList() ?? new()
                };
            }
        }
        catch (Exception)
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel"));
        }

        return response;
    }
}
