using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user quick BFF query handler", "Returns compact user summary for the quick-view panel.")]
public sealed class GetAdminUserQuickBffQueryHandler
    : AizenQueryHandler<GetAdminUserQuickBffQuery, AdminUserQuickBffResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    private readonly ILogger<GetAdminUserQuickBffQueryHandler> _logger;

    public GetAdminUserQuickBffQueryHandler(
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider,
        ILogger<GetAdminUserQuickBffQueryHandler> logger)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
        _logger = logger;
    }

    public override async Task<AdminUserQuickBffResponse?> Handle(
        GetAdminUserQuickBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminUserQuickBffResponse();

        string authHeader;
        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
            authHeader = $"Bearer {serviceToken}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserQuickBff] Failed to acquire Keycloak service token.");
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        try
        {
            var result = await _identity.GetAdminUserProfileDetail(request.ProfileId, authHeader, request.UserToken);

            if (result?.Header?.IsSuccess != true || result.Body == null)
                return response; // User is null → controller returns 404

            var p = result.Body;
            response.User = new AdminUserQuickBffDto
            {
                Id = p.Id.ToString(),
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = null,
                Phone = null,
                Role = AdminUserBffHelpers.MapRole(p.RoleContext),
                Status = AdminUserBffHelpers.MapStatus(p.ApprovalStatus, p.Status),
                IdentityType = p.RoleContext,
                AvatarInitials = AdminUserBffHelpers.ComputeAvatarInitials(p.FirstName, p.LastName),
                LastLoginAt = null,
                CreatedAt = p.CreateDate?.ToString("o") ?? string.Empty,
                VesselCount = 0,    // TODO: needs vessel-count-by-userId
                Vessels = new(),    // TODO: no userId filter in vessel list endpoint
                RecentActivity = new() // TODO: requires activity aggregation across modules
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserQuickBff] Identity call failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
        }

        return response;
    }
}
