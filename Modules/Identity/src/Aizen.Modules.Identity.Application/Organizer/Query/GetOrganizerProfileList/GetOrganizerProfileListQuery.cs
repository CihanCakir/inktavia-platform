using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Organizer;

public sealed class GetOrganizerProfileListQuery : AizenListedQuery<OrganizerProfileListItemDto>
{
    public string? FirstName { get; }
    public string? LastName { get; }
    public string? ApprovalStatus { get; }

    public GetOrganizerProfileListQuery(string? firstName, string? lastName, string? approvalStatus)
    {
        FirstName = firstName;
        LastName = lastName;
        ApprovalStatus = approvalStatus;
    }
}
