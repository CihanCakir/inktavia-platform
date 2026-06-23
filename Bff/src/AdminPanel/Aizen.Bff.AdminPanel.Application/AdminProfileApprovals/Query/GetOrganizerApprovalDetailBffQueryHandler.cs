using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Query;

[DocumentationInfo("Get organizer approval detail BFF query handler", "Fetches organizer profile detail and with-user info in parallel from Identity, maps to BFF review DTO.")]
public sealed class GetOrganizerApprovalDetailBffQueryHandler
    : AizenQueryHandler<GetOrganizerApprovalDetailBffQuery, OrganizerApprovalDetailBffResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    private readonly ILogger<GetOrganizerApprovalDetailBffQueryHandler> _logger;

    public GetOrganizerApprovalDetailBffQueryHandler(
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider,
        ILogger<GetOrganizerApprovalDetailBffQueryHandler> logger)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
        _logger = logger;
    }

    public override async Task<OrganizerApprovalDetailBffResponse?> Handle(
        GetOrganizerApprovalDetailBffQuery request, CancellationToken cancellationToken)
    {
        var response = new OrganizerApprovalDetailBffResponse();

        string authHeader;
        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
            authHeader = $"Bearer {serviceToken}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[OrganizerApprovalDetailBff] Failed to acquire Keycloak service token.");
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        try
        {
            // Fetch detail-only (for RejectReason) and with-user (for Email/Phone) in parallel.
            var detailTask = _identity.GetAdminOrganizerProfileOnly(
                request.ProfileId, authHeader, request.UserToken);
            var withUserTask = _identity.GetAdminOrganizerProfileWithUser(
                request.ProfileId, authHeader, request.UserToken);

            await Task.WhenAll(detailTask, withUserTask);

            var detailResult = await detailTask;
            var withUserResult = await withUserTask;

            if (detailResult?.Header?.IsSuccess != true || detailResult.Body == null)
            {
                _logger.LogWarning("[OrganizerApprovalDetailBff] Profile {ProfileId} not found.", request.ProfileId);
                return response;
            }

            var detail = detailResult.Body;
            OrganizerProfileWithUserDetailDto? withUser = null;

            if (withUserResult?.Header?.IsSuccess == true)
                withUser = withUserResult.Body;
            else
                response.Warnings.Add(AdminBffWarning.CallFailed("Identity.OrganizerWithUser", "Could not fetch user contact info."));

            response.Organizer = MapToDetailDto(detail, withUser);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[OrganizerApprovalDetailBff] Identity call failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
        }

        return response;
    }

    private static OrganizerApprovalDetailBffDto MapToDetailDto(
        OrganizerProfileDetailDto detail,
        OrganizerProfileWithUserDetailDto? withUser)
    {
        var reviewedAt = detail.ApprovalStatus?.ToLowerInvariant() switch
        {
            "approved" => detail.ApprovedAt?.ToString("O"),
            "rejected" => detail.RejectedAt?.ToString("O"),
            _ => null
        };

        var fullName = $"{detail.FirstName} {detail.LastName}".Trim();

        return new OrganizerApprovalDetailBffDto
        {
            UserId = detail.UserId,
            ProfileId = detail.Id,
            Status = MapApprovalStatus(detail.ApprovalStatus),
            ReviewedBy = null,
            ReviewedAt = reviewedAt,
            RejectionCategory = null,
            RejectionReason = detail.RejectReason,
            InternalNote = null,
            Applicant = new OrganizerApplicantBffDto
            {
                FullName = string.IsNullOrEmpty(fullName) ? null : fullName,
                Email = withUser?.Email,
                Phone = withUser?.PhoneNumber,
                AvatarUrl = detail.ProfilePhotoUrl,
                RegisteredAt = withUser?.UserCreatedAt?.ToString("O"),
                IdentityType = withUser?.LoginType,
                Role = "Organizer"
            },
            Company = new OrganizerCompanyBffDto(),
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
            Activity = new List<ProfileApprovalActivityItemBffDto>(),
            Warnings = new List<AdminBffWarning>()
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
