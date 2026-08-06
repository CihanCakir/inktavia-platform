using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

public sealed class SearchOrganizerProfilesBffQuery : AizenQuery<PagedOrganizerProfileResult>
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public string? SearchTerm { get; }
    public string? ApprovalStatus { get; }
    public string? Status { get; }
    public string? City { get; }
    public string? Country { get; }

    public SearchOrganizerProfilesBffQuery(int pageIndex, int pageSize, string? searchTerm = null, string? approvalStatus = null, string? status = null, string? city = null, string? country = null)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        SearchTerm = searchTerm;
        ApprovalStatus = approvalStatus;
        Status = status;
        City = city;
        Country = country;
    }
}
