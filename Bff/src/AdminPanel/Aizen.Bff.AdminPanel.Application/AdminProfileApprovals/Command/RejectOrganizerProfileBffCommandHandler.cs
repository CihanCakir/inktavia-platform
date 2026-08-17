using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Request;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Command;

[DocumentationInfo("Reject organizer profile BFF command handler", "Validates rejection reason and calls Identity admin reject endpoint for an organizer profile.")]
public sealed class RejectOrganizerProfileBffCommandHandler
    : AizenCommandHandler<RejectOrganizerProfileBffCommand, ProfileApprovalDecisionBffResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly ILogger<RejectOrganizerProfileBffCommandHandler> _logger;

    public RejectOrganizerProfileBffCommandHandler(
        IIdentityAdminBffRemoteCall identity,
        ILogger<RejectOrganizerProfileBffCommandHandler> logger)
    {
        _identity = identity;
        _logger = logger;
    }

    public override async Task<ProfileApprovalDecisionBffResponse?> Handle(
        RejectOrganizerProfileBffCommand request, CancellationToken cancellationToken)
    {
        var response = new ProfileApprovalDecisionBffResponse();

        var trimmedReason = request.Reason?.Trim() ?? string.Empty;
        _logger.LogDebug("[RejectOrganizerBff] Rejection reason length: {Length}", trimmedReason.Length);

        if (trimmedReason.Length < 10 || trimmedReason.Length > 1000)
        {
            response.Warnings.Add(new AdminBffWarning("Validation",
                "Rejection reason must be between 10 and 1000 characters."));
            return response;
        }

        try
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RejectOrganizerBff] Failed to acquire Keycloak service token.");
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        var result = await _identity.RejectOrganizerProfileAdmin(
            request.UserId,
            request.ProfileId,
            new RejectProfileRequest { Reason = trimmedReason });

        if (result?.Header?.IsSuccess != true)
        {
            _logger.LogWarning("[RejectOrganizerBff] Identity rejection failed: userId={UserId} profileId={ProfileId} errorCode={Code}",
                request.UserId, request.ProfileId, result?.Header?.ErrorCode);
            response.Warnings.Add(AdminBffWarning.CallFailed("Identity", result?.Header?.ErrorMessage ?? "Rejection failed."));
            return response;
        }

        _logger.LogInformation("[RejectOrganizerBff] Organizer profile rejected: userId={UserId} profileId={ProfileId}",
            request.UserId, request.ProfileId);

        response.Decision = new ProfileApprovalDecisionBffDto
        {
            UserId = request.UserId,
            ProfileId = request.ProfileId,
            ProfileType = "organizer",
            Status = "rejected",
            ReviewedAt = null,
            ReviewedByName = null
        };

        return response;
    }
}
