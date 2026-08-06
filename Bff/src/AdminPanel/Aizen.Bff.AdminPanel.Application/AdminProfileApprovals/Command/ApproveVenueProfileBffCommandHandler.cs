using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Command;

[DocumentationInfo("Approve venue profile BFF command handler", "Calls Identity admin approve endpoint for a venue profile using the BFF service token.")]
public sealed class ApproveVenueProfileBffCommandHandler
    : AizenCommandHandler<ApproveVenueProfileBffCommand, ProfileApprovalDecisionBffResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<ApproveVenueProfileBffCommandHandler> _logger;

    public ApproveVenueProfileBffCommandHandler(
        IIdentityRemoteCall identity,
        ILogger<ApproveVenueProfileBffCommandHandler> logger)
    {
        _identity = identity;
        _logger = logger;
    }

    public override async Task<ProfileApprovalDecisionBffResponse?> Handle(
        ApproveVenueProfileBffCommand request, CancellationToken cancellationToken)
    {
        var response = new ProfileApprovalDecisionBffResponse();

        try
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ApproveVenueBff] Failed to acquire Keycloak service token.");
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        var result = await _identity.ApproveVenueProfileAdmin(
            request.UserId, request.ProfileId);

        if (result?.Header?.IsSuccess != true)
        {
            _logger.LogWarning("[ApproveVenueBff] Identity approval failed: userId={UserId} profileId={ProfileId} errorCode={Code}",
                request.UserId, request.ProfileId, result?.Header?.ErrorCode);
            response.Warnings.Add(AdminBffWarning.CallFailed("Identity", result?.Header?.ErrorMessage ?? "Approval failed."));
            return response;
        }

        _logger.LogInformation("[ApproveVenueBff] Venue profile approved: userId={UserId} profileId={ProfileId}",
            request.UserId, request.ProfileId);

        response.Decision = new ProfileApprovalDecisionBffDto
        {
            UserId = request.UserId,
            ProfileId = request.ProfileId,
            ProfileType = "venue",
            Status = "approved",
            ReviewedAt = null,
            ReviewedByName = null
        };

        return response;
    }
}
