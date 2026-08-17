using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Command;

public sealed class RejectVenueProfileCommand : AizenCommand<AdminBffCommandResultDto>
{
    public long UserId { get; }
    public Guid ProfileId { get; }
    public string Reason { get; }
    public RejectVenueProfileCommand(long userId, Guid profileId, string reason)
    {
        UserId = userId;
        ProfileId = profileId;
        Reason = reason;
    }
}
