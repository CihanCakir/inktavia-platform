using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user detail BFF query handler", "Fetches profile detail from Identity and maps to the admin user detail response.")]
public sealed class GetAdminUserDetailBffQueryHandler
    : AizenQueryHandler<GetAdminUserDetailBffQuery, AdminUserDetailBffResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    private readonly ILogger<GetAdminUserDetailBffQueryHandler> _logger;

    public GetAdminUserDetailBffQueryHandler(
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider,
        ILogger<GetAdminUserDetailBffQueryHandler> logger)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
        _logger = logger;
    }

    public override async Task<AdminUserDetailBffResponse?> Handle(
        GetAdminUserDetailBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminUserDetailBffResponse();

        string authHeader;
        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
            authHeader = $"Bearer {serviceToken}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserDetailBff] Failed to acquire Keycloak service token.");
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        try
        {
            var result = await _identity.GetAdminUserProfileDetail(request.ProfileId, authHeader, request.UserToken);

            if (result?.Header?.IsSuccess != true || result.Body == null)
            {
                _logger.LogWarning("[UserDetailBff] Profile {ProfileId} not found or Identity returned non-success.", request.ProfileId);
                return response; // User is null → controller returns 404
            }

            var p = result.Body;
            response.User = new AdminUserDetailBffDto
            {
                Id = p.Id.ToString(),
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = null, // TODO: not in UserProfileDetailDto
                EmailVerified = false,
                Phone = null,
                Role = AdminUserBffHelpers.MapRole(p.RoleContext),
                Status = AdminUserBffHelpers.MapStatus(p.ApprovalStatus, p.Status),
                IdentityType = p.RoleContext,
                Bio = p.Bio,
                AvatarInitials = AdminUserBffHelpers.ComputeAvatarInitials(p.FirstName, p.LastName),
                LastLoginAt = null, // TODO: not tracked locally
                CreatedAt = p.CreateDate?.ToString("o") ?? string.Empty,
                VesselCount = 0,        // TODO: needs vessel count by userId
                TotalTransactions = 0,  // TODO: no payment module remote call
                TotalServiceRequests = 0 // TODO: no userId filter in SR module
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserDetailBff] Identity call failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
        }

        return response;
    }
}
