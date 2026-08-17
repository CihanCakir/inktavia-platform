using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Identity.Command;

public sealed class ApproveVenueProfileBffCommand : AizenCommand<AdminBffCommandResultDto>
{
    public long UserId { get; }
    public Guid ProfileId { get; }
    public ApproveVenueProfileBffCommand(long userId, Guid profileId)
    {
        UserId = userId;
        ProfileId = profileId;
    }
}
