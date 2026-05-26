using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;
using MiniUow.Paging;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Participant;

public sealed class GetParticipantProfilesByFilterQuery : AizenPagedQuery<ParticipantProfileListItemDto>
{
    public string? FirstName { get; }
    public string? LastName { get; }
    public string? ApprovalStatus { get; }
    public int PageIndex { get; }
    public int PageSize { get; }

    public GetParticipantProfilesByFilterQuery(string? firstName, string? lastName, string? approvalStatus, int pageIndex, int pageSize)
    {
        FirstName = firstName;
        LastName = lastName;
        ApprovalStatus = approvalStatus;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
