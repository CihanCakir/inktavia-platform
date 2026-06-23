using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user quick BFF query handler", "Returns compact user summary with top vessels for the quick-view panel.")]
public sealed class GetAdminUserQuickBffQueryHandler
    : AizenQueryHandler<GetAdminUserQuickBffQuery, AdminUserQuickBffResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IVesselAdminBffRemoteCall _vessel;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    private readonly ILogger<GetAdminUserQuickBffQueryHandler> _logger;

    public GetAdminUserQuickBffQueryHandler(
        IIdentityAdminBffRemoteCall identity,
        IVesselAdminBffRemoteCall vessel,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider,
        ILogger<GetAdminUserQuickBffQueryHandler> logger)
    {
        _identity = identity;
        _vessel = vessel;
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
            var identityResult = await _identity.GetAdminUserProfileDetail(request.ProfileId, authHeader, request.UserToken);

            if (identityResult?.Header?.IsSuccess != true || identityResult.Body == null)
                return response; // User is null → controller returns 404

            var p = identityResult.Body;

            // Fetch top 3 vessels for the user.
            var vesselItems = new List<AdminUserVesselSummaryBffDto>();
            int vesselCount = 0;
            try
            {
                var vesselResult = await _vessel.GetAdminVesselList(
                    authHeader, request.UserToken,
                    pageIndex: 0, pageSize: 3,
                    ownerUserId: p.UserId);

                if (vesselResult?.Body?.Vessels is { } page)
                {
                    vesselCount = (int)page.Count;
                    vesselItems = page.Items?
                        .Select(v => new AdminUserVesselSummaryBffDto
                        {
                            Id = v.Id.ToString(),
                            Name = v.Name,
                            Type = v.VesselTypeCode
                        })
                        .ToList() ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[UserQuickBff] Vessel fetch failed for userId={UserId}: {Message}", p.UserId, ex.Message);
                response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel"));
            }

            response.User = new AdminUserQuickBffDto
            {
                Id = p.Id.ToString(),
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                Phone = p.PhoneNumber,
                Role = AdminUserBffHelpers.MapRole(p.RoleContext),
                Status = AdminUserBffHelpers.MapStatus(p.ApprovalStatus, p.Status),
                IdentityType = p.RoleContext,
                AvatarInitials = AdminUserBffHelpers.ComputeAvatarInitials(p.FirstName, p.LastName),
                LastLoginAt = null, // TODO: not tracked locally; requires Keycloak event sync
                CreatedAt = p.CreateDate?.ToString("o") ?? string.Empty,
                VesselCount = vesselCount,
                Vessels = vesselItems,
                RecentActivity = new() // TODO: requires cross-module activity aggregation
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
