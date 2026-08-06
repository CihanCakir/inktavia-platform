using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Identity.Command;

public sealed class ApproveOrganizerProfileBffCommand : AizenCommand<AdminBffCommandResultDto>
{
    public long UserId { get; }
    public Guid ProfileId { get; }
    public ApproveOrganizerProfileBffCommand(long userId, Guid profileId)
    {
        UserId = userId;
        ProfileId = profileId;
    }
}
