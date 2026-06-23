using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Venue;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Query;

[DocumentationInfo("Get venue approval detail BFF query handler", "Fetches venue profile detail and with-user info in parallel from Identity, maps to BFF review DTO.")]
public sealed class GetVenueApprovalDetailBffQueryHandler
    : AizenQueryHandler<GetVenueApprovalDetailBffQuery, VenueApprovalDetailBffResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    private readonly ILogger<GetVenueApprovalDetailBffQueryHandler> _logger;

    public GetVenueApprovalDetailBffQueryHandler(
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider,
        ILogger<GetVenueApprovalDetailBffQueryHandler> logger)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
        _logger = logger;
    }

    public override async Task<VenueApprovalDetailBffResponse?> Handle(
        GetVenueApprovalDetailBffQuery request, CancellationToken cancellationToken)
    {
        var response = new VenueApprovalDetailBffResponse();

        string authHeader;
        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
            authHeader = $"Bearer {serviceToken}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VenueApprovalDetailBff] Failed to acquire Keycloak service token.");
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        try
        {
            // Fetch detail-only (for RejectReason) and with-user (for Email/Phone) in parallel.
            var detailTask = _identity.GetAdminVenueProfileOnly(
                request.ProfileId, authHeader, request.UserToken);
            var withUserTask = _identity.GetAdminVenueProfileWithUser(
                request.ProfileId, authHeader, request.UserToken);

            await Task.WhenAll(detailTask, withUserTask);

            var detailResult = await detailTask;
            var withUserResult = await withUserTask;

            if (detailResult?.Header?.IsSuccess != true || detailResult.Body == null)
            {
                _logger.LogWarning("[VenueApprovalDetailBff] Profile {ProfileId} not found.", request.ProfileId);
                return response;
            }

            var detail = detailResult.Body;
            VenueProfileWithUserDetailDto? withUser = null;

            if (withUserResult?.Header?.IsSuccess == true)
                withUser = withUserResult.Body;
            else
                response.Warnings.Add(AdminBffWarning.CallFailed("Identity.VenueWithUser", "Could not fetch user contact info."));

            response.Venue = MapToDetailDto(detail, withUser);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VenueApprovalDetailBff] Identity call failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
        }

        return response;
    }

    private static VenueApprovalDetailBffDto MapToDetailDto(
        VenueProfileDetailDto detail,
        VenueProfileWithUserDetailDto? withUser)
    {
        var reviewedAt = detail.ApprovalStatus?.ToLowerInvariant() switch
        {
            "approved" => detail.ApprovedAt?.ToString("O"),
            "rejected" => detail.RejectedAt?.ToString("O"),
            _ => null
        };

        var fullName = $"{detail.FirstName} {detail.LastName}".Trim();

        return new VenueApprovalDetailBffDto
        {
            UserId = detail.UserId,
            ProfileId = detail.Id,
            Status = MapApprovalStatus(detail.ApprovalStatus),
            ReviewedBy = null,
            ReviewedAt = reviewedAt,
            RejectionCategory = null,
            RejectionReason = detail.RejectReason,
            InternalNote = null,
            Owner = new VenueOwnerBffDto
            {
                FullName = string.IsNullOrEmpty(fullName) ? null : fullName,
                Email = withUser?.Email,
                Phone = withUser?.PhoneNumber,
                AvatarUrl = detail.ProfilePhotoUrl,
                RegisteredAt = withUser?.UserCreatedAt?.ToString("O")
            },
            Venue = new VenueDetailBffDto(),
            Location = new VenueLocationBffDto
            {
                Latitude = null,
                Longitude = null,
                AddressVerified = false,
                DisplayText = null
            },
            Checklist = new ProfileApprovalChecklistBffDto
            {
                EmailVerified = false,
                PhoneVerified = false,
                CompanyNameProvided = false,
                TaxNumberProvided = false,
                DocumentsUploaded = false,
                DuplicateAccountFound = false,
                SuspiciousActivityFound = false
            },
            Documents = new List<ProfileApprovalDocumentBffDto>(),
            RiskSignals = new List<ProfileApprovalRiskSignalBffDto>(),
            Activity = new List<ProfileApprovalActivityItemBffDto>()
        };
    }

    private static string MapApprovalStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "pending" => "pending",
        "approved" => "approved",
        "rejected" => "rejected",
        _ => "pending"
    };
}
