using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user list BFF query handler", "Fetches paged user profile list from Identity and maps to UI-ready response.")]
public sealed class GetAdminUserListBffQueryHandler
    : AizenQueryHandler<GetAdminUserListBffQuery, AdminUserListBffResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    private readonly ILogger<GetAdminUserListBffQueryHandler> _logger;

    public GetAdminUserListBffQueryHandler(
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider,
        ILogger<GetAdminUserListBffQueryHandler> logger)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
        _logger = logger;
    }

    public override async Task<AdminUserListBffResponse?> Handle(
        GetAdminUserListBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminUserListBffResponse
        {
            Page = request.Page,
            PageSize = request.PageSize
        };

        string authHeader;
        try
        {
            var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
            authHeader = $"Bearer {serviceToken}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserListBff] Failed to acquire Keycloak service token: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        try
        {
            // Identity SearchProfiles uses 0-based pageIndex; our API uses 1-based page.
            var pageIndex = Math.Max(0, request.Page - 1);

            // Map BFF status to the correct Identity filter:
            //   Active/Deactivated → approvalStatus filter
            //   Suspended          → status=Inactive (ProfileStatus enum)
            var (approvalStatusFilter, profileStatusFilter) = MapStatusFilters(request.Status);

            var result = await _identity.SearchProfiles(
                authHeader,
                request.UserToken,
                firstName: request.Search,
                lastName: null,
                roleContext: request.IdentityType ?? MapRoleToRoleContext(request.Role),
                approvalStatus: approvalStatusFilter,
                status: profileStatusFilter,
                email: request.Search, // also search by email when search term is provided
                pageIndex: pageIndex,
                pageSize: request.PageSize);

            if (result?.Header?.IsSuccess != true)
            {
                _logger.LogWarning("[UserListBff] Identity SearchProfiles returned non-success. ErrorCode={Code}", result?.Header?.ErrorCode);
                response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
                return response;
            }

            var body = result.Body;
            response.Total = body?.TotalCount ?? 0;
            response.Items = (body?.Items ?? new())
                .Select(p => new AdminUserListItemBffDto
                {
                    Id = p.Id.ToString(),
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email,
                    Phone = p.PhoneNumber,
                    Role = AdminUserBffHelpers.MapRole(p.RoleContext),
                    Status = AdminUserBffHelpers.MapStatus(p.ApprovalStatus, p.Status),
                    IdentityType = p.RoleContext,
                    VesselCount = 0, // TODO: needs batch vessel-count-by-userId endpoint
                    LastLoginAt = null, // TODO: not tracked locally; requires Keycloak event sync
                    CreatedAt = p.CreateDate?.ToString("o") ?? string.Empty,
                    AvatarInitials = AdminUserBffHelpers.ComputeAvatarInitials(p.FirstName, p.LastName)
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserListBff] Identity call failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
        }

        return response;
    }

    /// <summary>
    /// Maps the BFF status filter to the appropriate Identity module filter pair.
    /// Returns (approvalStatus, profileStatus) — at most one will be non-null.
    /// </summary>
    private static (string? approvalStatus, string? profileStatus) MapStatusFilters(string? status) => status switch
    {
        "Pending" => ("Pending", null),
        "Active" => ("Approved", null),
        "Deactivated" => ("Rejected", null),
        "Suspended" => (null, "Inactive"),
        _ => (null, null)
    };

    private static string? MapRoleToRoleContext(string? role) => role switch
    {
        "Organizer" => "Organizer",
        "Participant" => "Participant",
        "VenueManager" => "Venue",
        _ => null
    };
}
