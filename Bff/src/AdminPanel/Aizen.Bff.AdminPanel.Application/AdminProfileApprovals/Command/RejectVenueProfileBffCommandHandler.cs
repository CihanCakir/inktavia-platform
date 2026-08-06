using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Request;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Command;

[DocumentationInfo("Reject venue profile BFF command handler", "Validates rejection reason and calls Identity admin reject endpoint for a venue profile.")]
public sealed class RejectVenueProfileBffCommandHandler
    : AizenCommandHandler<RejectVenueProfileBffCommand, ProfileApprovalDecisionBffResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<RejectVenueProfileBffCommandHandler> _logger;

    public RejectVenueProfileBffCommandHandler(
        IIdentityRemoteCall identity,
        ILogger<RejectVenueProfileBffCommandHandler> logger)
    {
        _identity = identity;
        _logger = logger;
    }

    public override async Task<ProfileApprovalDecisionBffResponse?> Handle(
        RejectVenueProfileBffCommand request, CancellationToken cancellationToken)
    {
        var response = new ProfileApprovalDecisionBffResponse();

        var trimmedReason = request.Reason?.Trim() ?? string.Empty;
        _logger.LogDebug("[RejectVenueBff] Rejection reason length: {Length}", trimmedReason.Length);

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
            _logger.LogWarning(ex, "[RejectVenueBff] Failed to acquire Keycloak service token.");
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        var result = await _identity.RejectVenueProfileAdmin(
            request.UserId,
            request.ProfileId,
            new RejectProfileRequest { Reason = trimmedReason });

        if (result?.Header?.IsSuccess != true)
        {
            _logger.LogWarning("[RejectVenueBff] Identity rejection failed: userId={UserId} profileId={ProfileId} errorCode={Code}",
                request.UserId, request.ProfileId, result?.Header?.ErrorCode);
            response.Warnings.Add(AdminBffWarning.CallFailed("Identity", result?.Header?.ErrorMessage ?? "Rejection failed."));
            return response;
        }

        _logger.LogInformation("[RejectVenueBff] Venue profile rejected: userId={UserId} profileId={ProfileId}",
            request.UserId, request.ProfileId);

        response.Decision = new ProfileApprovalDecisionBffDto
        {
            UserId = request.UserId,
            ProfileId = request.ProfileId,
            ProfileType = "venue",
            Status = "rejected",
            ReviewedAt = null,
            ReviewedByName = null
        };

        return response;
    }
}
