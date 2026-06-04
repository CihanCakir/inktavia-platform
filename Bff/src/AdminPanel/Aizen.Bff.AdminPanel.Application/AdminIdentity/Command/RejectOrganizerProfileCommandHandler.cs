using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Request;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Command;

[DocumentationInfo("Reject organizer profile command handler", "Rejects an organizer profile with a reason via the Identity module admin endpoint.")]
public sealed class RejectOrganizerProfileCommandHandler
    : AizenCommandHandler<RejectOrganizerProfileCommand, AdminBffCommandResultDto>
{
    private readonly IIdentityAdminBffRemoteCall _identity;

    public RejectOrganizerProfileCommandHandler(IIdentityAdminBffRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        RejectOrganizerProfileCommand request, CancellationToken cancellationToken)
    {
        var result = await _identity.RejectOrganizerProfile(
            request.UserId,
            request.ProfileId,
            new RejectProfileRequest { Reason = request.Reason },
            request.Authorization,
            request.UserToken);

        return result.Header.IsSuccess
            ? AdminBffCommandResultDto.Ok()
            : AdminBffCommandResultDto.Fail(result.Header.ErrorMessage ?? "Rejection failed.");
    }
}
