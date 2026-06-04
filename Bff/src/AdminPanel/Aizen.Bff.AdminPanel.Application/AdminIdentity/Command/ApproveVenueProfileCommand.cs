using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Command;

public sealed class ApproveVenueProfileCommand : AizenCommand<AdminBffCommandResultDto>
{
    public long UserId { get; }
    public Guid ProfileId { get; }
    public string Authorization { get; }
    public ApproveVenueProfileCommand(long userId, Guid profileId, string authorization)
    {
        UserId = userId;
        ProfileId = profileId;
        Authorization = authorization;
    }
}

[DocumentationInfo("Approve venue profile command handler", "Approves a venue profile via the Identity module admin endpoint.")]
public sealed class ApproveVenueProfileCommandHandler
    : AizenCommandHandler<ApproveVenueProfileCommand, AdminBffCommandResultDto>
{
    private readonly IIdentityAdminBffRemoteCall _identity;

    public ApproveVenueProfileCommandHandler(IIdentityAdminBffRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        ApproveVenueProfileCommand request, CancellationToken cancellationToken)
    {
        var result = await _identity.ApproveVenueProfile(
            request.UserId,
            request.ProfileId,
            request.Authorization);

        return result.Header.IsSuccess
            ? AdminBffCommandResultDto.Ok()
            : AdminBffCommandResultDto.Fail(result.Header.ErrorMessage ?? "Approval failed.");
    }
}
