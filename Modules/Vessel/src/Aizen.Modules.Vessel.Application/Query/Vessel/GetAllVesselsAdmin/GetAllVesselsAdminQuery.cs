using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Model;
using MiniUow.Paging;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

[DocumentationInfo("Get All Vessels Admin Query", "Admin query to retrieve a paged list of all vessels.")]
public sealed class GetAllVesselsAdminQuery : AizenPagedQuery<VesselListItemDto>
{
    public string? SearchTerm { get; }
    public bool? IsArchived { get; }

    public int PageIndex { get; }
    public int PageSize { get; }

    public GetAllVesselsAdminQuery(int pageIndex = 0, int pageSize = 20, string? searchTerm = null, bool? isArchived = null)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        SearchTerm = searchTerm;
        IsArchived = isArchived;
    }
}
