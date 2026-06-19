using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel list BFF query", "Query for the Vessel Management List with UI-specific filters and enriched response.")]
public sealed class GetAdminVesselListBffQuery : AizenQuery<AdminVesselListBffResponse>
{
    public string UserToken { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public string? SearchTerm { get; }
    public bool? IsArchived { get; }
    public int[]? AssetTypes { get; }
    public int[]? OwnershipStatuses { get; }
    public int[]? OperationalStatuses { get; }

    public GetAdminVesselListBffQuery(
        string userToken, int pageIndex, int pageSize,
        string? searchTerm, bool? isArchived,
        int[]? assetTypes, int[]? ownershipStatuses, int[]? operationalStatuses)
    {
        UserToken = userToken;
        PageIndex = pageIndex;
        PageSize = pageSize;
        SearchTerm = searchTerm;
        IsArchived = isArchived;
        AssetTypes = assetTypes;
        OwnershipStatuses = ownershipStatuses;
        OperationalStatuses = operationalStatuses;
    }
}
