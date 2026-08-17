using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Command;

public sealed class ApproveOrganizerProfileCommand : AizenCommand<AdminBffCommandResultDto>
{
    public long UserId { get; }
    public Guid ProfileId { get; }
    public ApproveOrganizerProfileCommand(long userId, Guid profileId)
    {
        UserId = userId;
        ProfileId = profileId;
    }
}
