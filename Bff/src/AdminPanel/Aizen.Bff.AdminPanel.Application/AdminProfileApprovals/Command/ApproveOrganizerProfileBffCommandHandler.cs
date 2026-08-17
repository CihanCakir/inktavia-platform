using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Command;

[DocumentationInfo("Approve organizer profile BFF command handler", "Calls Identity admin approve endpoint for an organizer profile using the BFF service token.")]
public sealed class ApproveOrganizerProfileBffCommandHandler
    : AizenCommandHandler<ApproveOrganizerProfileBffCommand, ProfileApprovalDecisionBffResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly ILogger<ApproveOrganizerProfileBffCommandHandler> _logger;

    public ApproveOrganizerProfileBffCommandHandler(
        IIdentityAdminBffRemoteCall identity,
        ILogger<ApproveOrganizerProfileBffCommandHandler> logger)
    {
        _identity = identity;
        _logger = logger;
    }

    public override async Task<ProfileApprovalDecisionBffResponse?> Handle(
        ApproveOrganizerProfileBffCommand request, CancellationToken cancellationToken)
    {
        var response = new ProfileApprovalDecisionBffResponse();

        try
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ApproveOrganizerBff] Failed to acquire Keycloak service token.");
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        var result = await _identity.ApproveOrganizerProfileAdmin(
            request.UserId, request.ProfileId);

        if (result?.Header?.IsSuccess != true)
        {
            _logger.LogWarning("[ApproveOrganizerBff] Identity approval failed: userId={UserId} profileId={ProfileId} errorCode={Code}",
                request.UserId, request.ProfileId, result?.Header?.ErrorCode);
            response.Warnings.Add(AdminBffWarning.CallFailed("Identity", result?.Header?.ErrorMessage ?? "Approval failed."));
            return response;
        }

        _logger.LogInformation("[ApproveOrganizerBff] Organizer profile approved: userId={UserId} profileId={ProfileId}",
            request.UserId, request.ProfileId);

        response.Decision = new ProfileApprovalDecisionBffDto
        {
            UserId = request.UserId,
            ProfileId = request.ProfileId,
            ProfileType = "organizer",
            Status = "approved",
            ReviewedAt = null,
            ReviewedByName = null
        };

        return response;
    }
}
