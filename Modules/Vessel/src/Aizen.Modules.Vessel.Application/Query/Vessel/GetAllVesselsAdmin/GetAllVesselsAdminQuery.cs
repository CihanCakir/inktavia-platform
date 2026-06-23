using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get All Vessels Admin Query", "Admin query to retrieve a paged list of all vessels.")]
public sealed class GetAllVesselsAdminQuery : AizenQuery<GetAllVesselsAdminResponse>
{
    public string? SearchTerm { get; }
    public bool? IsArchived { get; }
    public int[]? AssetTypes { get; }
    public int[]? OwnershipStatuses { get; }
    public int[]? OperationalStatuses { get; }
    public long? OwnerUserId { get; }

    public int PageIndex { get; }
    public int PageSize { get; }

    public GetAllVesselsAdminQuery(int pageIndex = 0, int pageSize = 20, string? searchTerm = null, bool? isArchived = null, int[]? assetTypes = null, int[]? ownershipStatuses = null, int[]? operationalStatuses = null, long? ownerUserId = null)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        SearchTerm = searchTerm;
        IsArchived = isArchived;
        AssetTypes = assetTypes;
        OwnershipStatuses = ownershipStatuses;
        OperationalStatuses = operationalStatuses;
        OwnerUserId = ownerUserId;
    }
}
