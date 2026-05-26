using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Venue;
using MiniUow.Paging;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Venue;

public sealed class GetVenueProfilesByFilterQuery : AizenPagedQuery<VenueProfileListItemDto>
{
    public string? FirstName { get; }
    public string? LastName { get; }
    public string? ApprovalStatus { get; }
    public int PageIndex { get; }
    public int PageSize { get; }

    public GetVenueProfilesByFilterQuery(string? firstName, string? lastName, string? approvalStatus, int pageIndex, int pageSize)
    {
        FirstName = firstName;
        LastName = lastName;
        ApprovalStatus = approvalStatus;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
