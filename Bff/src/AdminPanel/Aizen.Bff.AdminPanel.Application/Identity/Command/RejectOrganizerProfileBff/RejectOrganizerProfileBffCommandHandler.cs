using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Request;

namespace Aizen.Bff.AdminPanel.Application.Identity.Command;

[DocumentationInfo("Reject organizer profile command handler", "Rejects an organizer profile with a reason via the Identity module admin endpoint.")]
public sealed class RejectOrganizerProfileBffCommandHandler
    : AizenCommandHandler<RejectOrganizerProfileBffCommand, AdminBffCommandResultDto>
{
    private readonly IIdentityRemoteCall _identity;

    public RejectOrganizerProfileBffCommandHandler(IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        RejectOrganizerProfileBffCommand request, CancellationToken cancellationToken)
    {

        var result = await _identity.RejectOrganizerProfile(
            request.UserId,
            request.ProfileId,
            new RejectProfileRequest { Reason = request.Reason });

        return result.Header.IsSuccess
            ? AdminBffCommandResultDto.Ok()
            : AdminBffCommandResultDto.Fail(result.Header.ErrorMessage ?? "Rejection failed.");
    }
}
