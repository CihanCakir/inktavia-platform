using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Query;

public sealed class GetVesselOverviewBffQuery : AizenQuery<AdminVesselOverviewResponse>
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public string? SearchTerm { get; }
    public bool? IsArchived { get; }
    public GetVesselOverviewBffQuery(int pageIndex, int pageSize, string? searchTerm, bool? isArchived)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        SearchTerm = searchTerm;
        IsArchived = isArchived;
    }
}
