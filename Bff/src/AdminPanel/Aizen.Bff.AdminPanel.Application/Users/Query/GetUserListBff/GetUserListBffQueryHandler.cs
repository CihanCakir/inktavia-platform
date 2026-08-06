using Aizen.Bff.AdminPanel.Application.Users.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.Users.Query;

[DocumentationInfo("Get admin user list BFF query handler", "Fetches paged user profile list from Identity, then enriches each row with vessel count via a single bulk Vessel call.")]
public sealed class GetUserListBffQueryHandler
    : AizenQueryHandler<GetUserListBffQuery, AdminUserListBffResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly IVesselRemoteCall _vessel;
    private readonly ILogger<GetUserListBffQueryHandler> _logger;

    public GetUserListBffQueryHandler(
        IIdentityRemoteCall identity,
        IVesselRemoteCall vessel,
        ILogger<GetUserListBffQueryHandler> logger)
    {
        _identity = identity;
        _vessel = vessel;
        _logger = logger;
    }

    public override async Task<AdminUserListBffResponse?> Handle(
        GetUserListBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminUserListBffResponse();

        try
        {
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

            var (approvalStatusFilter, profileStatusFilter) = MapStatusFilters(request.Status);

            var result = await _identity.SearchProfiles(
                firstName: request.Search,
                lastName: null,
                roleContext: request.IdentityType ?? MapRoleToRoleContext(request.Role),
                approvalStatus: approvalStatusFilter,
                status: profileStatusFilter,
                email: request.Search,
                pageIndex: pageIndex,
                pageSize: request.PageSize);

            if (result?.Header?.IsSuccess != true)
            {
                _logger.LogWarning("[UserListBff] Identity SearchProfiles returned non-success. ErrorCode={Code}", result?.Header?.ErrorCode);
                response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
                return response;
            }

            var page = result.Body;
            var profiles = page?.Items ?? new();

            // Bulk vessel count — single SQL query for all users on this page.
            var vesselCounts = await FetchVesselCountsAsync(profiles.Select(p => p.UserId).ToArray(), response, cancellationToken);

            response.Users = new UserPageBffDto
            {
                From = page?.From ?? 0,
                Index = page?.Index ?? 0,
                Size = page?.Size ?? request.PageSize,
                Count = page?.Count ?? 0,
                Pages = page?.Pages ?? 0,
                HasPrevious = page?.HasPrevious ?? false,
                HasNext = page?.HasNext ?? false,
                Items = profiles
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
                        VesselCount = vesselCounts.GetValueOrDefault(p.UserId, 0),
                        LastLoginAt = p.LastLoginAt?.ToString("o"),
                        CreatedAt = p.CreateDate?.ToString("o") ?? string.Empty,
                        AvatarInitials = AdminUserBffHelpers.ComputeAvatarInitials(p.FirstName, p.LastName)
                    })
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserListBff] Identity call failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
        }

        return response;
    }

    private async Task<Dictionary<long, int>> FetchVesselCountsAsync(
        long[] userIds,
        AdminUserListBffResponse response,
        CancellationToken cancellationToken)
    {
        if (userIds.Length == 0)
            return new Dictionary<long, int>();

        try
        {
            var vesselResult = await _vessel.GetVesselCountsByOwnerUserIds(userIds);

            if (vesselResult?.Header?.IsSuccess != true || vesselResult.Body == null)
            {
                _logger.LogWarning("[UserListBff] Vessel counts-by-owner returned non-success.");
                response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel"));
                return new Dictionary<long, int>();
            }

            return vesselResult.Body.ToDictionary(x => x.UserId, x => x.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserListBff] Vessel count enrichment failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel"));
            return new Dictionary<long, int>();
        }
    }

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
