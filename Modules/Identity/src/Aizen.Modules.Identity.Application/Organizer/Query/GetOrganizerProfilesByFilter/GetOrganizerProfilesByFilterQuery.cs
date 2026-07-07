using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using MiniUow.Paging;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Organizer;

public sealed class GetOrganizerProfilesByFilterQuery : AizenPagedQuery<OrganizerProfileListItemDto>
{
    public string? FirstName { get; }
    public string? LastName { get; }
    public string? ApprovalStatus { get; }
    public string? SearchTerm { get; }
    public string? Status { get; }
    public string? City { get; }
    public string? Country { get; }
    public int PageIndex { get; }
    public int PageSize { get; }

    public GetOrganizerProfilesByFilterQuery(string? firstName, string? lastName, string? approvalStatus, string? searchTerm, string? status, string? city, string? country, int pageIndex, int pageSize)
    {
        FirstName = firstName;
        LastName = lastName;
        ApprovalStatus = approvalStatus;
        SearchTerm = searchTerm;
        Status = status;
        City = city;
        Country = country;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
