using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Command;

[DocumentationInfo("Approve venue profile command handler", "Approves a venue profile via the Identity module admin endpoint.")]
public sealed class ApproveVenueProfileCommandHandler
    : AizenCommandHandler<ApproveVenueProfileCommand, AdminBffCommandResultDto>
{
    private readonly IIdentityRemoteCall _identity;

    public ApproveVenueProfileCommandHandler(IIdentityRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        ApproveVenueProfileCommand request, CancellationToken cancellationToken)
    {

        var result = await _identity.ApproveVenueProfile(
            request.UserId,
            request.ProfileId);

        return result.Header.IsSuccess
            ? AdminBffCommandResultDto.Ok()
            : AdminBffCommandResultDto.Fail(result.Header.ErrorMessage ?? "Approval failed.");
    }
}
