using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Request;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Command;

public sealed class RejectVenueProfileCommand : AizenCommand<AdminBffCommandResultDto>
{
    public long UserId { get; }
    public Guid ProfileId { get; }
    public string Reason { get; }
    public string Authorization { get; }
    public RejectVenueProfileCommand(long userId, Guid profileId, string reason, string authorization)
    {
        UserId = userId;
        ProfileId = profileId;
        Reason = reason;
        Authorization = authorization;
    }
}

[DocumentationInfo("Reject venue profile command handler", "Rejects a venue profile with a reason via the Identity module admin endpoint.")]
public sealed class RejectVenueProfileCommandHandler
    : AizenCommandHandler<RejectVenueProfileCommand, AdminBffCommandResultDto>
{
    private readonly IIdentityAdminBffRemoteCall _identity;

    public RejectVenueProfileCommandHandler(IIdentityAdminBffRemoteCall identity)
    {
        _identity = identity;
    }

    public override async Task<AdminBffCommandResultDto?> Handle(
        RejectVenueProfileCommand request, CancellationToken cancellationToken)
    {
        var result = await _identity.RejectVenueProfile(
            request.UserId,
            request.ProfileId,
            new RejectProfileRequest { Reason = request.Reason },
            request.Authorization);

        return result.Header.IsSuccess
            ? AdminBffCommandResultDto.Ok()
            : AdminBffCommandResultDto.Fail(result.Header.ErrorMessage ?? "Rejection failed.");
    }
}
