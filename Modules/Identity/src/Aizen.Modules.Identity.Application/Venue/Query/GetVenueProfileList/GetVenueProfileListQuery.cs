using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Venue;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Venue;

public sealed class GetVenueProfileListQuery : AizenListedQuery<VenueProfileListItemDto>
{
    public string? FirstName { get; }
    public string? LastName { get; }
    public string? ApprovalStatus { get; }

    public GetVenueProfileListQuery(string? firstName, string? lastName, string? approvalStatus)
    {
        FirstName = firstName;
        LastName = lastName;
        ApprovalStatus = approvalStatus;
    }
}
