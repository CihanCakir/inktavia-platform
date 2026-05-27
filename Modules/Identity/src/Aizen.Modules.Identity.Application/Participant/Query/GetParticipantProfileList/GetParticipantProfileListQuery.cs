using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Participant;

public sealed class GetParticipantProfileListQuery : AizenListedQuery<ParticipantProfileListItemDto>
{
    public string? FirstName { get; }
    public string? LastName { get; }
    public string? ApprovalStatus { get; }

    public GetParticipantProfileListQuery(string? firstName, string? lastName, string? approvalStatus)
    {
        FirstName = firstName;
        LastName = lastName;
        ApprovalStatus = approvalStatus;
    }
}
