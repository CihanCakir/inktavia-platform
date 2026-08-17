using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

public sealed class GetAdminVesselOverviewQuery : AizenQuery<AdminVesselOverviewResponse>
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public string? SearchTerm { get; }
    public bool? IsArchived { get; }
    public GetAdminVesselOverviewQuery(int pageIndex, int pageSize, string? searchTerm, bool? isArchived)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        SearchTerm = searchTerm;
        IsArchived = isArchived;
    }
}
