using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Identity.Command;

public sealed class RejectVenueProfileBffCommand : AizenCommand<AdminBffCommandResultDto>
{
    public long UserId { get; }
    public Guid ProfileId { get; }
    public string Reason { get; }
    public RejectVenueProfileBffCommand(long userId, Guid profileId, string reason)
    {
        UserId = userId;
        ProfileId = profileId;
        Reason = reason;
    }
}
