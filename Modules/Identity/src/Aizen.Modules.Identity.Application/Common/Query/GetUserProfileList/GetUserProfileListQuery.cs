using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Common;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;

public sealed class GetUserProfileListQuery : AizenListedQuery<UserProfileListItemDto>
{
    public string? FirstName { get; }
    public string? LastName { get; }
    public string? RoleContext { get; }
    public string? ApprovalStatus { get; }

    public GetUserProfileListQuery(string? firstName, string? lastName, string? roleContext, string? approvalStatus)
    {
        FirstName = firstName;
        LastName = lastName;
        RoleContext = roleContext;
        ApprovalStatus = approvalStatus;
    }
}
