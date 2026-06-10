using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Command;

public sealed class RejectOrganizerProfileCommand : AizenCommand<AdminBffCommandResultDto>
{
    public long UserId { get; }
    public Guid ProfileId { get; }
    public string Reason { get; }
    public string UserToken { get; }
    public RejectOrganizerProfileCommand(long userId, Guid profileId, string reason, string userToken)
    {
        UserId = userId;
        ProfileId = profileId;
        Reason = reason;
        UserToken = userToken;
    }
}
