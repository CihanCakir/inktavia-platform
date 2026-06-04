using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Command;

public sealed class ApproveVenueProfileCommand : AizenCommand<AdminBffCommandResultDto>
{
    public long UserId { get; }
    public Guid ProfileId { get; }
    public string Authorization { get; }
    public string UserToken { get; }
    public ApproveVenueProfileCommand(long userId, Guid profileId, string authorization, string userToken)
    {
        UserId = userId;
        ProfileId = profileId;
        Authorization = authorization;
        UserToken = userToken;
    }
}
