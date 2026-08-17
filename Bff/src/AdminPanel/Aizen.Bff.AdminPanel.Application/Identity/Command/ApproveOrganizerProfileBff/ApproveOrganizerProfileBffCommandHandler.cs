using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Identity.Command;

[DocumentationInfo("Approve organizer profile command handler", "Approves an organizer profile via the Identity module admin endpoint.")]
public sealed class ApproveOrganizerProfileBffCommandHandler
    : AizenCommandHandler<ApproveOrganizerProfileBffCommand, AdminBffCommandResultDto>
{
    private readonly IIdentityRemoteCall _identity;

    public ApproveOrganizerProfileBffCommandHandler(IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        ApproveOrganizerProfileBffCommand request, CancellationToken cancellationToken)
    {

        var result = await _identity.ApproveOrganizerProfile(
            request.UserId,
            request.ProfileId);

        return result.Header.IsSuccess
            ? AdminBffCommandResultDto.Ok()
            : AdminBffCommandResultDto.Fail(result.Header.ErrorMessage ?? "Approval failed.");
    }
}
