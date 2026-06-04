using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Command;

public sealed class ApproveOrganizerProfileCommand : AizenCommand<AdminBffCommandResultDto>
{
    public long UserId { get; }
    public Guid ProfileId { get; }
    public string Authorization { get; }
    public ApproveOrganizerProfileCommand(long userId, Guid profileId, string authorization)
    {
        UserId = userId;
        ProfileId = profileId;
        Authorization = authorization;
    }
}

[DocumentationInfo("Approve organizer profile command handler", "Approves an organizer profile via the Identity module admin endpoint.")]
public sealed class ApproveOrganizerProfileCommandHandler
    : AizenCommandHandler<ApproveOrganizerProfileCommand, AdminBffCommandResultDto>
{
    private readonly IIdentityAdminBffRemoteCall _identity;

    public ApproveOrganizerProfileCommandHandler(IIdentityAdminBffRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        ApproveOrganizerProfileCommand request, CancellationToken cancellationToken)
    {
        var result = await _identity.ApproveOrganizerProfile(
            request.UserId,
            request.ProfileId,
            request.Authorization);

        return result.Header.IsSuccess
            ? AdminBffCommandResultDto.Ok()
            : AdminBffCommandResultDto.Fail(result.Header.ErrorMessage ?? "Approval failed.");
    }
}
